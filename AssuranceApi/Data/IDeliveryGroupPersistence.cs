using AssuranceApi.Data.Models;

namespace AssuranceApi.Data
{
    /// <summary>
    /// Interface for delivery group persistence operations.
    /// Defines the contract for CRUD operations on delivery groups.
    /// </summary>
    public interface IDeliveryGroupPersistence
    {
        /// <summary>
        /// Retrieves all delivery groups from the database.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of delivery groups.</returns>
        Task<List<DeliveryGroupModel>> GetAllAsync();

        /// <summary>
        /// Retrieves a delivery group by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery group.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the delivery group if found; otherwise, null.</returns>
        Task<DeliveryGroupModel?> GetByIdAsync(string id);

        /// <summary>
        /// Creates a new delivery group in the database.
        /// </summary>
        /// <param name="deliveryGroup">The delivery group model to create.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        Task<bool> CreateAsync(DeliveryGroupModel deliveryGroup);

        /// <summary>
        /// Updates an existing delivery group in the database.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery group to update.</param>
        /// <param name="deliveryGroup">The updated delivery group model.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        Task<bool> UpdateAsync(string id, DeliveryGroupModel deliveryGroup);

        /// <summary>
        /// Deletes a delivery group by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the delivery group to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        Task<bool> DeleteAsync(string id);
    }
}
