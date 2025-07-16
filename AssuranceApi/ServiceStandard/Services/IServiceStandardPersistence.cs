using AssuranceApi.ServiceStandard.Models;

namespace AssuranceApi.ServiceStandard.Services;

/// <summary>
/// Interface for persistence operations related to Service Standards.
/// </summary>
public interface IServiceStandardPersistence
{
    /// <summary>
    /// Creates a new service standard.
    /// </summary>
    /// <param name="serviceStandard">The service standard to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> CreateAsync(ServiceStandardModel serviceStandard);

    /// <summary>
    /// Seeds the database with a list of service standards.
    /// </summary>
    /// <param name="standards">The list of service standards to seed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> SeedStandardsAsync(List<ServiceStandardModel> standards);

    /// <summary>
    /// Retrieves all service standards.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of all service standards.</returns>
    Task<List<ServiceStandardModel>> GetAllAsync();

    /// <summary>
    /// Retrieves all active service standards.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of all active service standards.</returns>
    Task<List<ServiceStandardModel>> GetAllActiveAsync();

    /// <summary>
    /// Retrieves a service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the service standard if found, otherwise null.</returns>
    Task<ServiceStandardModel?> GetByIdAsync(string id);

    /// <summary>
    /// Retrieves an active service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the active service standard if found, otherwise null.</returns>
    Task<ServiceStandardModel?> GetActiveByIdAsync(string id);

    /// <summary>
    /// Deletes all service standards.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAllAsync();

    /// <summary>
    /// Deletes a service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Soft deletes a service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard to soft delete.</param>
    /// <param name="deletedBy">The user who performed the deletion.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> SoftDeleteAsync(string id, string deletedBy);

    /// <summary>
    /// Restores a soft-deleted service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard to restore.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    Task<bool> RestoreAsync(string id);
}
