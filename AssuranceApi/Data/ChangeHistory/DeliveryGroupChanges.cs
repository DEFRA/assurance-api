
namespace AssuranceApi.Data.ChangeHistory
{
    /// <summary>
    /// Represents the details of changes made to a delivery group.
    /// </summary>
    public class DeliveryGroupChanges
    {
        /// <summary>
        /// Gets or sets the name change details, if applicable.
        /// </summary>
        public Change<string>? Name { get; set; }

        /// <summary>
        /// Gets or sets the status change details, if applicable.
        /// </summary>
        public Change<string>? Status { get; set; }

        /// <summary>
        /// Gets or sets the name of the lead change details, if applicable.
        /// </summary>
        public Change<string>? Lead { get; set; }

        /// <summary>
        /// Gets or sets the outcome change details, if applicable.
        /// </summary>
        public Change<string>? Outcome { get; set; }

        /// <summary>
        /// Gets or sets the roadmap name change details, if applicable.
        /// </summary>
        public Change<string>? RoadmapName { get; set; }

        /// <summary>
        /// Gets or sets the roadmap link change details, if applicable.
        /// </summary>
        public Change<string>? RoadmapLink { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the delivery Group is active.
        /// </summary>
        public Change<bool>? IsActive { get; set; }
    }
}
