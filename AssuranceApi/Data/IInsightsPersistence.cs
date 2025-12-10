using AssuranceApi.Insights.Models;

namespace AssuranceApi.Data;

/// <summary>
/// Interface for insights data persistence operations.
/// </summary>
public interface IInsightsPersistence
{
    /// <summary>
    /// Gets deliveries that haven't had a service standard update within the specified threshold.
    /// </summary>
    /// <param name="standardThresholdDays">The number of days threshold for considering a delivery stale.</param>
    /// <returns>A list of deliveries needing standard updates, sorted by oldest first.</returns>
    Task<List<DeliveryNeedingUpdate>> GetDeliveriesNeedingStandardUpdatesAsync(int standardThresholdDays);

    /// <summary>
    /// Gets deliveries where service standards have worsened within the specified period.
    /// </summary>
    /// <param name="worseningDays">The number of days to look back for worsening standards.</param>
    /// <param name="historyDepth">The number of historical statuses to include (default: 5).</param>
    /// <returns>A list of deliveries with worsening standards.</returns>
    Task<List<WorseningStandardsDelivery>> GetDeliveriesWithWorseningStandardsAsync(int worseningDays, int historyDepth = 5);
}

