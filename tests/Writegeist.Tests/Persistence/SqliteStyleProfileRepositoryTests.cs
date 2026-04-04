using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Writegeist.Core.Models;
using Writegeist.Infrastructure.Persistence;

namespace Writegeist.Tests.Persistence;

public class SqliteStyleProfileRepositoryTests : IDisposable
{
    private readonly SqliteStyleProfileRepository _repository;
    private readonly SqliteConnection _keepAlive;

    public SqliteStyleProfileRepositoryTests()
    {
        var dbName = $"file:test_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var config = Substitute.For<IConfiguration>();
        config["Writegeist:DatabasePath"].Returns(dbName);

        var database = new SqliteDatabase(config);
        database.EnsureCreated();

        _keepAlive = new SqliteConnection($"Data Source={dbName}");
        _keepAlive.Open();

        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = "INSERT INTO persons (name) VALUES ('TestPerson');";
        cmd.ExecuteNonQuery();

        _repository = new SqliteStyleProfileRepository(database);
    }

    public void Dispose()
    {
        _keepAlive.Dispose();
    }

    [Fact]
    public async Task SaveAsync_ReturnsProfileWithGeneratedId()
    {
        var profile = new StyleProfile
        {
            PersonId = 1,
            ProfileJson = "{\"tone\": \"casual\"}",
            Provider = "anthropic",
            Model = "claude-sonnet-4-20250514"
        };

        var saved = await _repository.SaveAsync(profile);

        saved.Id.Should().BeGreaterThan(0);
        saved.PersonId.Should().Be(1);
        saved.ProfileJson.Should().Be("{\"tone\": \"casual\"}");
        saved.Provider.Should().Be("anthropic");
        saved.Model.Should().Be("claude-sonnet-4-20250514");
        saved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task GetLatestByPersonIdAsync_ReturnsLatestProfile()
    {
        await _repository.SaveAsync(new StyleProfile
        {
            PersonId = 1, ProfileJson = "{\"v\": 1}", Provider = "anthropic", Model = "m1"
        });
        var latest = await _repository.SaveAsync(new StyleProfile
        {
            PersonId = 1, ProfileJson = "{\"v\": 2}", Provider = "openai", Model = "m2"
        });

        var result = await _repository.GetLatestByPersonIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(latest.Id);
        result.ProfileJson.Should().Be("{\"v\": 2}");
    }

    [Fact]
    public async Task GetLatestByPersonIdAsync_NoProfiles_ReturnsNull()
    {
        var result = await _repository.GetLatestByPersonIdAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByPersonIdAsync_ReturnsAllProfilesDescending()
    {
        var first = await _repository.SaveAsync(new StyleProfile
        {
            PersonId = 1, ProfileJson = "{\"v\": 1}", Provider = "anthropic", Model = "m1"
        });
        var second = await _repository.SaveAsync(new StyleProfile
        {
            PersonId = 1, ProfileJson = "{\"v\": 2}", Provider = "openai", Model = "m2"
        });

        var all = await _repository.GetAllByPersonIdAsync(1);

        all.Should().HaveCount(2);
        // Ordered by created_at DESC, id DESC — second should come first
        all[0].Id.Should().Be(second.Id);
        all[1].Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task GetAllByPersonIdAsync_NoProfiles_ReturnsEmpty()
    {
        var all = await _repository.GetAllByPersonIdAsync(999);

        all.Should().BeEmpty();
    }
}
