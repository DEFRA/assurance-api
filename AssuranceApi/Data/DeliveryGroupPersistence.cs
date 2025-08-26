using AssuranceApi.Data.Models;
using AssuranceApi.Utils.Mongo;
using MongoDB.Driver;

namespace AssuranceApi.Data
{
    /// <summary>
    /// Concrete implementation of IDeliveryGroupPersistence using MongoDB.
    /// </summary>
    public class DeliveryGroupPersistence
        : MongoService<DeliveryGroupModel>,
            IDeliveryGroupPersistence
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryGroupPersistence"/> class.
        /// </summary>
        /// <param name="connectionFactory">The factory to create MongoDB client connections.</param>
        /// <param name="loggerFactory">The factory to create loggers.</param>
        public DeliveryGroupPersistence(
            IMongoDbClientFactory connectionFactory,
            ILoggerFactory loggerFactory
        )
            : base(connectionFactory, "deliveryGroups", loggerFactory) { }

        /// <summary>
        /// Defines the indexes for the delivery groups collection.
        /// </summary>
        /// <param name="builder">The index keys definition builder.</param>
        /// <returns>A list of index models to be created on the collection.</returns>
        protected override List<CreateIndexModel<DeliveryGroupModel>> DefineIndexes(
            IndexKeysDefinitionBuilder<DeliveryGroupModel> builder
        )
        {
            return new List<CreateIndexModel<DeliveryGroupModel>>
                {
                    new CreateIndexModel<DeliveryGroupModel>(
                        builder.Ascending(x => x.Name),
                        new CreateIndexOptions { Unique = true }
                    ),
                };
        }

        /// <summary>
        /// Retrieves all delivery groups from the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of delivery groups.</returns>
        public async Task<List<DeliveryGroupModel>> GetAllAsync()
        {
            var findOptions = new FindOptions
            {
                Collation = MongoDbHelpers.GetCaseInsensitiveCollation()
            };

            var filter = Builders<DeliveryGroupModel>.Filter.Empty;

            return await Collection
                .Find(filter, findOptions)
                .Sort(Builders<DeliveryGroupModel>.Sort.Ascending(x => x.Name))
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a delivery group by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery group.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the delivery group if found; otherwise, null.</returns>
        public async Task<DeliveryGroupModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new delivery group in the database.
        /// </summary>
        /// <param name="deliveryGroup">The delivery group model to create.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        public async Task<bool> CreateAsync(DeliveryGroupModel deliveryGroup)
        {
            try
            {
                await Collection.InsertOneAsync(deliveryGroup);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create delivery group");
                return false;
            }
        }

        /// <summary>
        /// Updates an existing delivery group in the database.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery group to update.</param>
        /// <param name="deliveryGroup">The updated delivery group model.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        public async Task<bool> UpdateAsync(string id, DeliveryGroupModel deliveryGroup)
        {
            try
            {
                Logger.LogInformation("Updating delivery group {DeliveryGroupId}", id);

                var result = await Collection.ReplaceOneAsync(x => x.Id == id, deliveryGroup);
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update delivery group {DeliveryGroupId}", id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a delivery group by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery group to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                Logger.LogInformation("Deleting delivery group with ID: {Id}", id);

                var deleteResult = await Collection.DeleteOneAsync(p => p.Id == id);

                if (deleteResult.DeletedCount == 0)
                {
                    Logger.LogWarning("Delivery group with ID {Id} not found for deletion", id);

                    return false;
                }

                Logger.LogInformation("Successfully deleted delivery group with ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting delivery group with ID: {Id}", id);
                return false;
            }
        }
    }
}
