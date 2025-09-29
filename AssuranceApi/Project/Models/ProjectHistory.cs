using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

/// <summary>
/// Represents the history of changes made to a project.
/// </summary>
public class ProjectHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the project history entry.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the project associated with this history entry.
    /// </summary>
    public string ProjectId { get; set; } = null!;

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
    public Changes Changes { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether this history entry is archived.
    /// </summary>
    public bool IsArchived { get; set; } = false;
}

/// <summary>
/// Represents the changes made to a project.
/// </summary>
public class Changes
{
    /// <summary>
    /// Gets or sets the name change details, if applicable.
    /// </summary>
    public ProjectNameChange? Name { get; set; }

    /// <summary>
    /// Gets or sets the phase change details, if applicable.
    /// </summary>
    public PhaseChange? Phase { get; set; }

    /// <summary>
    /// Gets or sets the status change details, if applicable.
    /// </summary>
    public StatusChange? Status { get; set; }

    /// <summary>
    /// Gets or sets the commentary change details, if applicable.
    /// </summary>
    public CommentaryChange? Commentary { get; set; }

    /// <summary>
    /// Gets or sets the delivery group change details, if applicable.
    /// </summary>
    public DeliveryGroupChange? DeliveryGroup { get; set; }

    /// <summary>
    /// Gets or sets the tags change details, if applicable.
    /// </summary>
    public TagsChange? Tags { get; set; }
}

/// <summary>
/// Represents a change in the project's name.
/// </summary>
public class ProjectNameChange
{
    /// <summary>
    /// Gets or sets the previous name.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new name.
    /// </summary>
    public string To { get; set; } = null!;
}

/// <summary>
/// Represents a change in the project's phase.
/// </summary>
public class PhaseChange
{
    /// <summary>
    /// Gets or sets the previous phase.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new phase.
    /// </summary>
    public string To { get; set; } = null!;
}

/// <summary>
/// Represents a change in the project's status.
/// </summary>
public class StatusChange
{
    /// <summary>
    /// Gets or sets the previous status.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new status.
    /// </summary>
    public string To { get; set; } = null!;
}

/// <summary>
/// Represents a change in the project's commentary.
/// </summary>
public class CommentaryChange
{
    /// <summary>
    /// Gets or sets the previous commentary.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// Gets or sets the new commentary.
    /// </summary>
    public string To { get; set; } = null!;
}

/// <summary>
/// Represents a change in the project's delivery group.
/// </summary>
public class DeliveryGroupChange
{
    /// <summary>
    /// Gets or sets the previous delivery group identifier.
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the new delivery group identifier.
    /// </summary>
    public string? To { get; set; }
}

/// <summary>
/// Represents a change in the project's tags.
/// </summary>
public class TagsChange
{
    /// <summary>
    /// Gets or sets the previous tags.
    /// </summary>
    public List<string> From { get; set; } = new();

    /// <summary>
    /// Gets or sets the new tags.
    /// </summary>
    public List<string> To { get; set; } = new();
}
