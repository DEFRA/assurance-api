using AssuranceApi.Utils.Mongo;
using MongoDB.Driver;

namespace AssuranceApi.Data.ChangeHistory
{
    /// <summary>
    /// Provides persistence for delivery group history in the MongoDB database.
    /// </summary>
    public class DeliveryGroupHistoryPersistence : MongoService<History<DeliveryGroupChanges>>, IHistoryPersistence<DeliveryGroupChanges>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryGroupHistoryPersistence"/> class with the specified MongoDB client factory and logger factory.
        /// </summary>
        /// <param name="connectionFactory">The MongoDB client factory to use for database connections.</param>
        /// <param name="loggerFactory">The logger factory to use for logging operations.</param>
        public DeliveryGroupHistoryPersistence(
            IMongoDbClientFactory connectionFactory,
            ILoggerFactory loggerFactory
        )
            : base(connectionFactory, "deliveryGroupHistory", loggerFactory)
        {
            Logger.LogInformation(
                "Initializing DeliveryGroupHistoryPersistence with collection: deliveryGroupHistory"
            );
            try
            {
                // Create the index
                var builder = Builders<History<DeliveryGroupChanges>>.IndexKeys;
                var indexes = DefineIndexes(builder);
                foreach (var index in indexes)
                {
                    Collection.Indexes.CreateOne(index);
                }
                Logger.LogInformation("Successfully created indexes for deliveryGroupHistory collection");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create indexes for deliveryGroupHistory collection");
            }
        }

        /// <summary>
        /// Defines the indexes for the ProjectHistory collection.
        /// </summary>
        /// <param name="builder">The index keys definition builder.</param>
        /// <returns>A list of CreateIndexModel objects representing the indexes to be created.</returns>
        protected override List<CreateIndexModel<History<DeliveryGroupChanges>>> DefineIndexes(
            IndexKeysDefinitionBuilder<History<DeliveryGroupChanges>> builder
        )
        {
            return new List<CreateIndexModel<History<DeliveryGroupChanges>>>
        {
            new CreateIndexModel<History<DeliveryGroupChanges>>(
                builder.Ascending(x => x.ItemId).Ascending(x => x.Timestamp)
            ),
        };
        }

        /// <inheritdoc />
        public async Task<bool> CreateAsync(History<DeliveryGroupChanges> history)
        {
            try
            {
                Logger.LogInformation($"Creating history entry for DeliveryGroup {history.ItemId}");
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

        /// <inheritdoc />
        public async Task<IEnumerable<History<DeliveryGroupChanges>>> GetHistoryAsync(string id)
        {
            try
            {
                Logger.LogInformation($"Getting history for delivery group {id}");
                var result = await Collection
                    .Find(x => x.ItemId == id && !x.IsArchived)
                    .SortByDescending(x => x.Timestamp)
                    .ToListAsync();
                Logger.LogInformation($"Found {result.Count} history entries");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get project history");
                return Enumerable.Empty<History<DeliveryGroupChanges>>();
            }
        }
    }
}
