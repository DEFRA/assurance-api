using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Data.Models
{
    /// <summary>
    /// Represents a delivery group in the system.
    /// </summary>
    public class DeliveryGroupModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the delivery group.
        /// </summary>
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the delivery group.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the delivery group is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the status of the delivery group.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Gets or sets the lead of the delivery group.
        /// </summary>
        public string Lead { get; set; } = null!;

        /// <summary>
        /// Gets or sets the date and time when the delivery group was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the delivery group was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
