using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.ServiceStandard.Services;

public class ServiceStandardHistoryPersistence
    : MongoService<StandardDefinitionHistory>,
        IServiceStandardHistoryPersistence
{
    public ServiceStandardHistoryPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "serviceStandardHistory", loggerFactory)
    {
        Logger.LogInformation(
            "Initializing ServiceStandardHistoryPersistence with collection: serviceStandardHistory"
        );
        try
        {
            var builder = Builders<StandardDefinitionHistory>.IndexKeys;
            var indexes = DefineIndexes(builder);
            foreach (var index in indexes)
            {
                Collection.Indexes.CreateOne(index);
            }
            Logger.LogInformation(
                "Successfully created indexes for serviceStandardHistory collection"
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create indexes for serviceStandardHistory collection");
        }
    }

    protected override List<CreateIndexModel<StandardDefinitionHistory>> DefineIndexes(
        IndexKeysDefinitionBuilder<StandardDefinitionHistory> builder
    )
    {
        return new List<CreateIndexModel<StandardDefinitionHistory>>
        {
            new CreateIndexModel<StandardDefinitionHistory>(
                builder.Ascending(x => x.StandardId).Ascending(x => x.Timestamp)
            ),
        };
    }

    public async Task<bool> CreateAsync(StandardDefinitionHistory history)
    {
        try
        {
            Logger.LogInformation(
                "Creating history entry for standard {StandardId}",
                history.StandardId
            );
            await Collection.InsertOneAsync(history);
            Logger.LogInformation("Successfully created history entry");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create service standard history");
            return false;
        }
    }

    public async Task<IEnumerable<StandardDefinitionHistory>> GetHistoryAsync(string standardId)
    {
        try
        {
            Logger.LogInformation("Getting history for standard {StandardId}", standardId);
            var result = await Collection
                .Find(x => x.StandardId == standardId)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();
            Logger.LogInformation("Found {Count} history entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get service standard history");
            return Enumerable.Empty<StandardDefinitionHistory>();
        }
    }
}
