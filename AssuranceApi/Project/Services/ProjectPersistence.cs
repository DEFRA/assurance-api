using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text.Json;

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

    public async Task<List<ProjectModel>> GetAllAsync(string? tag = null)
    {
        var filter = tag == null 
            ? Builders<ProjectModel>.Filter.Empty
            : Builders<ProjectModel>.Filter.AnyEq(p => p.Tags, tag);
        
        return await Collection.Find(filter).ToListAsync();
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
                "Updating project {ProjectId}",
                id
            );

            var updateDef = Builders<ProjectModel>.Update;
            var updates = new List<UpdateDefinition<ProjectModel>>();

            if (project.Name != null)
                updates.Add(updateDef.Set(x => x.Name, project.Name));
            if (project.Phase != null)
                updates.Add(updateDef.Set(x => x.Phase, project.Phase));
            if (project.DefCode != null)
                updates.Add(updateDef.Set(x => x.DefCode, project.DefCode));
            if (project.Status != null)
                updates.Add(updateDef.Set(x => x.Status, project.Status));
            if (project.Commentary != null)
                updates.Add(updateDef.Set(x => x.Commentary, project.Commentary));
            if (project.Tags != null)
                updates.Add(updateDef.Set(x => x.Tags, project.Tags));
            if (project.LastUpdated != null)
                updates.Add(updateDef.Set(x => x.LastUpdated, project.LastUpdated));
            if (project.UpdateDate != null)
                updates.Add(updateDef.Set(x => x.UpdateDate, project.UpdateDate));
            if (project.StandardsSummary != null)
                updates.Add(updateDef.Set(x => x.StandardsSummary, project.StandardsSummary));

            if (!updates.Any())
            {
                Logger.LogWarning("No fields to update for project {ProjectId}", id);
                return false;
            }

            var result = await Collection.UpdateOneAsync(
                x => x.Id == id,
                updateDef.Combine(updates));

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

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            Logger.LogInformation("Deleting project with ID: {Id}", id);
            
            // Delete the project
            var deleteResult = await Collection.DeleteOneAsync(p => p.Id == id);
            
            if (deleteResult.DeletedCount == 0)
            {
                Logger.LogWarning("Project with ID {Id} not found for deletion", id);
                return false;
            }
            
            Logger.LogInformation("Successfully deleted project with ID: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting project with ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> SeedAsync(List<ProjectModel> projects)
    {
        try
        {
            // Insert new projects
            if (projects.Any())
            {
                await Collection.InsertManyAsync(projects);
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to seed projects");
            return false;
        }
    }

    // Add a new method for adding projects without clearing
    public async Task<bool> AddProjectsAsync(List<ProjectModel> projects)
    {
        try
        {
            if (projects.Any())
            {
                await Collection.InsertManyAsync(projects);
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to add projects");
            return false;
        }
    }
}