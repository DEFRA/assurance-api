using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IProjectProfessionStandardAssessmentPersistence
{
    Task<ProjectProfessionStandardAssessment?> GetAsync(string projectId, string standardId, string professionId);
    Task<List<ProjectProfessionStandardAssessment>> GetByProjectAndStandardAsync(string projectId, string standardId);
    Task<List<ProjectProfessionStandardAssessment>> GetByProjectAsync(string projectId);
    Task UpsertAsync(ProjectProfessionStandardAssessment assessment);
    Task<bool> DeleteAsync(string projectId, string standardId, string professionId);
}
