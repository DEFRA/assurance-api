using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

public class ProjectProfessionStandardAssessmentHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public string ProfessionId { get; set; } = null!;
    public string StandardId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string ChangedBy { get; set; } = null!;
    public AssessmentChanges Changes { get; set; } = null!;
    public bool Archived { get; set; } = false;
}

public class AssessmentChanges
{
    public StatusChange? Status { get; set; }
    public CommentaryChange? Commentary { get; set; }
}
