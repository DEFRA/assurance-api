using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Data;

/// <summary>
/// Provides persistence operations for Project Standards History in the MongoDB database.
/// </summary>
public class ProjectStandardsHistoryPersistence
    : MongoService<ProjectStandardsHistory>,
        IProjectStandardsHistoryPersistence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectStandardsHistoryPersistence"/> class.
    /// </summary>
    /// <param name="connectionFactory">The MongoDB client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ProjectStandardsHistoryPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "projectStandardsHistory", loggerFactory)
    {
        Logger.LogInformation(
            "Initializing ProjectStandardsHistoryPersistence with collection: projectStandardsHistory"
        );
    }

    /// <summary>
    /// Defines the indexes for the Project Standards History collection.
    /// </summary>
    /// <param name="builder">The index keys definition builder.</param>
    /// <returns>A list of index models.</returns>
    protected override List<CreateIndexModel<ProjectStandardsHistory>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProjectStandardsHistory> builder
    )
    {
        return new List<CreateIndexModel<ProjectStandardsHistory>>
        {
            new CreateIndexModel<ProjectStandardsHistory>(
                builder
                    .Ascending(x => x.ProjectId)
                    .Ascending(x => x.StandardId)
                    .Ascending(x => x.ProfessionId)
                    .Descending(x => x.Timestamp)
            ),
        };
    }

    /// <summary>
    /// Retrieves the history of project standards based on the specified parameters.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <returns>A list of project standards history records.</returns>
    public async Task<List<ProjectStandardsHistory>> GetHistoryAsync(
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
                && !x.Archived
            )
            .SortByDescending(x => x.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new project standards history record to the collection.
    /// </summary>
    /// <param name="history">The project standards history record to add.</param>
    public async Task AddAsync(ProjectStandardsHistory history)
    {
        await Collection.InsertOneAsync(history);
    }

    /// <summary>
    /// Archives a specific project standards history record.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <param name="historyId">The history record ID.</param>
    /// <returns>True if the record was successfully archived; otherwise, false.</returns>
    public async Task<bool> ArchiveAsync(
        string projectId,
        string standardId,
        string professionId,
        string historyId
    )
    {
        var filter = Builders<ProjectStandardsHistory>.Filter.Where(x =>
            x.ProjectId == projectId
            && x.StandardId == standardId
            && x.ProfessionId == professionId
            && x.Id == historyId
        );

        var update = Builders<ProjectStandardsHistory>.Update.Set(x => x.Archived, true);
        var result = await Collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }
}
