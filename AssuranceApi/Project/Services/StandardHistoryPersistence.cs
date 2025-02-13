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
            await Collection.InsertOneAsync(history);
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
            var filter = Builders<StandardHistory>.Filter.And(
                Builders<StandardHistory>.Filter.Eq(h => h.ProjectId, projectId),
                Builders<StandardHistory>.Filter.Eq(h => h.StandardId, standardId)
            );

            return await Collection
                .Find(filter)
                .SortByDescending(h => h.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get standard history");
            return Enumerable.Empty<StandardHistory>();
        }
    }
} 