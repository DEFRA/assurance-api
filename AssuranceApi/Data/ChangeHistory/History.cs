using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace AssuranceApi.Data.ChangeHistory
{
    /// <summary>
    /// Represents the history of changes made to an item.
    /// </summary>
    public class History<T>
    {
        /// <summary>
        /// Gets or sets the unique identifier for the history entry.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Gets or sets the identifier of the item associated with this history entry.
        /// </summary>
        public string ItemId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the timestamp of when the change occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the identity of the user who made the change.
        /// </summary>
        public string ChangedBy { get; set; } = null!;

        /// <summary>
        /// Gets or sets the full name of the user who made the change.
        /// </summary>
        public string ChangedByName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the details of the changes made.
        /// </summary>
        public T? Changes { get; set; } = default;

        /// <summary>
        /// Gets or sets a value indicating whether this history entry is archived.
        /// </summary>
        public bool IsArchived { get; set; } = false;
    }
}
