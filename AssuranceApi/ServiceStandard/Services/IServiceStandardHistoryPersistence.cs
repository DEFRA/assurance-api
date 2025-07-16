using AssuranceApi.ServiceStandard.Models;

namespace AssuranceApi.ServiceStandard.Services;

/// <summary>
/// Interface for persisting and retrieving the history of standard definitions.
/// </summary>
public interface IServiceStandardHistoryPersistence
{
    /// <summary>
    /// Creates a new history record for a standard definition.
    /// </summary>
    /// <param name="history">The history record to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    Task<bool> CreateAsync(ServiceStandardHistory history);

    /// <summary>
    /// Retrieves the history records for a specific standard definition.
    /// </summary>
    /// <param name="standardId">The ID of the standard definition.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of history records.</returns>
    Task<IEnumerable<ServiceStandardHistory>> GetHistoryAsync(string standardId);
}
