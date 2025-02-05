using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssuranceApi.ServiceStandard.Models;

public class ServiceStandardModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public int Number { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
} 