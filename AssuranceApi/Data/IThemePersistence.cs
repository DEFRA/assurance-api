using AssuranceApi.Data.Models;

namespace AssuranceApi.Data;

/// <summary>
/// Interface for theme persistence operations.
/// Defines the contract for CRUD operations on themes.
/// </summary>
public interface IThemePersistence
{
    /// <summary>
    /// Retrieves all themes from the database.
    /// </summary>
    /// <param name="includeArchived">Whether to include archived themes.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of themes.</returns>
    Task<List<ThemeModel>> GetAllAsync(bool includeArchived = false);

    /// <summary>
    /// Retrieves a theme by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the theme.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the theme if found; otherwise, null.</returns>
    Task<ThemeModel?> GetByIdAsync(string id);

    /// <summary>
    /// Creates a new theme in the database.
    /// </summary>
    /// <param name="theme">The theme model to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    Task<bool> CreateAsync(ThemeModel theme);

    /// <summary>
    /// Updates an existing theme in the database.
    /// </summary>
    /// <param name="id">The unique identifier of the theme to update.</param>
    /// <param name="theme">The updated theme model.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    Task<bool> UpdateAsync(string id, ThemeModel theme);

    /// <summary>
    /// Archives a theme by setting IsActive to false.
    /// </summary>
    /// <param name="id">The unique identifier of the theme to archive.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    Task<bool> ArchiveAsync(string id);

    /// <summary>
    /// Restores an archived theme by setting IsActive to true.
    /// </summary>
    /// <param name="id">The unique identifier of the theme to restore.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    Task<bool> RestoreAsync(string id);

    /// <summary>
    /// Gets all themes associated with a specific project.
    /// </summary>
    /// <param name="projectId">The project ID to filter by.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of themes.</returns>
    Task<List<ThemeModel>> GetByProjectIdAsync(string projectId);
}

