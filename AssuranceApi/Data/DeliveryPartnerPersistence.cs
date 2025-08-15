using AssuranceApi.Data.Models;
using AssuranceApi.Utils.Mongo;
using MongoDB.Driver;

namespace AssuranceApi.Data
{
    /// <summary>
    /// Concrete implementation of IDeliveryPartnerPersistence using MongoDB.
    /// </summary>
    public class DeliveryPartnerPersistence : MongoService<DeliveryPartnerModel>, IDeliveryPartnerPersistence
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryPartnerPersistence"/> class.
        /// </summary>
        /// <param name="connectionFactory">The factory to create MongoDB client connections.</param>
        /// <param name="loggerFactory">The factory to create loggers.</param>
        public DeliveryPartnerPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
            : base(connectionFactory, "deliveryPartners", loggerFactory) { }

        protected override List<CreateIndexModel<DeliveryPartnerModel>> DefineIndexes(
            IndexKeysDefinitionBuilder<DeliveryPartnerModel> builder
        )
        {
            return new List<CreateIndexModel<DeliveryPartnerModel>>
                {
                    new CreateIndexModel<DeliveryPartnerModel>(
                        builder.Ascending(x => x.Name),
                        new CreateIndexOptions { Unique = true }
                    ),
                };
        }

        /// <summary>
        /// Retrieves all delivery partners from the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of delivery partners.</returns>
        public async Task<List<DeliveryPartnerModel>> GetAllAsync()
        {
            var findOptions = new FindOptions
            {
                Collation = MongoDbHelpers.GetCaseInsensitiveCollation()
            };

            var filter = Builders<DeliveryPartnerModel>.Filter.Empty;

            return await Collection
                .Find(filter, findOptions)
                .Sort(Builders<DeliveryPartnerModel>.Sort.Ascending(x => x.Name))
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a delivery partner by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery partner.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the delivery partner if found; otherwise, null.</returns>
        public async Task<DeliveryPartnerModel?> GetByIdAsync(string id)
        {
            return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new delivery partner in the database.
        /// </summary>
        /// <param name="deliveryPartner">The delivery partner model to create.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        public async Task<bool> CreateAsync(DeliveryPartnerModel deliveryPartner)
        {
            try
            {
                await Collection.InsertOneAsync(deliveryPartner);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create delivery partner");
                return false;
            }
        }

        /// <summary>
        /// Updates an existing delivery partner in the database.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery partner to update.</param>
        /// <param name="deliveryPartner">The updated delivery partner model.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        public async Task<bool> UpdateAsync(string id, DeliveryPartnerModel deliveryPartner)
        {
            try
            {
                Logger.LogInformation("Updating delivery partner {DeliveryPartnerId}", id);

                var result = await Collection.ReplaceOneAsync(x => x.Id == id, deliveryPartner);
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update delivery partner {DeliveryPartnerId}", id);
                return false;
            }
        }

        /// <summary>
        /// Deletes a delivery partner by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery partner to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                Logger.LogInformation("Deleting delivery partner with ID: {Id}", id);

                var deleteResult = await Collection.DeleteOneAsync(p => p.Id == id);

                if (deleteResult.DeletedCount == 0)
                {
                    Logger.LogWarning("Delivery partner with ID {Id} not found for deletion", id);

                    return false;
                }

                Logger.LogInformation("Successfully deleted delivery partner with ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting delivery partner with ID: {Id}", id);
                throw;
            }
        }
    }
}
