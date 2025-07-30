using AssuranceApi.Profession.Models;

namespace AssuranceApi.Data;

/// <summary>
/// Interface for persisting profession history data.
/// </summary>
public interface IProfessionHistoryPersistence
{
    /// <summary>
    /// Retrieves the history of a profession by its ID.
    /// </summary>
    /// <param name="professionId">The ID of the profession.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of profession history records.</returns>
    Task<IEnumerable<ProfessionHistory>> GetHistoryAsync(string professionId);

    /// <summary>
    /// Creates a new profession history record.
    /// </summary>
    /// <param name="history">The profession history record to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the creation was successful.</returns>
    Task<bool> CreateAsync(ProfessionHistory history);

    /// <summary>
    /// Deletes all profession history records.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
    Task<bool> DeleteAllAsync();
}
