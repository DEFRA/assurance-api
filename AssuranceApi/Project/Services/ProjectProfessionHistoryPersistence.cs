using MongoDB.Driver;
using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;

namespace AssuranceApi.Project.Services;

public class ProjectProfessionHistoryPersistence : MongoService<ProjectProfessionHistory>, IProjectProfessionHistoryPersistence
{
    public ProjectProfessionHistoryPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "projectProfessionHistory", loggerFactory)
    {
        Logger.LogInformation("Initializing ProjectProfessionHistoryPersistence with collection: projectProfessionHistory");
        try 
        {
            var builder = Builders<ProjectProfessionHistory>.IndexKeys;
            var indexes = DefineIndexes(builder);
            foreach (var index in indexes)
            {
                Collection.Indexes.CreateOne(index);
            }
            Logger.LogInformation("Successfully created indexes for projectProfessionHistory collection");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create indexes for projectProfessionHistory collection");
        }
    }

    protected override List<CreateIndexModel<ProjectProfessionHistory>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProjectProfessionHistory> builder)
    {
        return new List<CreateIndexModel<ProjectProfessionHistory>>
        {
            new CreateIndexModel<ProjectProfessionHistory>(
                builder.Ascending(x => x.ProjectId)
                    .Ascending(x => x.ProfessionId)
                    .Ascending(x => x.Timestamp))
        };
    }

    public async Task<bool> CreateAsync(ProjectProfessionHistory history)
    {
        try
        {
            Logger.LogInformation("Creating history entry for project {ProjectId}, profession {ProfessionId}", 
                history.ProjectId, history.ProfessionId);
            await Collection.InsertOneAsync(history);
            Logger.LogInformation("Successfully created history entry");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create profession history for project");
            return false;
        }
    }

    public async Task<IEnumerable<ProjectProfessionHistory>> GetHistoryAsync(string projectId, string professionId)
    {
        try
        {
            Logger.LogInformation("Getting history for project {ProjectId}, profession {ProfessionId}", 
                projectId, professionId);
            var result = await Collection
                .Find(x => x.ProjectId == projectId && x.ProfessionId == professionId)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();
            Logger.LogInformation("Found {Count} history entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get profession history for project");
            return Enumerable.Empty<ProjectProfessionHistory>();
        }
    }

    public async Task<bool> DeleteAllAsync()
    {
        try
        {
            await Collection.DeleteManyAsync(Builders<ProjectProfessionHistory>.Filter.Empty);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete all profession history for projects");
            return false;
        }
    }

    public async Task<bool> ArchiveHistoryEntryAsync(string projectId, string professionId, string historyId)
    {
        try
        {
            Logger.LogInformation("Archiving history entry {HistoryId} for project {ProjectId}, profession {ProfessionId}", 
                historyId, projectId, professionId);
                
            var filter = Builders<ProjectProfessionHistory>.Filter.And(
                Builders<ProjectProfessionHistory>.Filter.Eq(x => x.Id, historyId),
                Builders<ProjectProfessionHistory>.Filter.Eq(x => x.ProjectId, projectId),
                Builders<ProjectProfessionHistory>.Filter.Eq(x => x.ProfessionId, professionId)
            );
            
            var update = Builders<ProjectProfessionHistory>.Update.Set(x => x.Archived, true);
            
            var result = await Collection.UpdateOneAsync(filter, update);
            
            if (result.ModifiedCount == 0)
            {
                Logger.LogWarning("No history entry found to archive with ID {HistoryId}", historyId);
                return false;
            }
            
            Logger.LogInformation("Successfully archived history entry {HistoryId}", historyId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to archive profession history entry {HistoryId}", historyId);
            return false;
        }
    }

    public async Task<ProjectProfessionHistory?> GetLatestHistoryAsync(string projectId, string professionId)
    {
        try
        {
            return await Collection
                .Find(x => x.ProjectId == projectId && x.ProfessionId == professionId && !x.Archived)
                .SortByDescending(x => x.Timestamp)
                .Limit(1)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get latest profession history for project");
            return null;
        }
    }
} 