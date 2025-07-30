using AssuranceApi.Project.Models;

namespace AssuranceApi.Data;

/// <summary>
/// Interface for persisting project history data.
/// </summary>
public interface IProjectHistoryPersistence
{
    /// <summary>
    /// Creates a new project history entry.
    /// </summary>
    /// <param name="history">The project history entry to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> CreateAsync(ProjectHistory history);

    /// <summary>
    /// Retrieves the history entries for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of project history entries.</returns>
    Task<IEnumerable<ProjectHistory>> GetHistoryAsync(string projectId);

    /// <summary>
    /// Deletes all project history entries.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAllAsync();

    /// <summary>
    /// Archives a specific project history entry.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="historyId">The ID of the history entry to archive.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> ArchiveHistoryEntryAsync(string projectId, string historyId);

    /// <summary>
    /// Retrieves the latest history entry for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the latest project history entry, or null if none exists.</returns>
    Task<ProjectHistory?> GetLatestHistoryAsync(string projectId);
}
