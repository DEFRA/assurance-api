using AssuranceApi.Project.Models;

namespace AssuranceApi.Project.Services;

public interface IStandardHistoryPersistence
{
    Task<bool> CreateAsync(StandardHistory history);
    Task<IEnumerable<StandardHistory>> GetHistoryAsync(string projectId, string standardId);
    Task DeleteAllAsync();
} 