using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.ServiceStandard.Models;

public class StandardDefinitionHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string StandardId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string ChangedBy { get; set; } = null!;
    public StandardDefinitionChanges Changes { get; set; } = null!;
}

public class StandardDefinitionChanges
{
    public NameChange? Name { get; set; }
    public DescriptionChange? Description { get; set; }
    public GuidanceChange? Guidance { get; set; }
}

public class NameChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
}

public class DescriptionChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
}

public class GuidanceChange
{
    public string From { get; set; } = null!;
    public string To { get; set; } = null!;
} 