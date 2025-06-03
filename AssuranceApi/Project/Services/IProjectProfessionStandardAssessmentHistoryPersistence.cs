using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IProjectProfessionStandardAssessmentHistoryPersistence
{
    Task<List<ProjectProfessionStandardAssessmentHistory>> GetHistoryAsync(string projectId, string standardId, string professionId);
    Task AddAsync(ProjectProfessionStandardAssessmentHistory history);
    Task<bool> ArchiveAsync(string projectId, string standardId, string professionId, string historyId);
}
