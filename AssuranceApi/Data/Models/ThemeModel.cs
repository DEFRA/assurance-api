using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Data.Models;

/// <summary>
/// Represents a key theme that can be associated with multiple projects/deliveries.
/// Themes are used to group and track cross-cutting concerns across the portfolio.
/// </summary>
public class ThemeModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the theme.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the theme.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the theme.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list of project IDs associated with this theme.
    /// </summary>
    public List<string> ProjectIds { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the theme is active (not archived).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the theme was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the theme was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the email of the user who created the theme.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the email of the user who last updated the theme.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

