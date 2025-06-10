using AssuranceApi.ServiceStandard.Models;

namespace AssuranceApi.ServiceStandard.Services;

public interface IServiceStandardHistoryPersistence
{
    Task<bool> CreateAsync(StandardDefinitionHistory history);
    Task<IEnumerable<StandardDefinitionHistory>> GetHistoryAsync(string standardId);
}
