using AssuranceApi.Data.Models;
using AssuranceApi.Utils.Mongo;
using MongoDB.Driver;

namespace AssuranceApi.Data;

/// <summary>
/// Concrete implementation of IThemePersistence using MongoDB.
/// </summary>
public class ThemePersistence : MongoService<ThemeModel>, IThemePersistence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemePersistence"/> class.
    /// </summary>
    /// <param name="connectionFactory">The factory to create MongoDB client connections.</param>
    /// <param name="loggerFactory">The factory to create loggers.</param>
    public ThemePersistence(
        IMongoDbClientFactory connectionFactory,
        ILoggerFactory loggerFactory
    )
        : base(connectionFactory, "themes", loggerFactory) { }

    /// <summary>
    /// Defines the indexes for the themes collection.
    /// </summary>
    /// <param name="builder">The index keys definition builder.</param>
    /// <returns>A list of index models to be created on the collection.</returns>
    protected override List<CreateIndexModel<ThemeModel>> DefineIndexes(
        IndexKeysDefinitionBuilder<ThemeModel> builder
    )
    {
        return new List<CreateIndexModel<ThemeModel>>
        {
            new CreateIndexModel<ThemeModel>(
                builder.Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true }
            ),
            new CreateIndexModel<ThemeModel>(
                builder.Ascending(x => x.IsActive)
            ),
            new CreateIndexModel<ThemeModel>(
                builder.Ascending(x => x.ProjectIds)
            )
        };
    }

    /// <inheritdoc/>
    public async Task<List<ThemeModel>> GetAllAsync(bool includeArchived = false)
    {
        var findOptions = new FindOptions
        {
            Collation = MongoDbHelpers.GetCaseInsensitiveCollation()
        };

        FilterDefinition<ThemeModel> filter;
        if (includeArchived)
        {
            filter = Builders<ThemeModel>.Filter.Empty;
        }
        else
        {
            filter = Builders<ThemeModel>.Filter.Eq(x => x.IsActive, true);
        }

        return await Collection
            .Find(filter, findOptions)
            .Sort(Builders<ThemeModel>.Sort.Ascending(x => x.Name))
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<ThemeModel?> GetByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> CreateAsync(ThemeModel theme)
    {
        try
        {
            await Collection.InsertOneAsync(theme);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create theme");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(string id, ThemeModel theme)
    {
        try
        {
            Logger.LogInformation("Updating theme {ThemeId}", id);

            var result = await Collection.ReplaceOneAsync(x => x.Id == id, theme);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update theme {ThemeId}", id);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ArchiveAsync(string id)
    {
        try
        {
            Logger.LogInformation("Archiving theme with ID: {Id}", id);

            var update = Builders<ThemeModel>.Update
                .Set(x => x.IsActive, false)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(x => x.Id == id, update);

            if (result.ModifiedCount == 0)
            {
                Logger.LogWarning("Theme with ID {Id} not found for archiving", id);
                return false;
            }

            Logger.LogInformation("Successfully archived theme with ID: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error archiving theme with ID: {Id}", id);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RestoreAsync(string id)
    {
        try
        {
            Logger.LogInformation("Restoring theme with ID: {Id}", id);

            var update = Builders<ThemeModel>.Update
                .Set(x => x.IsActive, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(x => x.Id == id, update);

            if (result.ModifiedCount == 0)
            {
                Logger.LogWarning("Theme with ID {Id} not found for restoring", id);
                return false;
            }

            Logger.LogInformation("Successfully restored theme with ID: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error restoring theme with ID: {Id}", id);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ThemeModel>> GetByProjectIdAsync(string projectId)
    {
        var filter = Builders<ThemeModel>.Filter.And(
            Builders<ThemeModel>.Filter.Eq(x => x.IsActive, true),
            Builders<ThemeModel>.Filter.AnyEq(x => x.ProjectIds, projectId)
        );

        return await Collection.Find(filter).ToListAsync();
    }
}

