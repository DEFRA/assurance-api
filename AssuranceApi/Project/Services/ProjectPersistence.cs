using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Project.Services;

public class ProjectPersistence : MongoService<ProjectModel>, IProjectPersistence
{
    public ProjectPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "projects", loggerFactory)
    {
    }

    protected override List<CreateIndexModel<ProjectModel>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProjectModel> builder)
    {
        return new List<CreateIndexModel<ProjectModel>>
        {
            new CreateIndexModel<ProjectModel>(
                builder.Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true })
        };
    }

    public async Task<bool> CreateAsync(ProjectModel project)
    {
        try
        {
            await Collection.InsertOneAsync(project);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create project");
            return false;
        }
    }

    public async Task<List<ProjectModel>> GetAllAsync()
    {
        return await Collection.Find(Builders<ProjectModel>.Filter.Empty)
            .SortBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<ProjectModel?> GetByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(string id, ProjectModel project)
    {
        try
        {
            Logger.LogInformation(
                "Updating project {ProjectId} with {StandardCount} standards",
                id,
                project.Standards?.Count ?? 0
            );

            var result = await Collection.ReplaceOneAsync(
                x => x.Id == id,
                project);

            if (result.ModifiedCount == 0)
            {
                Logger.LogWarning("No project was updated for ID {ProjectId}", id);
            }

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update project {ProjectId}", id);
            return false;
        }
    }

    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(Builders<ProjectModel>.Filter.Empty);
    }
} 