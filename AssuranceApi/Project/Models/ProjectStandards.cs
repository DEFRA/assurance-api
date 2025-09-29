using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

/// <summary>
/// Represents the standards associated with a project.
/// </summary>
public class ProjectStandards
{
    /// <summary>
    /// Gets or sets the unique identifier for the project standard.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    /// <summary>
    /// Gets or sets the identifier of the associated project.
    /// </summary>
    public string ProjectId { get; set; } = "";

    /// <summary>
    /// Gets or sets the identifier of the associated profession.
    /// </summary>
    public string ProfessionId { get; set; } = "";

    /// <summary>
    /// Gets or sets the identifier of the associated standard.
    /// </summary>
    public string StandardId { get; set; } = "";

    /// <summary>
    /// Gets or sets the status of the project standard.
    /// Possible values: RED, AMBER_RED, AMBER, GREEN_AMBER, GREEN, TBC.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commentary for the project standard.
    /// </summary>
    public string Commentary { get; set; } = "";

    /// <summary>
    /// Gets or sets the date and time when the project standard was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last changed the project standard.
    /// </summary>
    public string ChangedBy { get; set; } = "";

    /// <summary>
    /// Gets or sets the name of the user who last changed the project standard.
    /// </summary>
    public string ChangedByName { get; set; } = "";
}
