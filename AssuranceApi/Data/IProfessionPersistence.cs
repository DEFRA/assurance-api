using AssuranceApi.Profession.Models;

namespace AssuranceApi.Data;

/// <summary>
/// Interface for managing persistence operations related to professions.
/// </summary>
public interface IProfessionPersistence
{
    /// <summary>
    /// Retrieves all professions.
    /// </summary>
    /// <returns>A collection of all professions.</returns>
    Task<IEnumerable<ProfessionModel>> GetAllAsync();

    /// <summary>
    /// Retrieves all active professions.
    /// </summary>
    /// <returns>A collection of all active professions.</returns>
    Task<IEnumerable<ProfessionModel>> GetAllActiveAsync();

    /// <summary>
    /// Retrieves a profession by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the profession.</param>
    /// <returns>The profession if found; otherwise, null.</returns>
    Task<ProfessionModel?> GetByIdAsync(string id);

    /// <summary>
    /// Retrieves an active profession by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the profession.</param>
    /// <returns>The active profession if found; otherwise, null.</returns>
    Task<ProfessionModel?> GetActiveByIdAsync(string id);

    /// <summary>
    /// Creates a new profession.
    /// </summary>
    /// <param name="profession">The profession to create.</param>
    /// <returns>True if the creation was successful; otherwise, false.</returns>
    Task<bool> CreateAsync(ProfessionModel profession);

    /// <summary>
    /// Updates an existing profession.
    /// </summary>
    /// <param name="profession">The updated profession data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> UpdateAsync(ProfessionModel profession);

    /// <summary>
    /// Seeds the database with a collection of professions.
    /// </summary>
    /// <param name="professions">The collection of professions to seed.</param>
    /// <returns>True if the seeding was successful; otherwise, false.</returns>
    Task<bool> SeedProfessionsAsync(IEnumerable<ProfessionModel> professions);

    /// <summary>
    /// Deletes all professions.
    /// </summary>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteAllAsync();

    /// <summary>
    /// Deletes a profession by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the profession to delete.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Soft deletes a profession by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the profession to soft delete.</param>
    /// <param name="deletedBy">The user who performed the deletion.</param>
    /// <returns>True if the soft deletion was successful; otherwise, false.</returns>
    Task<bool> SoftDeleteAsync(string id, string deletedBy);

    /// <summary>
    /// Restores a soft-deleted profession by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the profession to restore.</param>
    /// <returns>True if the restoration was successful; otherwise, false.</returns>
    Task<bool> RestoreAsync(string id);
}
