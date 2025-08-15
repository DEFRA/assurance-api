using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using MongoDB.Driver;

namespace AssuranceApi.Data;

/// <summary>
/// Provides persistence operations for ProjectModel entities in a MongoDB collection.
/// </summary>
public class ProjectPersistence : MongoService<ProjectModel>, IProjectPersistence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectPersistence"/> class.
    /// </summary>
    /// <param name="connectionFactory">The factory to create MongoDB client connections.</param>
    /// <param name="loggerFactory">The factory to create loggers.</param>
    public ProjectPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "projects", loggerFactory) { }

    /// <summary>
    /// Defines the indexes for the ProjectModel collection.
    /// </summary>
    /// <param name="builder">The index keys definition builder.</param>
    /// <returns>A list of index models to be created.</returns>
    protected override List<CreateIndexModel<ProjectModel>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProjectModel> builder
    )
    {
        return new List<CreateIndexModel<ProjectModel>>
        {
            new CreateIndexModel<ProjectModel>(
                builder.Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true }
            ),
        };
    }

    /// <summary>
    /// Creates a new project in the database.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>True if the project was created successfully; otherwise, false.</returns>
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

    /// <summary>
    /// Retrieves all projects, optionally filtered by a tag and date range.
    /// </summary>
    /// <param name="projectQueryParameters">The optional parameters to filter projects.</param>
    /// <returns>A list of projects.</returns>
    public async Task<List<ProjectModel>> GetAllAsync(ProjectQueryParameters projectQueryParameters)
    {
        var filter = Builders<ProjectModel>.Filter.Empty;

        if (projectQueryParameters != null)
        {
            var filters = new List<FilterDefinition<ProjectModel>>();

            if (projectQueryParameters.Tags != null)
            {
                filters.Add(Builders<ProjectModel>.Filter.AnyEq(p => p.Tags, projectQueryParameters.Tags));
            }

            if (projectQueryParameters.StartDate != null)
            {
                filters.Add(Builders<ProjectModel>.Filter.Gte(p => p.LastUpdated, projectQueryParameters.StartDate.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
            }
            if (projectQueryParameters.EndDate != null)
            {
                filters.Add(Builders<ProjectModel>.Filter.Lt(p => p.LastUpdated, projectQueryParameters.EndDate.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
            }

            if (filters.Any())
                filter = Builders<ProjectModel>.Filter.And(filters);
        }

        var findOptions = new FindOptions
        {
            Collation = MongoDbHelpers.GetCaseInsensitiveCollation()
        };

        return await Collection
            .Find(filter, findOptions)
            .Sort(Builders<ProjectModel>.Sort.Ascending(x => x.Name))
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a project by its ID.
    /// </summary>
    /// <param name="id">The ID of the project to retrieve.</param>
    /// <returns>The project with the specified ID, or null if not found.</returns>
    public async Task<ProjectModel?> GetByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Updates an existing project in the database.
    /// </summary>
    /// <param name="id">The ID of the project to update.</param>
    /// <param name="project">The updated project data.</param>
    /// <returns>True if the project was updated successfully; otherwise, false.</returns>
    public async Task<bool> UpdateAsync(string id, ProjectModel project)
    {
        try
        {
            Logger.LogInformation("Updating project {ProjectId}", id);

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
                updateDef.Combine(updates)
            );

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

    /// <summary>
    /// Deletes all projects from the database.
    /// </summary>
    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(Builders<ProjectModel>.Filter.Empty);
    }

    /// <summary>
    /// Deletes a project by its ID.
    /// </summary>
    /// <param name="id">The ID of the project to delete.</param>
    /// <returns>True if the project was deleted successfully; otherwise, false.</returns>
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

    /// <summary>
    /// Seeds the database with a list of projects.
    /// </summary>
    /// <param name="projects">The list of projects to seed.</param>
    /// <returns>True if the seeding was successful; otherwise, false.</returns>
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

    /// <summary>
    /// Adds a list of projects to the database without clearing existing data.
    /// </summary>
    /// <param name="projects">The list of projects to add.</param>
    /// <returns>True if the projects were added successfully; otherwise, false.</returns>
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
