using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IProjectStandardsPersistence
{
    Task<ProjectStandards?> GetAsync(string projectId, string standardId, string professionId);
    Task<List<ProjectStandards>> GetByProjectAndStandardAsync(string projectId, string standardId);
    Task<List<ProjectStandards>> GetByProjectAsync(string projectId);
    Task UpsertAsync(ProjectStandards assessment);
    Task<bool> DeleteAsync(string projectId, string standardId, string professionId);
}
