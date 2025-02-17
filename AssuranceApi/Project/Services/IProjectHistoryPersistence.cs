using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IProjectHistoryPersistence
{
    Task<bool> CreateAsync(ProjectHistory history);
    Task<IEnumerable<ProjectHistory>> GetHistoryAsync(string projectId);
} 