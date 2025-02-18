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
    public Changes Changes { get; set; } = null!;
}

public class Changes
{
    public NameChange? Name { get; set; }
    public StatusChange? Status { get; set; }
    public CommentaryChange? Commentary { get; set; }
    public TagsChange? Tags { get; set; }
}

public class NameChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
}

public class StatusChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
}

public class CommentaryChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
}

public class TagsChange
{
    public List<string> From { get; set; } = new();
    public List<string> To { get; set; } = new();
} 