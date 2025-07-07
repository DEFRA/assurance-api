using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

/// <summary>
/// Represents the history of standards associated with a project.
/// </summary>
public class ProjectStandardsHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the history record.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the project.
    /// </summary>
    public string ProjectId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the profession.
    /// </summary>
    public string ProfessionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the standard.
    /// </summary>
    public string StandardId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp of the change.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who made the change.
    /// </summary>
    public string ChangedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the details of the changes made.
    /// </summary>
    public AssessmentChanges Changes { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the record is archived.
    /// </summary>
    public bool Archived { get; set; } = false;
}

/// <summary>
/// Represents the changes made to an assessment.
/// </summary>
public class AssessmentChanges
{
    /// <summary>
    /// Gets or sets the status change details.
    /// </summary>
    public StatusChange? Status { get; set; }

    /// <summary>
    /// Gets or sets the commentary change details.
    /// </summary>
    public CommentaryChange? Commentary { get; set; }
}
