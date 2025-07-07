using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Project.Services;

/// <summary>
/// Provides persistence operations for Project Standards in the MongoDB database.
/// </summary>
public class ProjectStandardsPersistence
    : MongoService<ProjectStandards>,
        IProjectStandardsPersistence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectStandardsPersistence"/> class.
    /// </summary>
    /// <param name="connectionFactory">The MongoDB client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ProjectStandardsPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "projectStandards", loggerFactory)
    {
        Logger.LogInformation(
            "Initializing ProjectStandardsPersistence with collection: projectStandards"
        );
    }

    /// <inheritdoc />
    protected override List<CreateIndexModel<ProjectStandards>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProjectStandards> builder
    )
    {
        return new List<CreateIndexModel<ProjectStandards>>
        {
            new CreateIndexModel<ProjectStandards>(
                builder
                    .Ascending(x => x.ProjectId)
                    .Ascending(x => x.StandardId)
                    .Ascending(x => x.ProfessionId),
                new CreateIndexOptions { Unique = true }
            ),
        };
    }

    /// <summary>
    /// Retrieves a specific Project Standard by project, standard, and profession IDs.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <returns>The matching <see cref="ProjectStandards"/> or null if not found.</returns>
    public async Task<ProjectStandards?> GetAsync(
        string projectId,
        string standardId,
        string professionId
    )
    {
        return await Collection
            .Find(x =>
                x.ProjectId == projectId
                && x.StandardId == standardId
                && x.ProfessionId == professionId
            )
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retrieves all Project Standards for a specific project and standard.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <returns>A list of matching <see cref="ProjectStandards"/>.</returns>
    public async Task<List<ProjectStandards>> GetByProjectAndStandardAsync(
        string projectId,
        string standardId
    )
    {
        return await Collection
            .Find(x => x.ProjectId == projectId && x.StandardId == standardId)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves all Project Standards for a specific project.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <returns>A list of matching <see cref="ProjectStandards"/>.</returns>
    public async Task<List<ProjectStandards>> GetByProjectAsync(string projectId)
    {
        return await Collection.Find(x => x.ProjectId == projectId).ToListAsync();
    }

    /// <summary>
    /// Inserts or updates a Project Standard in the database.
    /// </summary>
    /// <param name="assessment">The <see cref="ProjectStandards"/> to upsert.</param>
    public async Task UpsertAsync(ProjectStandards assessment)
    {
        var filter = Builders<ProjectStandards>.Filter.Where(x =>
            x.ProjectId == assessment.ProjectId
            && x.StandardId == assessment.StandardId
            && x.ProfessionId == assessment.ProfessionId
        );
        await Collection.ReplaceOneAsync(
            filter,
            assessment,
            new ReplaceOptions { IsUpsert = true }
        );
    }

    /// <summary>
    /// Deletes a specific Project Standard by project, standard, and profession IDs.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    public async Task<bool> DeleteAsync(string projectId, string standardId, string professionId)
    {
        var filter = Builders<ProjectStandards>.Filter.Where(x =>
            x.ProjectId == projectId && x.StandardId == standardId && x.ProfessionId == professionId
        );
        var result = await Collection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }
}
