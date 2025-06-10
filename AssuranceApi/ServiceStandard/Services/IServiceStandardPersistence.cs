using AssuranceApi.ServiceStandard.Models;

namespace AssuranceApi.ServiceStandard.Services;

public interface IServiceStandardPersistence
{
    Task<bool> SeedStandardsAsync(List<ServiceStandardModel> standards);
    Task<List<ServiceStandardModel>> GetAllAsync();
    Task<List<ServiceStandardModel>> GetAllActiveAsync();
    Task<ServiceStandardModel?> GetByIdAsync(string id);
    Task<ServiceStandardModel?> GetActiveByIdAsync(string id);
    Task DeleteAllAsync();
    Task<bool> DeleteAsync(string id);
    Task<bool> SoftDeleteAsync(string id, string deletedBy);
    Task<bool> RestoreAsync(string id);
}
