using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IProjectProfessionHistoryPersistence
{
    Task<IEnumerable<ProjectProfessionHistory>> GetHistoryAsync(
        string projectId,
        string professionId
    );
    Task<bool> CreateAsync(ProjectProfessionHistory history);
    Task<bool> DeleteAllAsync();
    Task<bool> ArchiveHistoryEntryAsync(string projectId, string professionId, string historyId);
    Task<ProjectProfessionHistory?> GetLatestHistoryAsync(string projectId, string professionId);
}
