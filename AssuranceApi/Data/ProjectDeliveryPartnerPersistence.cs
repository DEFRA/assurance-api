using AssuranceApi.Data.Models;
using AssuranceApi.Utils.Mongo;
using MongoDB.Driver;

namespace AssuranceApi.Data
{
    /// <summary>
    /// Provides persistence operations for Project Delivery Partner data.
    /// </summary>
    public class ProjectDeliveryPartnerPersistence
        : MongoService<ProjectDeliveryPartnerModel>,
        IProjectDeliveryPartnerPersistence
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectDeliveryPartnerPersistence"/> class.
        /// </summary>
        /// <param name="connectionFactory">The factory to create MongoDB client connections.</param>
        /// <param name="loggerFactory">The factory to create loggers.</param>
        public ProjectDeliveryPartnerPersistence(
            IMongoDbClientFactory connectionFactory,
            ILoggerFactory loggerFactory
        )
            : base(connectionFactory, "projectDeliveryPartners", loggerFactory)
        {
            Logger.LogInformation(
                "Initializing ProjectDeliveryPartnerPersistence with collection: projectDeliveryPartners"
            );
        }

        /// <summary>
        /// Defines the indexes for the Project Delivery Partner collection.
        /// </summary>
        /// <param name="builder">The index keys definition builder.</param>
        /// <returns>A list of index models to be created.</returns>
        protected override List<CreateIndexModel<ProjectDeliveryPartnerModel>> DefineIndexes(IndexKeysDefinitionBuilder<ProjectDeliveryPartnerModel> builder)
        {
            return
                [
                    new CreateIndexModel<ProjectDeliveryPartnerModel>(
                        builder.Ascending(x => x.ProjectId).Ascending(x => x.DeliveryPartnerId),
                        new CreateIndexOptions { Unique = true }
                    )
                ];
        }

        /// <summary>
        /// Retrieves a specific Project Delivery Partner by project ID and delivery partner ID.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="deliveryPartnerId">The ID of the delivery partner.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ProjectDeliveryPartnerModel if found, otherwise null.</returns>
        public async Task<ProjectDeliveryPartnerModel?> GetAsync(string projectId, string deliveryPartnerId)
        {
            Logger.LogDebug($"Getting project delivery partner with Project ID: {projectId} AND Delivery Partner ID: {deliveryPartnerId}");

            try
            {
                return await Collection
                    .Find(x =>
                        x.ProjectId == projectId
                        && x.DeliveryPartnerId == deliveryPartnerId
                    )
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get the Project Delivery Partner");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all Project Delivery Partners associated with a specific project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of ProjectDeliveryPartnerModel objects.</returns>
        public async Task<List<ProjectDeliveryPartnerModel>> GetByProjectAsync(string projectId)
        {
            Logger.LogDebug($"Getting project delivery partners with Project ID: {projectId}");

            try
            {
                return await Collection
                    .Find(x => x.ProjectId == projectId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get the Project Delivery Partner");
                throw;
            }
        }

        /// <summary>
        /// Inserts or updates a Project Delivery Partner record.
        /// </summary>
        /// <param name="projectDeliveryPartner">The ProjectDeliveryPartnerModel object to upsert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<bool> UpsertAsync(ProjectDeliveryPartnerModel projectDeliveryPartner)
        {
            Logger.LogDebug($"Entering upsert project delivery partner with Project ID: {projectDeliveryPartner.ProjectId} AND Delivery Partner ID: {projectDeliveryPartner.DeliveryPartnerId}");

            try
            {
                var filter = Builders<ProjectDeliveryPartnerModel>.Filter.Where(x =>
                x.ProjectId == projectDeliveryPartner.ProjectId
                && x.DeliveryPartnerId == projectDeliveryPartner.DeliveryPartnerId
            );
                var result = await Collection.ReplaceOneAsync(
                    filter,
                    projectDeliveryPartner,
                    new ReplaceOptions { IsUpsert = true }
                );
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to update project delivery partner with Project ID: {projectDeliveryPartner.ProjectId} AND Delivery Partner ID: {projectDeliveryPartner.DeliveryPartnerId}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a specific Project Delivery Partner by project ID and delivery partner ID.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="deliveryPartnerId">The ID of the delivery partner.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        public async Task<bool> DeleteAsync(string projectId, string deliveryPartnerId)
        {
            Logger.LogInformation($"Deleting project delivery partner with Project ID: {projectId} AND Delivery Partner ID: {deliveryPartnerId}");

            try
            {
                var deleteResult = await Collection.DeleteOneAsync(
                    pdp => pdp.ProjectId == projectId
                    && pdp.DeliveryPartnerId == deliveryPartnerId);

                if (deleteResult.DeletedCount == 0)
                {
                    Logger.LogWarning($"Project with Project ID: {projectId} AND Delivery Partner ID: {deliveryPartnerId} not found for deletion");
                    return false;
                }

                Logger.LogInformation($"Successfully deleted Project ID: {projectId} AND Delivery Partner ID: {deliveryPartnerId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error deleting Project ID: {projectId} AND Delivery Partner ID: {deliveryPartnerId}");
                throw;
            }
        }
    }
}
