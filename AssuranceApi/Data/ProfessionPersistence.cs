using AssuranceApi.Profession.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Data;

/// <summary>
/// Provides persistence operations for Profession entities in the MongoDB database.
/// </summary>
public class ProfessionPersistence : MongoService<ProfessionModel>, IProfessionPersistence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfessionPersistence"/> class.
    /// </summary>
    /// <param name="connectionFactory">The MongoDB client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ProfessionPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "professions", loggerFactory) { }

    /// <summary>
    /// Defines the indexes for the Profession collection.
    /// </summary>
    /// <param name="builder">The index keys definition builder.</param>
    /// <returns>A list of index models for the Profession collection.</returns>
    protected override List<CreateIndexModel<ProfessionModel>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProfessionModel> builder
    )
    {
        return new List<CreateIndexModel<ProfessionModel>>
        {
            new CreateIndexModel<ProfessionModel>(
                builder.Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true }
            ),
            new CreateIndexModel<ProfessionModel>(builder.Ascending(x => x.IsActive)),
        };
    }

    /// <summary>
    /// Creates a new profession in the database.
    /// </summary>
    /// <param name="profession">The profession to create.</param>
    /// <returns>True if the creation was successful; otherwise, false.</returns>
    public async Task<bool> CreateAsync(ProfessionModel profession)
    {
        try
        {
            // Check if profession with same ID already exists
            var existing = await GetByIdAsync(profession.Id);
            if (existing != null)
            {
                Logger.LogWarning("Profession with ID {Id} already exists", profession.Id);
                return false;
            }

            // Set audit fields
            profession.IsActive = true;
            profession.CreatedAt = DateTime.UtcNow;
            profession.UpdatedAt = DateTime.UtcNow;

            await Collection.InsertOneAsync(profession);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create profession");
            return false;
        }
    }

    /// <summary>
    /// Updates an existing profession in the database.
    /// </summary>
    /// <param name="profession">The updated profession data.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    public async Task<bool> UpdateAsync(ProfessionModel profession)
    {
        try
        {
            // Set audit field
            profession.UpdatedAt = DateTime.UtcNow;

            var filter = Builders<ProfessionModel>.Filter.Eq(x => x.Id, profession.Id);
            var update = Builders<ProfessionModel>.Update
                .Set(x => x.Name, profession.Name)
                .Set(x => x.Description, profession.Description)
                .Set(x => x.IsActive, profession.IsActive)
                .Set(x => x.UpdatedAt, profession.UpdatedAt);

            if (profession.DeletedAt != null)
                update = update.Set(x => x.DeletedAt, profession.DeletedAt);
            else
                update = update.Unset(x => x.DeletedAt);

            if (!string.IsNullOrEmpty(profession.DeletedBy))
                update = update.Set(x => x.DeletedBy, profession.DeletedBy);
            else
                update = update.Unset(x => x.DeletedBy);

            var result = await Collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update profession with ID {Id}", profession.Id);
            return false;
        }
    }

    /// <summary>
    /// Seeds the database with a collection of professions. If a profession with the same ID exists, it will be updated; otherwise, it will be inserted.
    /// </summary>
    /// <param name="professions">The collection of professions to seed.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    public async Task<bool> SeedProfessionsAsync(IEnumerable<ProfessionModel> professions)
    {
        try
        {
            // Upsert each profession by Id (incremental update)
            foreach (var profession in professions)
            {
                // Set audit fields
                profession.IsActive = true;
                profession.CreatedAt = DateTime.UtcNow;
                profession.UpdatedAt = DateTime.UtcNow;

                var filter = Builders<ProfessionModel>.Filter.Eq(x => x.Id, profession.Id);
                await Collection.ReplaceOneAsync(
                    filter,
                    profession,
                    new ReplaceOptions { IsUpsert = true }
                );
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to seed professions");
            return false;
        }
    }

    /// <summary>
    /// Retrieves all professions from the database.
    /// </summary>
    /// <returns>A collection of all professions.</returns>
    public async Task<IEnumerable<ProfessionModel>> GetAllAsync()
    {
        return await Collection
            .Find(Builders<ProfessionModel>.Filter.Empty)
            .SortBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves all active professions from the database.
    /// </summary>
    /// <returns>A collection of all active professions.</returns>
    public async Task<IEnumerable<ProfessionModel>> GetAllActiveAsync()
    {
        return await Collection.Find(p => p.IsActive).SortBy(p => p.Name).ToListAsync();
    }

    /// <summary>
    /// Retrieves a profession by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the profession.</param>
    /// <returns>The profession with the specified ID, or null if not found.</returns>
    public async Task<ProfessionModel?> GetByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retrieves an active profession by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the profession.</param>
    /// <returns>The active profession with the specified ID, or null if not found.</returns>
    public async Task<ProfessionModel?> GetActiveByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id && x.IsActive).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Deletes all professions from the database.
    /// </summary>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    public async Task<bool> DeleteAllAsync()
    {
        try
        {
            await Collection.DeleteManyAsync(Builders<ProfessionModel>.Filter.Empty);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete all professions");
            return false;
        }
    }

    /// <summary>
    /// Deletes a profession by its unique identifier. This performs a soft delete, marking the profession as inactive.
    /// </summary>
    /// <param name="id">The unique identifier of the profession to delete.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        // Now performs soft delete instead of hard delete
        return await SoftDeleteAsync(id, "System");
    }

    /// <summary>
    /// Performs a soft delete on a profession by marking it as inactive and setting audit fields.
    /// </summary>
    /// <param name="id">The unique identifier of the profession to delete.</param>
    /// <param name="deletedBy">The user or system that performed the delete operation.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    public async Task<bool> SoftDeleteAsync(string id, string deletedBy)
    {
        var update = Builders<ProfessionModel>
            .Update.Set(x => x.IsActive, false)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(p => p.Id == id && p.IsActive, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Restores a previously soft-deleted profession by marking it as active and clearing audit fields.
    /// </summary>
    /// <param name="id">The unique identifier of the profession to restore.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    public async Task<bool> RestoreAsync(string id)
    {
        var update = Builders<ProfessionModel>
            .Update.Set(x => x.IsActive, true)
            .Unset(x => x.DeletedAt)
            .Unset(x => x.DeletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(p => p.Id == id && !p.IsActive, update);
        return result.ModifiedCount > 0;
    }
}
