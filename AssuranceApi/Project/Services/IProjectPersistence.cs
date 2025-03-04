using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IProjectPersistence
{
    Task<bool> CreateAsync(ProjectModel project);
    Task<List<ProjectModel>> GetAllAsync(string? tag = null);
    Task<ProjectModel?> GetByIdAsync(string id);
    Task<bool> UpdateAsync(string id, ProjectModel project);
    Task DeleteAllAsync();
    Task<bool> DeleteAsync(string id);
} 