using Writegeist.Core.Models;

namespace Writegeist.Core.Interfaces;

public interface IPostRepository
{
    Task<bool> AddAsync(RawPost post);
    Task<IReadOnlyList<RawPost>> GetByPersonIdAsync(int personId);
    Task<int> GetCountByPersonIdAsync(int personId);
    Task<bool> ExistsByHashAsync(int personId, string contentHash);
}
