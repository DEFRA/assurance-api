using AssuranceApi.Profession.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Profession.Services;

internal class ProfessionHistoryPersistence
    : MongoService<ProfessionHistory>,
        IProfessionHistoryPersistence
{
    public ProfessionHistoryPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "professionHistory", loggerFactory)
    {
        Logger.LogInformation(
            "Initializing ProfessionHistoryPersistence with collection: professionHistory"
        );
        try
        {
            var builder = Builders<ProfessionHistory>.IndexKeys;
            var indexes = DefineIndexes(builder);
            foreach (var index in indexes)
            {
                Collection.Indexes.CreateOne(index);
            }
            Logger.LogInformation("Successfully created indexes for professionHistory collection");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create indexes for professionHistory collection");
        }
    }

    protected override List<CreateIndexModel<ProfessionHistory>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProfessionHistory> builder
    )
    {
        return new List<CreateIndexModel<ProfessionHistory>>
        {
            new CreateIndexModel<ProfessionHistory>(
                builder.Ascending(x => x.ProfessionId).Ascending(x => x.Timestamp)
            ),
        };
    }

    public async Task<bool> CreateAsync(ProfessionHistory history)
    {
        try
        {
            Logger.LogInformation(
                "Creating history entry for profession {ProfessionId}",
                history.ProfessionId
            );
            await Collection.InsertOneAsync(history);
            Logger.LogInformation("Successfully created history entry");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create profession history");
            return false;
        }
    }

    public async Task<IEnumerable<ProfessionHistory>> GetHistoryAsync(string professionId)
    {
        try
        {
            Logger.LogInformation("Getting history for profession {ProfessionId}", professionId);
            var result = await Collection
                .Find(x => x.ProfessionId == professionId)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();
            Logger.LogInformation("Found {Count} history entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get profession history");
            return Enumerable.Empty<ProfessionHistory>();
        }
    }

    public async Task<bool> DeleteAllAsync()
    {
        try
        {
            await Collection.DeleteManyAsync(Builders<ProfessionHistory>.Filter.Empty);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete all profession history");
            return false;
        }
    }
}
