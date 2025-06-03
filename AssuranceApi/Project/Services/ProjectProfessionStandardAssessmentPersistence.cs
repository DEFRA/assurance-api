using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Project.Services;

public class ProjectProfessionStandardAssessmentPersistence : MongoService<ProjectProfessionStandardAssessment>, IProjectProfessionStandardAssessmentPersistence
{
    public ProjectProfessionStandardAssessmentPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "projectProfessionStandardAssessments", loggerFactory)
    {
        Logger.LogInformation("Initializing ProjectProfessionStandardAssessmentPersistence with collection: projectProfessionStandardAssessments");
        try
        {
            var builder = Builders<ProjectProfessionStandardAssessment>.IndexKeys;
            var indexes = DefineIndexes(builder);
            foreach (var index in indexes)
            {
                Collection.Indexes.CreateOne(index);
            }
            Logger.LogInformation("Successfully created indexes for projectProfessionStandardAssessments collection");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create indexes for projectProfessionStandardAssessments collection");
        }
    }

    protected override List<CreateIndexModel<ProjectProfessionStandardAssessment>> DefineIndexes(IndexKeysDefinitionBuilder<ProjectProfessionStandardAssessment> builder)
    {
        return new List<CreateIndexModel<ProjectProfessionStandardAssessment>>
        {
            new CreateIndexModel<ProjectProfessionStandardAssessment>(
                builder.Ascending(x => x.ProjectId)
                       .Ascending(x => x.StandardId)
                       .Ascending(x => x.ProfessionId),
                new CreateIndexOptions { Unique = true })
        };
    }

    public async Task<ProjectProfessionStandardAssessment?> GetAsync(string projectId, string standardId, string professionId)
    {
        return await Collection.Find(x => x.ProjectId == projectId && x.StandardId == standardId && x.ProfessionId == professionId).FirstOrDefaultAsync();
    }

    public async Task<List<ProjectProfessionStandardAssessment>> GetByProjectAndStandardAsync(string projectId, string standardId)
    {
        return await Collection.Find(x => x.ProjectId == projectId && x.StandardId == standardId).ToListAsync();
    }

    public async Task<List<ProjectProfessionStandardAssessment>> GetByProjectAsync(string projectId)
    {
        return await Collection.Find(x => x.ProjectId == projectId).ToListAsync();
    }

    public async Task UpsertAsync(ProjectProfessionStandardAssessment assessment)
    {
        var filter = Builders<ProjectProfessionStandardAssessment>.Filter.Where(x => x.ProjectId == assessment.ProjectId && x.StandardId == assessment.StandardId && x.ProfessionId == assessment.ProfessionId);
        await Collection.ReplaceOneAsync(filter, assessment, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<bool> DeleteAsync(string projectId, string standardId, string professionId)
    {
        try
        {
            var filter = Builders<ProjectProfessionStandardAssessment>.Filter.Where(x => 
                x.ProjectId == projectId && 
                x.StandardId == standardId && 
                x.ProfessionId == professionId);
            
            var result = await Collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete assessment for project {ProjectId}, standard {StandardId}, profession {ProfessionId}", 
                projectId, standardId, professionId);
            return false;
        }
    }
}
