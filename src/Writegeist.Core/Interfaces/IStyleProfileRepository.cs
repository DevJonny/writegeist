using Writegeist.Core.Models;

namespace Writegeist.Core.Interfaces;

public interface IStyleProfileRepository
{
    Task<StyleProfile> SaveAsync(StyleProfile profile);
    Task<StyleProfile?> GetLatestByPersonIdAsync(int personId);
    Task<IReadOnlyList<StyleProfile>> GetAllByPersonIdAsync(int personId);
}
