using MongoDB.Driver;

namespace AssuranceApi.Utils.Mongo;

/// <summary>
/// Factory interface for creating MongoDB client and accessing collections.
/// </summary>
public interface IMongoDbClientFactory
{
    /// <summary>
    /// Gets an instance of the MongoDB client.
    /// </summary>
    /// <returns>An instance of <see cref="IMongoClient"/>.</returns>
    IMongoClient GetClient();

    /// <summary>
    /// Gets a MongoDB collection of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the collection's documents.</typeparam>
    /// <param name="collection">The name of the collection.</param>
    /// <returns>An instance of <see cref="IMongoCollection{T}"/>.</returns>
    IMongoCollection<T> GetCollection<T>(string collection);
}
