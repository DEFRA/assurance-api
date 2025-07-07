using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Utils.Mongo;

/// <summary>
/// Abstract base class for MongoDB services, providing common functionality for interacting with a MongoDB collection.
/// </summary>
/// <typeparam name="T">The type of the documents in the MongoDB collection.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class MongoService<T>
{
    /// <summary>
    /// The MongoDB client used to interact with the database.
    /// </summary>
    protected readonly IMongoClient Client;

    /// <summary>
    /// The MongoDB collection for the specified document type.
    /// </summary>
    protected readonly IMongoCollection<T> Collection;

    /// <summary>
    /// The logger used for logging operations in the service.
    /// </summary>
    protected readonly ILogger<MongoService<T>> Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoService{T}"/> class.
    /// </summary>
    /// <param name="connectionFactory">The factory to create MongoDB client and collection instances.</param>
    /// <param name="collectionName">The name of the MongoDB collection.</param>
    /// <param name="loggerFactory">The factory to create logger instances.</param>
    protected MongoService(
        IMongoDbClientFactory connectionFactory,
        string collectionName,
        ILoggerFactory loggerFactory
    )
    {
        Client = connectionFactory.GetClient();
        Collection = connectionFactory.GetCollection<T>(collectionName);
        Logger = loggerFactory.CreateLogger<MongoService<T>>();
        EnsureIndexes();
    }

    /// <summary>
    /// Defines the indexes for the MongoDB collection.
    /// </summary>
    /// <param name="builder">The index keys definition builder.</param>
    /// <returns>A list of index models to be created for the collection.</returns>
    protected abstract List<CreateIndexModel<T>> DefineIndexes(
        IndexKeysDefinitionBuilder<T> builder
    );

    /// <summary>
    /// Ensures that the defined indexes are created for the MongoDB collection.
    /// </summary>
    protected void EnsureIndexes()
    {
        var builder = Builders<T>.IndexKeys;
        var indexes = DefineIndexes(builder);
        if (indexes.Count == 0)
            return;

        Logger.LogInformation(
            "Ensuring index is created if it does not exist for collection {CollectionNamespaceCollectionName} in DB {DatabaseDatabaseNamespace}",
            Collection.CollectionNamespace.CollectionName,
            Collection.Database.DatabaseNamespace
        );
        Collection.Indexes.CreateMany(indexes);
    }
}
