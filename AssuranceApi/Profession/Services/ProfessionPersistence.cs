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

    public async Task<bool> CreateAsync(ProfessionModel profession)
    {
        try
        {
            // Check if profession with same ID already exists
            var existing = await GetByIdAsync(profession.Id);
            if (existing != null)
            {
                Logger.LogWarning("Profession with ID {Id} already exists", profession.Id);
                return false;
            }

            await Collection.InsertOneAsync(profession);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create profession");
            return false;
        }
    }

    public async Task<bool> SeedProfessionsAsync(IEnumerable<ProfessionModel> professions)
    {
        try
        {
            // Upsert each profession by Id (incremental update)
            foreach (var profession in professions)
            {
                var filter = Builders<ProfessionModel>.Filter.Eq(x => x.Id, profession.Id);
                await Collection.ReplaceOneAsync(filter, profession, new ReplaceOptions { IsUpsert = true });
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