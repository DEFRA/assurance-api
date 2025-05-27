using AssuranceApi.Profession.Models;

namespace AssuranceApi.Profession.Services;

public interface IProfessionPersistence
{
    Task<IEnumerable<ProfessionModel>> GetAllAsync();
    Task<ProfessionModel?> GetByIdAsync(string id);
    Task<bool> CreateAsync(ProfessionModel profession);
    Task<bool> SeedProfessionsAsync(IEnumerable<ProfessionModel> professions);
    Task<bool> DeleteAllAsync();
} 