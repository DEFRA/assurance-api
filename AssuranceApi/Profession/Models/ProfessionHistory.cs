using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Profession.Models;

/// <summary>
/// Represents the history of changes made to a profession.
/// </summary>
public class ProfessionHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the profession history record.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the profession associated with this history record.
    /// </summary>
    public string ProfessionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp of when the change occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user who made the change.
    /// </summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the details of the changes made to the profession.
    /// </summary>
    public ProfessionChanges Changes { get; set; } = null!;
}

/// <summary>
/// Represents the changes made to a profession.
/// </summary>
public class ProfessionChanges
{
    /// <summary>
    /// Gets or sets the name change details, if applicable.
    /// </summary>
    public NameChange? Name { get; set; }

    /// <summary>
    /// Gets or sets the description change details, if applicable.
    /// </summary>
    public DescriptionChange? Description { get; set; }
}

/// <summary>
/// Represents a change in the name of a profession.
/// </summary>
public class NameChange
{
    /// <summary>
    /// Gets or sets the original name of the profession.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new name of the profession.
    /// </summary>
    public string To { get; set; } = null!;
}

/// <summary>
/// Represents a change in the description of a profession.
/// </summary>
public class DescriptionChange
{
    /// <summary>
    /// Gets or sets the original description of the profession.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new description of the profession.
    /// </summary>
    public string To { get; set; } = null!;
}
