using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IProjectStandardsHistoryPersistence
{
    Task<List<ProjectStandardsHistory>> GetHistoryAsync(string projectId, string standardId, string professionId);
    Task AddAsync(ProjectStandardsHistory history);
    Task<bool> ArchiveAsync(string projectId, string standardId, string professionId, string historyId);
}
