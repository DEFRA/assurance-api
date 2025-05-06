using AssuranceApi.Profession.Models;

namespace AssuranceApi.Profession.Services;

public interface IProfessionPersistence
{
    Task<IEnumerable<ProfessionModel>> GetAllAsync();
    Task<ProfessionModel?> GetByIdAsync(string id);
    Task<bool> DeleteAllAsync();
    Task<bool> SeedProfessionsAsync(IEnumerable<ProfessionModel> professions);
} 