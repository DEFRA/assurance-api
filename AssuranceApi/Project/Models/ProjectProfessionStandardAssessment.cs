using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

public class ProjectProfessionStandardAssessment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public string ProfessionId { get; set; } = null!;
    public string StandardId { get; set; } = null!;
    public string Status { get; set; } = null!; // RED, AMBER_RED, AMBER, GREEN_AMBER, GREEN, TBC
    public string Commentary { get; set; } = null!;
    public DateTime LastUpdated { get; set; }
    public string ChangedBy { get; set; } = null!;
}
