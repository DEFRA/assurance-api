using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.ServiceStandard.Services;

public class ServiceStandardPersistence
    : MongoService<ServiceStandardModel>,
        IServiceStandardPersistence
{
    public ServiceStandardPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "serviceStandards", loggerFactory) { }

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

    public async Task<bool> SeedStandardsAsync(List<ServiceStandardModel> standards)
    {
        try
        {
            // Clear existing standards
            await Collection.DeleteManyAsync(Builders<ServiceStandardModel>.Filter.Empty);

            // Set audit fields for seeded standards
            foreach (var standard in standards)
            {
                standard.IsActive = true;
                standard.CreatedAt = DateTime.UtcNow;
                standard.UpdatedAt = DateTime.UtcNow;
            }

            // Insert new standards
            await Collection.InsertManyAsync(standards);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to seed service standards");
            return false;
        }
    }

    public async Task<List<ServiceStandardModel>> GetAllAsync()
    {
        return await Collection
            .Find(Builders<ServiceStandardModel>.Filter.Empty)
            .SortBy(s => s.Number)
            .ToListAsync();
    }

    public async Task<List<ServiceStandardModel>> GetAllActiveAsync()
    {
        return await Collection.Find(s => s.IsActive).SortBy(s => s.Number).ToListAsync();
    }

    public async Task<ServiceStandardModel?> GetByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<ServiceStandardModel?> GetActiveByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id && x.IsActive).FirstOrDefaultAsync();
    }

    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(Builders<ServiceStandardModel>.Filter.Empty);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        // Now performs soft delete instead of hard delete
        return await SoftDeleteAsync(id, "System");
    }

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
