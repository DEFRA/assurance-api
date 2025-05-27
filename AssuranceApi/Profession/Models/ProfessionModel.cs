using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace AssuranceApi.Profession.Models;

public class ProfessionModel
{
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = null!;
} 