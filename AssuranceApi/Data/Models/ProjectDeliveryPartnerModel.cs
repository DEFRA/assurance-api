using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Data.Models
{
    /// <summary>
    /// Represents the link between a project and a delivery partner, including engagement details.
    /// </summary>
    public class ProjectDeliveryPartnerModel
    {
        /// <summary>
        /// Gets or sets the unique identifier for the project - delivery partner link.
        /// </summary>
        [BsonId]
        [BsonElement("_id")]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Gets or sets the unique identifier for the project.
        /// </summary>
        public string ProjectId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the unique identifier for the delivery partner.
        /// </summary>
        public string DeliveryPartnerId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the engagement manager for the delivery partner.
        /// </summary>
        public string EngagementManager { get; set; } = null!;

        /// <summary>
        /// Gets or sets the date and time when the delivery partner's engagement started with the project.
        /// </summary>
        public DateTime EngagementStarted { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the delivery partner's engagement ended with the project.
        /// Null indicates an active engagement.
        /// </summary>
        public DateTime? EngagementEnded { get; set; } = null;
    }
}
