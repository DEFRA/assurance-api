using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Project.Models;

public class StandardHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public string StandardId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string ChangedBy { get; set; } = null!;
    public StandardChanges Changes { get; set; } = null!;
}

public class StandardChanges
{
    public StatusChange? Status { get; set; }
    public CommentaryChange? Commentary { get; set; }
} 