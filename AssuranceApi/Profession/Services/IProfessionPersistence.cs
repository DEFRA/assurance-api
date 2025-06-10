using AssuranceApi.Profession.Models;

namespace AssuranceApi.Profession.Services;

public interface IProfessionPersistence
{
    Task<IEnumerable<ProfessionModel>> GetAllAsync();
    Task<IEnumerable<ProfessionModel>> GetAllActiveAsync();
    Task<ProfessionModel?> GetByIdAsync(string id);
    Task<ProfessionModel?> GetActiveByIdAsync(string id);
    Task<bool> CreateAsync(ProfessionModel profession);
    Task<bool> SeedProfessionsAsync(IEnumerable<ProfessionModel> professions);
    Task<bool> DeleteAllAsync();
    Task<bool> DeleteAsync(string id);
    Task<bool> SoftDeleteAsync(string id, string deletedBy);
    Task<bool> RestoreAsync(string id);
}
