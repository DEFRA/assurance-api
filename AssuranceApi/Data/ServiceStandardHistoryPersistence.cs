using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Data;

/// <summary>
/// Provides persistence operations for service standard history, including creating and retrieving history entries.
/// </summary>
public class ServiceStandardHistoryPersistence
    : MongoService<ServiceStandardHistory>,
        IServiceStandardHistoryPersistence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceStandardHistoryPersistence"/> class.
    /// </summary>
    /// <param name="connectionFactory">The factory to create MongoDB client connections.</param>
    /// <param name="loggerFactory">The factory to create loggers.</param>
    public ServiceStandardHistoryPersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "serviceStandardHistory", loggerFactory)
    {
        Logger.LogInformation(
            "Initializing ServiceStandardHistoryPersistence with collection: serviceStandardHistory"
        );
        try
        {
            var builder = Builders<ServiceStandardHistory>.IndexKeys;
            var indexes = DefineIndexes(builder);
            foreach (var index in indexes)
            {
                Collection.Indexes.CreateOne(index);
            }
            Logger.LogInformation(
                "Successfully created indexes for serviceStandardHistory collection"
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create indexes for serviceStandardHistory collection");
        }
    }

    /// <summary>
    /// Defines the indexes for the service standard history collection.
    /// </summary>
    /// <param name="builder">The index key definition builder.</param>
    /// <returns>A list of index models to be created.</returns>
    protected override List<CreateIndexModel<ServiceStandardHistory>> DefineIndexes(
        IndexKeysDefinitionBuilder<ServiceStandardHistory> builder
    )
    {
        return new List<CreateIndexModel<ServiceStandardHistory>>
        {
            new CreateIndexModel<ServiceStandardHistory>(
                builder.Ascending(x => x.StandardId).Ascending(x => x.Timestamp)
            ),
        };
    }

    /// <summary>
    /// Creates a new history entry for a service standard.
    /// </summary>
    /// <param name="history">The history entry to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    public async Task<bool> CreateAsync(ServiceStandardHistory history)
    {
        try
        {
            Logger.LogInformation(
                "Creating history entry for standard {StandardId}",
                history.StandardId
            );
            await Collection.InsertOneAsync(history);
            Logger.LogInformation("Successfully created history entry");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create service standard history");
            return false;
        }
    }

    /// <summary>
    /// Retrieves the history entries for a specific service standard.
    /// </summary>
    /// <param name="standardId">The ID of the service standard.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of history entries.</returns>
    public async Task<IEnumerable<ServiceStandardHistory>> GetHistoryAsync(string standardId)
    {
        try
        {
            Logger.LogInformation("Getting history for standard {StandardId}", standardId);
            var result = await Collection
                .Find(x => x.StandardId == standardId)
                .SortByDescending(x => x.Timestamp)
                .ToListAsync();
            Logger.LogInformation("Found {Count} history entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get service standard history");
            return Enumerable.Empty<ServiceStandardHistory>();
        }
    }
}
