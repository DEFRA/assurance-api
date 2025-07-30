using AssuranceApi.Project.Models;

namespace AssuranceApi.Data;

/// <summary>
/// Interface for managing persistence of project standards.
/// </summary>
public interface IProjectStandardsPersistence
{
    /// <summary>
    /// Retrieves a specific project standard by project ID, standard ID, and profession ID.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="standardId">The ID of the standard.</param>
    /// <param name="professionId">The ID of the profession.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the project standard, or null if not found.</returns>
    Task<ProjectStandards?> GetAsync(string projectId, string standardId, string professionId);

    /// <summary>
    /// Retrieves all project standards for a specific project and standard.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="standardId">The ID of the standard.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of project standards.</returns>
    Task<List<ProjectStandards>> GetByProjectAndStandardAsync(string projectId, string standardId);

    /// <summary>
    /// Retrieves all project standards for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of project standards.</returns>
    Task<List<ProjectStandards>> GetByProjectAsync(string projectId);

    /// <summary>
    /// Inserts or updates a project standard.
    /// </summary>
    /// <param name="assessment">The project standard to upsert.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpsertAsync(ProjectStandards assessment);

    /// <summary>
    /// Deletes a specific project standard by project ID, standard ID, and profession ID.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="standardId">The ID of the standard.</param>
    /// <param name="professionId">The ID of the profession.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
    Task<bool> DeleteAsync(string projectId, string standardId, string professionId);
}
