using AssuranceApi.Insights.Models;
using AssuranceApi.Project.Models;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssuranceApi.Data;

/// <summary>
/// Provides persistence operations for Insights data aggregation.
/// </summary>
public class InsightsPersistence : IInsightsPersistence
{
    private readonly IMongoCollection<ProjectModel> _projectsCollection;
    private readonly IMongoCollection<ProjectStandardsHistory> _standardsHistoryCollection;
    private readonly IMongoCollection<ServiceStandardModel> _serviceStandardsCollection;
    private readonly ILogger<InsightsPersistence> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InsightsPersistence"/> class.
    /// </summary>
    /// <param name="connectionFactory">The MongoDB client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public InsightsPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
    {
        _projectsCollection = connectionFactory.GetCollection<ProjectModel>("projects");
        _standardsHistoryCollection = connectionFactory.GetCollection<ProjectStandardsHistory>("projectStandardsHistory");
        _serviceStandardsCollection = connectionFactory.GetCollection<ServiceStandardModel>("serviceStandards");
        _logger = loggerFactory.CreateLogger<InsightsPersistence>();

        _logger.LogInformation("Initializing InsightsPersistence");
    }

    /// <inheritdoc/>
    public async Task<List<DeliveryNeedingUpdate>> GetDeliveriesNeedingStandardUpdatesAsync(int standardThresholdDays)
    {
        _logger.LogDebug("Getting deliveries needing standard updates with threshold of {Days} days", standardThresholdDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-standardThresholdDays);
        var result = new List<DeliveryNeedingUpdate>();

        // Get all projects
        var projects = await _projectsCollection.Find(_ => true).ToListAsync();

        foreach (var project in projects)
        {
            // Find the most recent assessment update for this project
            var latestUpdate = await _standardsHistoryCollection
                .Find(h => h.ProjectId == project.Id && !h.Archived)
                .SortByDescending(h => h.Timestamp)
                .Limit(1)
                .FirstOrDefaultAsync();

            var lastUpdateDate = latestUpdate?.Timestamp;
            var daysSinceUpdate = lastUpdateDate.HasValue
                ? (int)(DateTime.UtcNow - lastUpdateDate.Value).TotalDays
                : int.MaxValue;

            if (!lastUpdateDate.HasValue || lastUpdateDate.Value < cutoffDate)
            {
                result.Add(new DeliveryNeedingUpdate
                {
                    Id = project.Id,
                    Name = project.Name,
                    Status = project.ProjectStatus?.LowestRag ?? project.Status,
                    LastServiceStandardUpdate = lastUpdateDate,
                    DaysSinceStandardUpdate = daysSinceUpdate
                });
            }
        }

        // Sort by oldest first (most days since update)
        return result.OrderByDescending(d => d.DaysSinceStandardUpdate).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<WorseningStandardsDelivery>> GetDeliveriesWithWorseningStandardsAsync(int worseningDays, int historyDepth = 5)
    {
        _logger.LogDebug("Getting deliveries with worsening standards within {Days} days", worseningDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-worseningDays);
        var result = new List<WorseningStandardsDelivery>();

        // Get all service standards for name lookup
        var serviceStandards = await _serviceStandardsCollection
            .Find(s => s.IsActive)
            .ToListAsync();
        var standardLookup = serviceStandards.ToDictionary(s => s.Id, s => s);

        // Get all projects
        var projects = await _projectsCollection.Find(_ => true).ToListAsync();

        foreach (var project in projects)
        {
            var standardChanges = new List<StandardChange>();

            // Get all non-archived history for this project, grouped by standard
            var historyByStandard = await GetHistoryByStandardAsync(project.Id, cutoffDate);

            foreach (var kvp in historyByStandard)
            {
                var standardId = kvp.Key;
                var history = kvp.Value;

                // Check if there's a worsening change in the recent period
                var recentHistory = history.Where(h => h.Timestamp >= cutoffDate).ToList();
                if (recentHistory.Count == 0)
                    continue;

                // Get last N statuses (oldest to newest) - only use actual entries, no padding
                var statusHistory = history
                    .Take(historyDepth)
                    .Reverse()
                    .Select(h => h.Changes?.Status?.To ?? "UNKNOWN")
                    .ToList();

                // Check if the most recent change was a worsening (or first-time RED/AMBER)
                if (IsWorseningChange(history))
                {
                    if (standardLookup.TryGetValue(standardId, out var standard))
                    {
                        standardChanges.Add(new StandardChange
                        {
                            StandardNumber = standard.Number,
                            StandardName = standard.Name,
                            StatusHistory = statusHistory
                        });
                    }
                }
            }

            if (standardChanges.Count > 0)
            {
                result.Add(new WorseningStandardsDelivery
                {
                    Id = project.Id,
                    Name = project.Name,
                    Status = project.ProjectStatus?.LowestRag ?? project.Status,
                    StandardChanges = standardChanges.OrderBy(s => s.StandardNumber).ToList()
                });
            }
        }

        return result;
    }

    private async Task<Dictionary<string, List<ProjectStandardsHistory>>> GetHistoryByStandardAsync(string projectId, DateTime since)
    {
        var history = await _standardsHistoryCollection
            .Find(h => h.ProjectId == projectId && !h.Archived)
            .SortByDescending(h => h.Timestamp)
            .ToListAsync();

        return history
            .GroupBy(h => h.StandardId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static bool IsWorseningChange(List<ProjectStandardsHistory> history)
    {
        if (history.Count == 0)
            return false;

        // Define status rankings (higher is better)
        var statusRank = new Dictionary<string, int>
        {
            { "GREEN", 3 },
            { "AMBER", 2 },
            { "RED", 1 },
            { "PENDING", 0 }
        };

        // Get the current (most recent) status
        var current = history[0].Changes?.Status?.To?.ToUpperInvariant();
        if (string.IsNullOrEmpty(current) || !statusRank.TryGetValue(current, out var currentRank))
            return false;

        // If there's only one history entry, it's "worsening" if the first-ever status is RED or AMBER
        // (compared to an implied PENDING baseline - not yet assessed)
        if (history.Count == 1)
        {
            return currentRank < statusRank["GREEN"]; // RED or AMBER is worse than implied PENDING->GREEN
        }

        // Get the previous status
        var previous = history[1].Changes?.Status?.To?.ToUpperInvariant();
        if (string.IsNullOrEmpty(previous) || !statusRank.TryGetValue(previous, out var previousRank))
            return false;

        // Worsening means current rank is lower than previous
        return currentRank < previousRank;
    }
}

