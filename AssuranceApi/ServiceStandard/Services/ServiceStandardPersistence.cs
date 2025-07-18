using AssuranceApi.Project.Models;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils.Mongo;
using MongoDB.Driver;

namespace AssuranceApi.ServiceStandard.Services;

/// <summary>
/// Provides persistence operations for service standards, including CRUD operations,
/// seeding, and soft delete functionality.
/// </summary>
public class ServiceStandardPersistence
    : MongoService<ServiceStandardModel>,
        IServiceStandardPersistence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceStandardPersistence"/> class.
    /// </summary>
    /// <param name="connectionFactory">The MongoDB client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ServiceStandardPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "serviceStandards", loggerFactory) { }

    /// <summary>
    /// Defines the indexes for the ServiceStandard collection.
    /// </summary>
    /// <param name="builder">The index keys definition builder.</param>
    /// <returns>A list of index models.</returns>
    protected override List<CreateIndexModel<ServiceStandardModel>> DefineIndexes(
        IndexKeysDefinitionBuilder<ServiceStandardModel> builder
    )
    {
        return new List<CreateIndexModel<ServiceStandardModel>>
        {
            new CreateIndexModel<ServiceStandardModel>(
                builder.Ascending(x => x.Number),
                new CreateIndexOptions { Unique = true }
            ),
            new CreateIndexModel<ServiceStandardModel>(builder.Ascending(x => x.IsActive)),
        };
    }

    /// <summary>
    /// Creates a new service standard in the database.
    /// </summary>
    /// <param name="serviceStandard">The project to create.</param>
    /// <returns>True if the service standard was created successfully; otherwise, false.</returns>
    public async Task<bool> CreateAsync(ServiceStandardModel serviceStandard)
    {
        try
        {
            await Collection.InsertOneAsync(serviceStandard);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create service standard");
            return false;
        }
    }

    /// <summary>
    /// Updates an existing service standard.
    /// </summary>
    /// <param name="standard">The updated service standard data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    public async Task<bool> UpdateAsync(ServiceStandardModel standard)
    {
        if (standard == null)
        {
            Logger.LogWarning("Attempted to update a null service standard");
            return false;
        }

        var id = standard.Id;

        try
        {
            Logger.LogInformation($"Updating service standard with ID='{id}'");

            var updateDef = Builders<ServiceStandardModel>.Update;
            var updates = new List<UpdateDefinition<ServiceStandardModel>>();

            if (standard.Name != null)
                updates.Add(updateDef.Set(x => x.Name, standard.Name));
            if (standard.Description != null)
                updates.Add(updateDef.Set(x => x.Description, standard.Description));
            if (standard.Guidance != null)
                updates.Add(updateDef.Set(x => x.Guidance, standard.Guidance));
            if (standard.DeletedAt != null)
                updates.Add(updateDef.Set(x => x.DeletedAt, standard.DeletedAt));
            if (standard.DeletedBy != null)
                updates.Add(updateDef.Set(x => x.DeletedBy, standard.DeletedBy));
            updates.Add(updateDef.Set(x => x.IsActive, standard.IsActive));
            updates.Add(updateDef.Set(x => x.UpdatedAt, standard.UpdatedAt));

            if (!updates.Any())
            {
                Logger.LogWarning($"No fields to update for service standard with ID='{id}'");
                return false;
            }

            var result = await Collection.UpdateOneAsync(
                x => x.Id == id,
                updateDef.Combine(updates)
            );

            if (result.ModifiedCount == 0)
            {
                Logger.LogWarning($"Nothing was updated for service standard with ID='{id}'");
            }
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to update service standard with ID='{id}'");
            return false;
        }
    }

    /// <summary>
    /// Seeds the service standards collection with the provided standards.
    /// </summary>
    /// <param name="standards">The list of service standards to seed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    public async Task<bool> SeedStandardsAsync(List<ServiceStandardModel> standards)
    {
        try
        {
            await Collection.DeleteManyAsync(Builders<ServiceStandardModel>.Filter.Empty);

            foreach (var standard in standards)
            {
                standard.IsActive = true;
                standard.CreatedAt = DateTime.UtcNow;
                standard.UpdatedAt = DateTime.UtcNow;
            }

            await Collection.InsertManyAsync(standards);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to seed service standards");
            return false;
        }
    }

    /// <summary>
    /// Retrieves all service standards.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of all service standards.</returns>
    public async Task<List<ServiceStandardModel>> GetAllAsync()
    {
        return await Collection
            .Find(Builders<ServiceStandardModel>.Filter.Empty)
            .SortBy(s => s.Number)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves all active service standards.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of active service standards.</returns>
    public async Task<List<ServiceStandardModel>> GetAllActiveAsync()
    {
        return await Collection.Find(s => s.IsActive).SortBy(s => s.Number).ToListAsync();
    }

    /// <summary>
    /// Retrieves a service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the service standard, or null if not found.</returns>
    public async Task<ServiceStandardModel?> GetByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retrieves an active service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the active service standard, or null if not found.</returns>
    public async Task<ServiceStandardModel?> GetActiveByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id && x.IsActive).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Deletes all service standards from the collection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(Builders<ServiceStandardModel>.Filter.Empty);
    }

    /// <summary>
    /// Performs a soft delete of a service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        return await SoftDeleteAsync(id, "System");
    }

    /// <summary>
    /// Performs a soft delete of a service standard by its ID and records the user who deleted it.
    /// </summary>
    /// <param name="id">The ID of the service standard to delete.</param>
    /// <param name="deletedBy">The user who deleted the service standard.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    public async Task<bool> SoftDeleteAsync(string id, string deletedBy)
    {
        var update = Builders<ServiceStandardModel>
            .Update.Set(x => x.IsActive, false)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(s => s.Id == id && s.IsActive, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Restores a previously soft-deleted service standard by its ID.
    /// </summary>
    /// <param name="id">The ID of the service standard to restore.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success.</returns>
    public async Task<bool> RestoreAsync(string id)
    {
        var update = Builders<ServiceStandardModel>
            .Update.Set(x => x.IsActive, true)
            .Unset(x => x.DeletedAt)
            .Unset(x => x.DeletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(s => s.Id == id && !s.IsActive, update);
        return result.ModifiedCount > 0;
    }
}
