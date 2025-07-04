using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

public class ProjectStandards
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string ProfessionId { get; set; } = "";
    public string StandardId { get; set; } = "";
    public string Status { get; set; } = null!; // RED, AMBER_RED, AMBER, GREEN_AMBER, GREEN, TBC
    public string Commentary { get; set; } = "";
    public DateTime LastUpdated { get; set; }
    public string ChangedBy { get; set; } = "";
}
