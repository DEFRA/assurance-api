using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

public class ProjectModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;  // RED, AMBER_RED, AMBER, GREEN_AMBER, GREEN, TBC
    public string LastUpdated { get; set; } = null!;
    public string Commentary { get; set; } = null!;
    public string? Phase { get; set; }  // GDS phase (e.g., Discovery, Alpha, Beta, Live) - optional
    public string? DefCode { get; set; }  // DEFRA project identifier - optional
    public List<string> Tags { get; set; } = new();
    public string? UpdateDate { get; set; }
    public List<StandardSummaryModel> StandardsSummary { get; set; } = new();
}