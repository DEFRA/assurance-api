using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IProjectPersistence
{
    Task<bool> CreateAsync(ProjectModel project);
    Task<List<ProjectModel>> GetAllAsync();
    Task<ProjectModel?> GetByIdAsync(string id);
    Task<bool> UpdateAsync(ProjectModel project);
} 