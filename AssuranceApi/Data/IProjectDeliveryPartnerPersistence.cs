using AssuranceApi.Data.Models;

namespace AssuranceApi.Data
{
    /// <summary>
    /// Interface for managing persistence operations related to Project Delivery Partners.
    /// </summary>
    public interface IProjectDeliveryPartnerPersistence
    {
        /// <summary>
        /// Retrieves a specific Project Delivery Partner by project ID and delivery partner ID.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="deliveryPartnerId">The ID of the delivery partner.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the ProjectDeliveryPartnerModel if found, otherwise null.</returns>
        Task<ProjectDeliveryPartnerModel?> GetAsync(string projectId, string deliveryPartnerId);

        /// <summary>
        /// Retrieves all Project Delivery Partners associated with a specific project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of ProjectDeliveryPartnerModel objects.</returns>
        Task<List<ProjectDeliveryPartnerModel>> GetByProjectAsync(string projectId);

        /// <summary>
        /// Inserts or updates a Project Delivery Partner record.
        /// </summary>
        /// <param name="projectDeliveryPartner">The ProjectDeliveryPartnerModel object to upsert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> UpsertAsync(ProjectDeliveryPartnerModel projectDeliveryPartner);

        /// <summary>
        /// Deletes a specific Project Delivery Partner by project ID and delivery partner ID.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="deliveryPartnerId">The ID of the delivery partner.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        Task<bool> DeleteAsync(string projectId, string deliveryPartnerId);
    }
}
