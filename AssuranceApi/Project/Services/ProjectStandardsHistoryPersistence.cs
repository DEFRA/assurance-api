using AssuranceApi.Project.Models;
using AssuranceApi.Utils.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AssuranceApi.Project.Services;

public class ProjectStandardsHistoryPersistence : MongoService<ProjectStandardsHistory>, IProjectStandardsHistoryPersistence
{
    public ProjectStandardsHistoryPersistence(IMongoDbClientFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(connectionFactory, "projectStandardsHistory", loggerFactory)
    {
        Logger.LogInformation("Initializing ProjectStandardsHistoryPersistence with collection: projectStandardsHistory");
    }

    protected override List<CreateIndexModel<ProjectStandardsHistory>> DefineIndexes(IndexKeysDefinitionBuilder<ProjectStandardsHistory> builder)
    {
        return new List<CreateIndexModel<ProjectStandardsHistory>>
        {
            new CreateIndexModel<ProjectStandardsHistory>(
                builder.Ascending(x => x.ProjectId)
                    .Ascending(x => x.StandardId)
                    .Ascending(x => x.ProfessionId)
                    .Descending(x => x.Timestamp))
        };
    }

    public async Task<List<ProjectStandardsHistory>> GetHistoryAsync(string projectId, string standardId, string professionId)
    {
        return await Collection.Find(x => x.ProjectId == projectId && x.StandardId == standardId && x.ProfessionId == professionId && !x.Archived)
            .SortByDescending(x => x.Timestamp)
            .ToListAsync();
    }

    public async Task AddAsync(ProjectStandardsHistory history)
    {
        await Collection.InsertOneAsync(history);
    }

    public async Task<bool> ArchiveAsync(string projectId, string standardId, string professionId, string historyId)
    {
        var filter = Builders<ProjectStandardsHistory>.Filter.Where(x =>
            x.ProjectId == projectId &&
            x.StandardId == standardId &&
            x.ProfessionId == professionId &&
            x.Id == historyId);

        var update = Builders<ProjectStandardsHistory>.Update.Set(x => x.Archived, true);
        var result = await Collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }
}
