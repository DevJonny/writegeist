using Writegeist.Core.Models;

namespace Writegeist.Core.Interfaces;

public interface IPersonRepository
{
    Task<Person> CreateAsync(string name);
    Task<Person?> GetByNameAsync(string name);
    Task<IReadOnlyList<Person>> GetAllAsync();
    Task<Person> GetOrCreateAsync(string name);
}
