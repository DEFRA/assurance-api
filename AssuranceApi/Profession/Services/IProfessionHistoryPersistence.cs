using AssuranceApi.Profession.Models;

namespace AssuranceApi.Profession.Services;

public interface IProfessionHistoryPersistence
{
    Task<IEnumerable<ProfessionHistory>> GetHistoryAsync(string professionId);
    Task<bool> CreateAsync(ProfessionHistory history);
    Task<bool> DeleteAllAsync();
} 