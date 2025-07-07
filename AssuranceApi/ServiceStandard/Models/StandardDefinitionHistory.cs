using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.ServiceStandard.Models;

/// <summary>
/// Represents the history of changes made to a standard definition.
/// </summary>
public class StandardDefinitionHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the history record.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the standard associated with this history record.
    /// </summary>
    public string StandardId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp of when the change occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user who made the change.
    /// </summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the details of the changes made to the standard definition.
    /// </summary>
    public StandardDefinitionChanges Changes { get; set; } = null!;
}

/// <summary>
/// Represents the changes made to a standard definition.
/// </summary>
public class StandardDefinitionChanges
{
    /// <summary>
    /// Gets or sets the changes made to the name of the standard.
    /// </summary>
    public ServiceStandardNameChange? Name { get; set; }

    /// <summary>
    /// Gets or sets the changes made to the description of the standard.
    /// </summary>
    public ServiceStandardDescriptionChange? Description { get; set; }

    /// <summary>
    /// Gets or sets the changes made to the guidance of the standard.
    /// </summary>
    public GuidanceChange? Guidance { get; set; }
}

/// <summary>
/// Represents a change made to the name of a standard.
/// </summary>
public class ServiceStandardNameChange
{
    /// <summary>
    /// Gets or sets the original name of the standard.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new name of the standard.
    /// </summary>
    public string To { get; set; } = null!;
}

/// <summary>
/// Represents a change made to the description of a standard.
/// </summary>
public class ServiceStandardDescriptionChange
{
    /// <summary>
    /// Gets or sets the original description of the standard.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new description of the standard.
    /// </summary>
    public string To { get; set; } = null!;
}

/// <summary>
/// Represents a change made to the guidance of a standard.
/// </summary>
public class GuidanceChange
{
    /// <summary>
    /// Gets or sets the original guidance of the standard.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new guidance of the standard.
    /// </summary>
    public string To { get; set; } = null!;
}
