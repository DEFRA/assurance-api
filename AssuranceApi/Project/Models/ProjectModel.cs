using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

public class ProjectModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;  // RED, AMBER, GREEN
    public string LastUpdated { get; set; } = null!;
    public string Commentary { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public List<StandardModel> Standards { get; set; } = new();
    public List<ProfessionModel> Professions { get; set; } = new();
    public string? UpdateDate { get; set; }
}

public class StandardModel
{
    public string StandardId { get; set; } = null!;
    public string Status { get; set; } = null!;  // RED, AMBER, GREEN
    public string Commentary { get; set; } = null!;
}

public class ProfessionModel
{
    public string ProfessionId { get; set; } = null!;
    public string Status { get; set; } = null!;  // RED, AMBER, GREEN
    public string Commentary { get; set; } = null!;
} 