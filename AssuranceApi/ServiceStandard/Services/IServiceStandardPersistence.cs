using AssuranceApi.ServiceStandard.Models;

namespace AssuranceApi.ServiceStandard.Services;

public interface IServiceStandardPersistence
{
    Task<bool> SeedStandardsAsync(List<ServiceStandardModel> standards);
    Task<List<ServiceStandardModel>> GetAllAsync();
    Task<ServiceStandardModel?> GetByIdAsync(string id);
    Task DeleteAllAsync();
} 