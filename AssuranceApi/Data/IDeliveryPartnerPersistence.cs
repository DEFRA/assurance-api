using AssuranceApi.Data.Models;
using AssuranceApi.Project.Models;

namespace AssuranceApi.Data
{
    /// <summary>
    /// Interface for managing persistence operations for Delivery Partners.
    /// </summary>
    public interface IDeliveryPartnerPersistence
    {
        /// <summary>
        /// Retrieves all delivery partners.
        /// </summary>
        /// <returns>A list of all delivery partners.</returns>
        Task<List<DeliveryPartnerModel>> GetAllAsync();

        /// <summary>
        /// Retrieves a delivery partner by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery partner.</param>
        /// <returns>The delivery partner if found; otherwise, null.</returns>
        Task<DeliveryPartnerModel?> GetByIdAsync(string id);

        /// <summary>
        /// Creates a new delivery partner.
        /// </summary>
        /// <param name="deliveryPartner">The delivery partner to create.</param>
        /// <returns>True if the creation was successful; otherwise, false.</returns>
        Task<bool> CreateAsync(DeliveryPartnerModel deliveryPartner);

        /// <summary>
        /// Updates an existing delivery partner.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery partner to update.</param>
        /// <param name="deliveryPartner">The updated delivery partner details.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        Task<bool> UpdateAsync(string id, DeliveryPartnerModel deliveryPartner);

        /// <summary>
        /// Deletes a delivery partner by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery partner to delete.</param>
        /// <returns>True if the deletion was successful; otherwise, false.</returns>
        Task<bool> DeleteAsync(string id);
    }
}
