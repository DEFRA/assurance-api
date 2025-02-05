using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.ServiceStandard.Services;

public class ServiceStandardPersistence : MongoService<ServiceStandardModel>, IServiceStandardPersistence
{
    public ServiceStandardPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "serviceStandards", loggerFactory)
    {
    }

    protected override List<CreateIndexModel<ServiceStandardModel>> DefineIndexes(
        IndexKeysDefinitionBuilder<ServiceStandardModel> builder)
    {
        return new List<CreateIndexModel<ServiceStandardModel>>
        {
            new CreateIndexModel<ServiceStandardModel>(
                builder.Ascending(x => x.Number),
                new CreateIndexOptions { Unique = true })
        };
    }

    public async Task<bool> SeedStandardsAsync(List<ServiceStandardModel> standards)
    {
        try
        {
            // Clear existing standards
            await Collection.DeleteManyAsync(Builders<ServiceStandardModel>.Filter.Empty);
            
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
        return await Collection.Find(Builders<ServiceStandardModel>.Filter.Empty)
            .SortBy(s => s.Number)
            .ToListAsync();
    }

    public async Task<ServiceStandardModel?> GetByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }
} 