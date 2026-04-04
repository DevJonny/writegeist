using Writegeist.Core.Models;

namespace Writegeist.Core.Interfaces;

public interface IDraftRepository
{
    Task<GeneratedDraft> SaveAsync(GeneratedDraft draft);
    Task<GeneratedDraft?> GetLatestAsync();
    Task<GeneratedDraft?> GetByIdAsync(int id);
}
