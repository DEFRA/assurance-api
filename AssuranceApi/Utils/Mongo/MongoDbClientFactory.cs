using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace AssuranceApi.Utils.Mongo;

/// <summary>
/// Factory class for creating MongoDB client and accessing collections.
/// </summary>
[ExcludeFromCodeCoverage]
public class MongoDbClientFactory : IMongoDbClientFactory
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly MongoClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbClientFactory"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string for MongoDB.</param>
    /// <param name="databaseName">The name of the database to connect to.</param>
    /// <exception cref="ArgumentException">Thrown when the connection string is null or empty.</exception>
    public MongoDbClientFactory(string? connectionString, string databaseName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("MongoDB connection string cannot be empty");

        var settings = MongoClientSettings.FromConnectionString(connectionString);
        _client = new MongoClient(settings);

        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        // convention must be registered before initializing collection
        ConventionRegistry.Register("CamelCase", camelCaseConvention, _ => true);

        _mongoDatabase = _client.GetDatabase(databaseName);
    }

    /// <summary>
    /// Creates and returns the MongoDB client.
    /// </summary>
    /// <returns>An instance of <see cref="IMongoClient"/>.</returns>
    public IMongoClient CreateClient()
    {
        return _client;
    }

    /// <summary>
    /// Gets a MongoDB collection of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the collection documents.</typeparam>
    /// <param name="collection">The name of the collection.</param>
    /// <returns>An instance of <see cref="IMongoCollection{T}"/>.</returns>
    public IMongoCollection<T> GetCollection<T>(string collection)
    {
        return _mongoDatabase.GetCollection<T>(collection);
    }

    /// <summary>
    /// Gets the MongoDB client instance.
    /// </summary>
    /// <returns>An instance of <see cref="IMongoClient"/>.</returns>
    public IMongoClient GetClient()
    {
        return _client;
    }
}
