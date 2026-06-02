using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

/// <summary>
/// Represents a project with details such as name, status, commentary, and associated metadata.
/// </summary>
public class ProjectModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the project.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status of the project. Possible values: RED, AMBER_RED, AMBER, GREEN_AMBER, GREEN, TBC/PENDING, EXCLUDED.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the last updated timestamp for the project. This field is managed automatically by the backend.
    /// </summary>
    public string? LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the commentary or description of the project.
    /// </summary>
    public string Commentary { get; set; } = null!;

    /// <summary>
    /// Gets or sets the GDS phase of the project (e.g., Discovery, Alpha, Beta, Live). This field is optional.
    /// </summary>
    public string? Phase { get; set; }

    /// <summary>
    /// Gets or sets the DEFRA project identifier. This field is optional.
    /// </summary>
    public string? DefCode { get; set; }

    /// <summary>
    /// Gets or sets the delivery group identifier associated with the project. This field is optional.
    /// </summary>
    public string? DeliveryGroupId { get; set; }

    /// <summary>
    /// Gets or sets the list of tags associated with the project.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the update date for the project.
    /// </summary>
    public string? UpdateDate { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the project was last reviewed.
    /// </summary>
    public DateTime? LastReviewed { get; set; }

    /// <summary>
    /// Gets or sets the identity of the user who last reviewed the project.
    /// </summary>
    public string? LastReviewedBy { get; set; }

    /// <summary>
    /// Gets or sets the next date when the project should be reviewed.
    /// </summary>
    public DateTime? NextReviewDate { get; set; }

    /// <summary>
    /// Gets or sets the next service assessment date for the project.
    /// </summary>
    public DateTime? NextServiceAssessmentDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the project is archived.
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Gets or sets the timestamp when the project was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Gets or sets the identity of the user who archived the project.
    /// </summary>
    public string? ArchivedBy { get; set; }

    /// <summary>
    /// Gets or sets the reason the project was archived.
    /// </summary>
    public string? ArchiveReason { get; set; }

    /// <summary>
    /// Gets or sets the summary of standards associated with the project.
    /// </summary>
    public List<StandardSummaryModel> StandardsSummary { get; set; } = new();

    /// <summary>
    /// Gets or sets the summary of standards associated with the project.
    /// </summary>
    [BsonIgnore]
    public ProjectStatus? ProjectStatus { get; set; }
}

/// <summary>
/// Request body for recording a project review.
/// </summary>
public class ProjectReviewModel
{
    /// <summary>
    /// Gets or sets the next date when the project should be reviewed.
    /// </summary>
    public DateTime? NextReviewDate { get; set; }
}

/// <summary>
/// Request body for archiving a project.
/// </summary>
public class ProjectArchiveModel
{
    /// <summary>
    /// Gets or sets the reason the project is being archived.
    /// </summary>
    public string? ArchiveReason { get; set; }
}
