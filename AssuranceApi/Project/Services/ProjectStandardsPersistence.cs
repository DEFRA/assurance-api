using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Project.Services;

public class ProjectStandardsPersistence : MongoService<ProjectStandards>, IProjectStandardsPersistence
{
    public ProjectStandardsPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "projectStandards", loggerFactory)
    {
        Logger.LogInformation("Initializing ProjectStandardsPersistence with collection: projectStandards");
    }

    protected override List<CreateIndexModel<ProjectStandards>> DefineIndexes(IndexKeysDefinitionBuilder<ProjectStandards> builder)
    {
        return new List<CreateIndexModel<ProjectStandards>>
        {
            new CreateIndexModel<ProjectStandards>(
                builder.Ascending(x => x.ProjectId)
                    .Ascending(x => x.StandardId)
                    .Ascending(x => x.ProfessionId),
                new CreateIndexOptions { Unique = true })
        };
    }

    public async Task<ProjectStandards?> GetAsync(string projectId, string standardId, string professionId)
    {
        return await Collection.Find(x => x.ProjectId == projectId && x.StandardId == standardId && x.ProfessionId == professionId).FirstOrDefaultAsync();
    }

    public async Task<List<ProjectStandards>> GetByProjectAndStandardAsync(string projectId, string standardId)
    {
        return await Collection.Find(x => x.ProjectId == projectId && x.StandardId == standardId).ToListAsync();
    }

    public async Task<List<ProjectStandards>> GetByProjectAsync(string projectId)
    {
        return await Collection.Find(x => x.ProjectId == projectId).ToListAsync();
    }

    public async Task UpsertAsync(ProjectStandards assessment)
    {
        var filter = Builders<ProjectStandards>.Filter.Where(x => x.ProjectId == assessment.ProjectId && x.StandardId == assessment.StandardId && x.ProfessionId == assessment.ProfessionId);
        await Collection.ReplaceOneAsync(filter, assessment, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<bool> DeleteAsync(string projectId, string standardId, string professionId)
    {
        var filter = Builders<ProjectStandards>.Filter.Where(x =>
            x.ProjectId == projectId &&
            x.StandardId == standardId &&
            x.ProfessionId == professionId);
        var result = await Collection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }
}
