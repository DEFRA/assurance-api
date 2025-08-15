using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Data.Models
{
    /// <summary>
    /// Represents a delivery partner in the system.
    /// </summary>
    public class DeliveryPartnerModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the delivery partner.
        /// </summary>
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the delivery partner.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the delivery partner is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the date and time when the delivery partner was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the delivery partnerswe was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
