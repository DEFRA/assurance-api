using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.ServiceStandard.Models;

/// <summary>
/// Represents a service standard model with details such as ID, number, name, description, and audit fields.
/// </summary>
public class ServiceStandardModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the service standard.
    /// </summary>
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number associated with the service standard.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets the name of the service standard.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the service standard.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets the guidance check list of the service standard.
    /// </summary>
    public string Guidance { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the service standard is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the service standard was deleted, if applicable.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the service standard, if applicable.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the service standard was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the service standard was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
