using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Writegeist.Infrastructure.Persistence;

namespace Writegeist.Tests.Persistence;

public class SqlitePersonRepositoryTests : IDisposable
{
    private readonly SqlitePersonRepository _repository;
    private readonly SqliteConnection _keepAlive;

    public SqlitePersonRepositoryTests()
    {
        var dbName = $"file:test_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var config = Substitute.For<IConfiguration>();
        config["Writegeist:DatabasePath"].Returns(dbName);

        var database = new SqliteDatabase(config);
        database.EnsureCreated();

        _keepAlive = new SqliteConnection($"Data Source={dbName}");
        _keepAlive.Open();

        _repository = new SqlitePersonRepository(database);
    }

    public void Dispose()
    {
        _keepAlive.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ReturnsPersonWithGeneratedId()
    {
        var person = await _repository.CreateAsync("Alice");

        person.Id.Should().BeGreaterThan(0);
        person.Name.Should().Be("Alice");
        person.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task GetByNameAsync_ExistingPerson_ReturnsPerson()
    {
        await _repository.CreateAsync("Bob");

        var result = await _repository.GetByNameAsync("Bob");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task GetByNameAsync_CaseInsensitive()
    {
        await _repository.CreateAsync("Charlie");

        var result = await _repository.GetByNameAsync("charlie");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByNameAsync("Nobody");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrderedByName()
    {
        await _repository.CreateAsync("Zara");
        await _repository.CreateAsync("Alice");
        await _repository.CreateAsync("Mike");

        var all = await _repository.GetAllAsync();

        all.Should().HaveCount(3);
        all[0].Name.Should().Be("Alice");
        all[1].Name.Should().Be("Mike");
        all[2].Name.Should().Be("Zara");
    }

    [Fact]
    public async Task GetOrCreateAsync_ExistingPerson_ReturnsExisting()
    {
        var created = await _repository.CreateAsync("Diana");

        var result = await _repository.GetOrCreateAsync("diana");

        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Diana");
    }

    [Fact]
    public async Task GetOrCreateAsync_NewPerson_CreatesAndReturns()
    {
        var result = await _repository.GetOrCreateAsync("Eve");

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Eve");

        var verify = await _repository.GetByNameAsync("Eve");
        verify.Should().NotBeNull();
    }
}
