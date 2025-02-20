using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Project.Services;

public class StandardHistoryPersistence : MongoService<StandardHistory>, IStandardHistoryPersistence
{
    public StandardHistoryPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "standardHistory", loggerFactory)
    {
        Logger.LogInformation("Initializing StandardHistoryPersistence with collection: standardHistory");
        try 
        {
            var builder = Builders<StandardHistory>.IndexKeys;
            var indexes = DefineIndexes(builder);
            foreach (var index in indexes)
            {
                Collection.Indexes.CreateOne(index);
            }
            Logger.LogInformation("Successfully created indexes for standardHistory collection");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create indexes for standardHistory collection");
        }
    }

    protected override List<CreateIndexModel<StandardHistory>> DefineIndexes(
        IndexKeysDefinitionBuilder<StandardHistory> builder)
    {
        return new List<CreateIndexModel<StandardHistory>>
        {
            new CreateIndexModel<StandardHistory>(
                builder.Ascending(x => x.ProjectId)
                    .Ascending(x => x.StandardId)
                    .Ascending(x => x.Timestamp))
        };
    }

    public async Task<bool> CreateAsync(StandardHistory history)
    {
        try
        {
            Logger.LogInformation("Creating history entry for standard {StandardId} in project {ProjectId}", 
                history.StandardId, history.ProjectId);
            await Collection.InsertOneAsync(history);
            Logger.LogInformation("Successfully created history entry");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create standard history");
            return false;
        }
    }

    public async Task<IEnumerable<StandardHistory>> GetHistoryAsync(string projectId, string standardId)
    {
        try
        {
            Logger.LogInformation("Getting history for standard {StandardId} in project {ProjectId}", 
                standardId, projectId);
            var result = await Collection
                .Find(x => x.ProjectId == projectId && x.StandardId == standardId)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();
            Logger.LogInformation("Found {Count} history entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get standard history");
            return Enumerable.Empty<StandardHistory>();
        }
    }

    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(Builders<StandardHistory>.Filter.Empty);
    }
} 