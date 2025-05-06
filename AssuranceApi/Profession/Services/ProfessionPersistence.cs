using AssuranceApi.Profession.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Profession.Services;

public class ProfessionPersistence : MongoService<ProfessionModel>, IProfessionPersistence
{
    public ProfessionPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "professions", loggerFactory)
    {
    }

    protected override List<CreateIndexModel<ProfessionModel>> DefineIndexes(
        IndexKeysDefinitionBuilder<ProfessionModel> builder)
    {
        return new List<CreateIndexModel<ProfessionModel>>
        {
            new CreateIndexModel<ProfessionModel>(
                builder.Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true })
        };
    }

    public async Task<bool> SeedProfessionsAsync(IEnumerable<ProfessionModel> professions)
    {
        try
        {
            // Clear existing professions
            await Collection.DeleteManyAsync(Builders<ProfessionModel>.Filter.Empty);
            
            // Insert new professions
            if (professions.Any())
            {
                await Collection.InsertManyAsync(professions);
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to seed professions");
            return false;
        }
    }

    public async Task<IEnumerable<ProfessionModel>> GetAllAsync()
    {
        return await Collection.Find(Builders<ProfessionModel>.Filter.Empty)
            .SortBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<ProfessionModel?> GetByIdAsync(string id)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAllAsync()
    {
        try
        {
            await Collection.DeleteManyAsync(Builders<ProfessionModel>.Filter.Empty);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete all professions");
            return false;
        }
    }
} 