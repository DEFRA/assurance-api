using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

public class ProjectHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string ChangedBy { get; set; } = null!;
    public ProjectChanges Changes { get; set; } = null!;
}

public class ProjectChanges
{
    public NameChange? Name { get; set; }
    public StatusChange? Status { get; set; }
    public CommentaryChange? Commentary { get; set; }
}

public class NameChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
} 