using AssuranceApi.Project.Models;

namespace AssuranceApi.Data;

/// <summary>
/// Interface for managing the persistence of project standards history.
/// </summary>
public interface IProjectStandardsHistoryPersistence
{
    /// <summary>
    /// Retrieves the history of a specific project standard for a given profession.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="standardId">The ID of the standard.</param>
    /// <param name="professionId">The ID of the profession.</param>
    /// <returns>A list of <see cref="ProjectStandardsHistory"/> objects.</returns>
    Task<List<ProjectStandardsHistory>> GetHistoryAsync(
        string projectId,
        string standardId,
        string professionId
    );

    /// <summary>
    /// Adds a new project standards history record.
    /// </summary>
    /// <param name="history">The <see cref="ProjectStandardsHistory"/> object to add.</param>
    Task AddAsync(ProjectStandardsHistory history);

    /// <summary>
    /// Archives a specific project standards history record.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="standardId">The ID of the standard.</param>
    /// <param name="professionId">The ID of the profession.</param>
    /// <param name="historyId">The ID of the history record to archive.</param>
    /// <returns>A boolean indicating whether the operation was successful.</returns>
    Task<bool> ArchiveAsync(
        string projectId,
        string standardId,
        string professionId,
        string historyId
    );
}
