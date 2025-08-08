using AssuranceApi.Project.Models;

namespace AssuranceApi.Data;

/// <summary>
/// Interface for project persistence operations.
/// </summary>
public interface IProjectPersistence
{
    /// <summary>
    /// Creates a new project asynchronously.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> CreateAsync(ProjectModel project);

    /// <summary>
    /// Retrieves all projects asynchronously, optionally filtered by the passed in parameters.
    /// </summary>
    /// <param name="projectQueryParameters">The optional parameters to filter projects.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of projects.</returns>
    Task<List<ProjectModel>> GetAllAsync(ProjectQueryParameters projectQueryParameters);

    /// <summary>
    /// Retrieves a project by its ID asynchronously.
    /// </summary>
    /// <param name="id">The ID of the project to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the project, or null if not found.</returns>
    Task<ProjectModel?> GetByIdAsync(string id);

    /// <summary>
    /// Updates an existing project asynchronously.
    /// </summary>
    /// <param name="id">The ID of the project to update.</param>
    /// <param name="project">The updated project data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> UpdateAsync(string id, ProjectModel project);

    /// <summary>
    /// Deletes all projects asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAllAsync();

    /// <summary>
    /// Deletes a project by its ID asynchronously.
    /// </summary>
    /// <param name="id">The ID of the project to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Seeds the database with a list of projects asynchronously.
    /// </summary>
    /// <param name="projects">The list of projects to seed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> SeedAsync(List<ProjectModel> projects);

    /// <summary>
    /// Adds multiple projects asynchronously.
    /// </summary>
    /// <param name="projects">The list of projects to add.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> AddProjectsAsync(List<ProjectModel> projects);
}
