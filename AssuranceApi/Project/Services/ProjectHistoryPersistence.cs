using MongoDB.Driver;
using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;

namespace AssuranceApi.Project.Services;

public class ProjectHistoryPersistence : MongoService<ProjectHistory>, IProjectHistoryPersistence
{
    public ProjectHistoryPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "projectHistory", loggerFactory)
    {
        Logger.LogInformation("Initializing ProjectHistoryPersistence with collection: projectHistory");
        try 
        {
            // Create the index
            var builder = Builders<ProjectHistory>.IndexKeys;
            var indexes = DefineIndexes(builder);
            foreach (var index in indexes)
            {
                Collection.Indexes.CreateOne(index);
            }
            Logger.LogInformation("Successfully created indexes for projectHistory collection");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create indexes for projectHistory collection");
        }
    }

    protected override List<CreateIndexModel<ProjectHistory>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProjectHistory> builder)
    {
        return new List<CreateIndexModel<ProjectHistory>>
        {
            new CreateIndexModel<ProjectHistory>(
                builder.Ascending(x => x.ProjectId)
                    .Ascending(x => x.Timestamp))
        };
    }

    public async Task<bool> CreateAsync(ProjectHistory history)
    {
        try
        {
            Logger.LogInformation("Creating history entry for project {ProjectId}", 
                history.ProjectId);
            await Collection.InsertOneAsync(history);
            Logger.LogInformation("Successfully created history entry");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create project history");
            return false;
        }
    }

    public async Task<IEnumerable<ProjectHistory>> GetHistoryAsync(string projectId)
    {
        try
        {
            Logger.LogInformation("Getting history for project {ProjectId}", projectId);
            var result = await Collection
                .Find(x => x.ProjectId == projectId && !x.IsArchived)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();
            Logger.LogInformation("Found {Count} history entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get project history");
            return Enumerable.Empty<ProjectHistory>();
        }
    }

    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(Builders<ProjectHistory>.Filter.Empty);
    }

    public async Task<bool> ArchiveHistoryEntryAsync(string projectId, string historyId)
    {
        try
        {
            Logger.LogInformation("Archiving history entry {HistoryId} for project {ProjectId}", 
                historyId, projectId);
                
            var filter = Builders<ProjectHistory>.Filter.And(
                Builders<ProjectHistory>.Filter.Eq(h => h.Id, historyId),
                Builders<ProjectHistory>.Filter.Eq(h => h.ProjectId, projectId)
            );
            
            var update = Builders<ProjectHistory>.Update
                .Set(h => h.IsArchived, true);
                
            var result = await Collection.UpdateOneAsync(filter, update);
            
            if (result.ModifiedCount > 0)
            {
                Logger.LogInformation("Successfully archived history entry");
                return true;
            }
            
            Logger.LogWarning("No history entry found to archive");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to archive project history entry");
            return false;
        }
    }

    public async Task<ProjectHistory?> GetLatestHistoryAsync(string projectId)
    {
        try
        {
            return await Collection
                .Find(x => x.ProjectId == projectId && !x.IsArchived)
                .SortByDescending(x => x.Timestamp)
                .Limit(1)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get latest project history");
            return null;
        }
    }
} 