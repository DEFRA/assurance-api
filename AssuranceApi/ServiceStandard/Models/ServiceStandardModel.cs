using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.ServiceStandard.Models;

public class ServiceStandardModel
{
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;
    public int Number { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    // Soft delete fields
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Basic audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
