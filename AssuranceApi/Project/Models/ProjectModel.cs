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
    public List<StandardAssessment> Standards { get; set; } = new();
}

public class StandardAssessment
{
    public string StandardId { get; set; } = null!;
    public string Status { get; set; } = null!;  // RED, AMBER, GREEN
    public string Commentary { get; set; } = null!;
} 