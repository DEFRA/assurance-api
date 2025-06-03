using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Project.Services;

public class ProjectProfessionStandardAssessmentHistoryPersistence : MongoService<ProjectProfessionStandardAssessmentHistory>, IProjectProfessionStandardAssessmentHistoryPersistence
{
    public ProjectProfessionStandardAssessmentHistoryPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "projectProfessionStandardAssessmentHistory", loggerFactory)
    {
        Logger.LogInformation("Initializing ProjectProfessionStandardAssessmentHistoryPersistence with collection: projectProfessionStandardAssessmentHistory");
        try
        {
            var builder = Builders<ProjectProfessionStandardAssessmentHistory>.IndexKeys;
            var indexes = DefineIndexes(builder);
            foreach (var index in indexes)
            {
                Collection.Indexes.CreateOne(index);
            }
            Logger.LogInformation("Successfully created indexes for projectProfessionStandardAssessmentHistory collection");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create indexes for projectProfessionStandardAssessmentHistory collection");
        }
    }

    protected override List<CreateIndexModel<ProjectProfessionStandardAssessmentHistory>> DefineIndexes(IndexKeysDefinitionBuilder<ProjectProfessionStandardAssessmentHistory> builder)
    {
        return new List<CreateIndexModel<ProjectProfessionStandardAssessmentHistory>>
        {
            new CreateIndexModel<ProjectProfessionStandardAssessmentHistory>(
                builder.Ascending(x => x.ProjectId)
                       .Ascending(x => x.StandardId)
                       .Ascending(x => x.ProfessionId)
                       .Ascending(x => x.Timestamp))
        };
    }

    public async Task<List<ProjectProfessionStandardAssessmentHistory>> GetHistoryAsync(string projectId, string standardId, string professionId)
    {
        return await Collection.Find(x => x.ProjectId == projectId && x.StandardId == standardId && x.ProfessionId == professionId && !x.Archived)
            .SortByDescending(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task AddAsync(ProjectProfessionStandardAssessmentHistory history)
    {
        await Collection.InsertOneAsync(history);
    }

    public async Task<bool> ArchiveAsync(string projectId, string standardId, string professionId, string historyId)
    {
        try
        {
            Logger.LogInformation("Archiving assessment history entry {HistoryId} for project {ProjectId}, standard {StandardId}, profession {ProfessionId}", 
                historyId, projectId, standardId, professionId);
            
            var filter = Builders<ProjectProfessionStandardAssessmentHistory>.Filter.Where(x => 
                x.ProjectId == projectId && 
                x.StandardId == standardId && 
                x.ProfessionId == professionId && 
                x.Id == historyId);
            
            var update = Builders<ProjectProfessionStandardAssessmentHistory>.Update.Set(x => x.Archived, true);
            
            var result = await Collection.UpdateOneAsync(filter, update);
            
            Logger.LogInformation("Archive operation result: {ModifiedCount} documents modified", result.ModifiedCount);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to archive assessment history entry");
            return false;
        }
    }
}
