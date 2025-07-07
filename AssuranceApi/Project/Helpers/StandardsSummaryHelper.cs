using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;

namespace AssuranceApi.Project.Helpers;

/// <summary>
/// Helper class for managing and updating standards summary for projects.
/// </summary>
public class StandardsSummaryHelper
{
    private readonly IProjectPersistence _projectPersistence;
    private readonly IProjectStandardsPersistence _assessmentPersistence;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardsSummaryHelper"/> class.
    /// </summary>
    /// <param name="projectPersistence">The project persistence service.</param>
    /// <param name="assessmentPersistence">The assessment persistence service.</param>
    public StandardsSummaryHelper(
        IProjectPersistence projectPersistence,
        IProjectStandardsPersistence assessmentPersistence
    )
    {
        _projectPersistence = projectPersistence;
        _assessmentPersistence = assessmentPersistence;
    }

    /// <summary>
    /// Updates the standards summary cache for a given project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateStandardsSummaryCacheAsync(string projectId)
    {
        var assessments = await _assessmentPersistence.GetByProjectAsync(projectId);
        var grouped = assessments
            .GroupBy(a => a.StandardId)
            .Select(g => new StandardSummaryModel
            {
                StandardId = g.Key,
                AggregatedStatus = AggregateStatus(g.Select(x => x.Status)),
                AggregatedCommentary = string.Join(
                    "; ",
                    g.Select(x => x.Commentary).Where(c => !string.IsNullOrWhiteSpace(c))
                ),
                LastUpdated = g.Max(x => x.LastUpdated),
                Professions = g.Select(x => new StandardSummaryProfessionModel
                {
                    ProfessionId = x.ProfessionId,
                    Status = x.Status,
                    Commentary = x.Commentary,
                    LastUpdated = x.LastUpdated,
                })
                    .ToList(),
            })
            .ToList();

        var project = await _projectPersistence.GetByIdAsync(projectId);
        if (project != null)
        {
            project.StandardsSummary = grouped;
            await _projectPersistence.UpdateAsync(projectId, project);
        }
    }

    /// <summary>
    /// Aggregates the statuses into a single status based on priority.
    /// </summary>
    /// <param name="statuses">The collection of statuses to aggregate.</param>
    /// <returns>The aggregated status.</returns>
    public static string AggregateStatus(IEnumerable<string> statuses)
    {
        // Aggregation logic that maps 5 RAG to 3 RAG for service standards
        // Maps: AMBER_RED -> AMBER, GREEN_AMBER -> AMBER
        // Priority: RED > AMBER > GREEN > TBC
        var mappedStatuses = statuses.Select(status =>
            status switch
            {
                "AMBER_RED" => "AMBER",
                "GREEN_AMBER" => "AMBER",
                _ => status,
            }
        );

        var order = new[] { "RED", "AMBER", "GREEN", "TBC" };
        return mappedStatuses.OrderBy(s => Array.IndexOf(order, s)).FirstOrDefault()
            ?? "NOT_UPDATED";
    }
}
