using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Profession.Models;

/// <summary>
/// Represents a profession with details such as name, description, and audit information.
/// </summary>
public class ProfessionModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the profession.
    /// </summary>
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the profession.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the profession.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the profession is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the profession was deleted, if applicable.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the profession, if applicable.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the profession was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the profession was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
