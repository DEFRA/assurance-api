using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.Profession.Models;

public class ProfessionHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    // This should match ProfessionModel.Id (string, e.g. 'delivery-management')
    public string ProfessionId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string ChangedBy { get; set; } = null!;
    public ProfessionChanges Changes { get; set; } = null!;
}

public class ProfessionChanges
{
    public NameChange? Name { get; set; }
    public DescriptionChange? Description { get; set; }
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