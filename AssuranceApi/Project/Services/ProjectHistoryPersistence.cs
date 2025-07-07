using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using MongoDB.Driver;

namespace AssuranceApi.Project.Services;

/// <summary>
/// Provides persistence operations for project history, including creating, retrieving, and archiving history entries.
/// </summary>
public class ProjectHistoryPersistence : MongoService<ProjectHistory>, IProjectHistoryPersistence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectHistoryPersistence"/> class with the specified MongoDB client factory and logger factory.
    /// </summary>
    /// <param name="connectionFactory">The MongoDB client factory to use for database connections.</param>
    /// <param name="loggerFactory">The logger factory to use for logging operations.</param>
    public ProjectHistoryPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "projectHistory", loggerFactory)
    {
        Logger.LogInformation(
            "Initializing ProjectHistoryPersistence with collection: projectHistory"
        );
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

    
    /// <summary>
    /// Defines the indexes for the ProjectHistory collection.
    /// </summary>
    /// <param name="builder">The index keys definition builder.</param>
    /// <returns>A list of CreateIndexModel objects representing the indexes to be created.</returns>
    protected override List<CreateIndexModel<ProjectHistory>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProjectHistory> builder
    )
    {
        return new List<CreateIndexModel<ProjectHistory>>
        {
            new CreateIndexModel<ProjectHistory>(
                builder.Ascending(x => x.ProjectId).Ascending(x => x.Timestamp)
            ),
        };
    }

    
    /// <summary>
    /// Creates a new project history entry in the database.
    /// </summary>
    /// <param name="history">The project history entry to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the operation was successful.</returns>
    public async Task<bool> CreateAsync(ProjectHistory history)
    {
        try
        {
            Logger.LogInformation(
                "Creating history entry for project {ProjectId}",
                history.ProjectId
            );
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

    
    /// <summary>
    /// Retrieves the history entries for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project whose history is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of project history entries.</returns>
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

    
    /// <summary>
    /// Deletes all project history entries from the database.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteAllAsync()
    {
        await Collection.DeleteManyAsync(Builders<ProjectHistory>.Filter.Empty);
    }

    
    /// <summary>
    /// Archives a specific project history entry by marking it as archived.
    /// </summary>
    /// <param name="projectId">The ID of the project to which the history entry belongs.</param>
    /// <param name="historyId">The ID of the history entry to archive.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the operation was successful.</returns>
    public async Task<bool> ArchiveHistoryEntryAsync(string projectId, string historyId)
    {
        try
        {
            Logger.LogInformation(
                "Archiving history entry {HistoryId} for project {ProjectId}",
                historyId,
                projectId
            );

            var filter = Builders<ProjectHistory>.Filter.And(
                Builders<ProjectHistory>.Filter.Eq(h => h.Id, historyId),
                Builders<ProjectHistory>.Filter.Eq(h => h.ProjectId, projectId)
            );

            var update = Builders<ProjectHistory>.Update.Set(h => h.IsArchived, true);

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

    
    /// <summary>
    /// Retrieves the latest history entry for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project whose latest history entry is to be retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the latest
    /// project history entry, or null if no entry exists.
    /// </returns>
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
