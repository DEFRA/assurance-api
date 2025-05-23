using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Profession.Models;

public class ProfessionModel
{
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
} 