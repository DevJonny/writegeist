using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Writegeist.Core.Models;
using Writegeist.Infrastructure.Persistence;

namespace Writegeist.Tests.Persistence;

public class SqliteDraftRepositoryTests : IDisposable
{
    private readonly SqliteDraftRepository _repository;
    private readonly SqliteConnection _keepAlive;

    public SqliteDraftRepositoryTests()
    {
        var dbName = $"file:test_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var config = Substitute.For<IConfiguration>();
        config["Writegeist:DatabasePath"].Returns(dbName);

        var database = new SqliteDatabase(config);
        database.EnsureCreated();

        _keepAlive = new SqliteConnection($"Data Source={dbName}");
        _keepAlive.Open();

        // Insert prerequisite person and style profile
        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = """
            INSERT INTO persons (name) VALUES ('TestPerson');
            INSERT INTO style_profiles (person_id, profile_json, provider, model) VALUES (1, '{}', 'anthropic', 'test');
            """;
        cmd.ExecuteNonQuery();

        _repository = new SqliteDraftRepository(database);
    }

    public void Dispose()
    {
        _keepAlive.Dispose();
    }

    private GeneratedDraft MakeDraft(string content = "Test post", int? parentDraftId = null, string? feedback = null)
    {
        return new GeneratedDraft
        {
            PersonId = 1,
            StyleProfileId = 1,
            Platform = Platform.LinkedIn,
            Topic = "testing",
            Content = content,
            ParentDraftId = parentDraftId,
            Feedback = feedback,
            Provider = "anthropic",
            Model = "test-model"
        };
    }

    [Fact]
    public async Task SaveAsync_ReturnsDraftWithGeneratedId()
    {
        var saved = await _repository.SaveAsync(MakeDraft());

        saved.Id.Should().BeGreaterThan(0);
        saved.Content.Should().Be("Test post");
        saved.Platform.Should().Be(Platform.LinkedIn);
        saved.Provider.Should().Be("anthropic");
        saved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsMostRecentDraft()
    {
        await _repository.SaveAsync(MakeDraft("First"));
        var second = await _repository.SaveAsync(MakeDraft("Second"));

        var latest = await _repository.GetLatestAsync();

        latest.Should().NotBeNull();
        latest!.Id.Should().Be(second.Id);
        latest.Content.Should().Be("Second");
    }

    [Fact]
    public async Task GetLatestAsync_NoDrafts_ReturnsNull()
    {
        var result = await _repository.GetLatestAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingDraft_ReturnsDraft()
    {
        var saved = await _repository.SaveAsync(MakeDraft());

        var result = await _repository.GetByIdAsync(saved.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(saved.Id);
        result.Content.Should().Be("Test post");
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ParentChildLinking_WorksCorrectly()
    {
        var parent = await _repository.SaveAsync(MakeDraft("Original"));
        var child = await _repository.SaveAsync(MakeDraft("Refined", parentDraftId: parent.Id, feedback: "Make it shorter"));

        child.ParentDraftId.Should().Be(parent.Id);
        child.Feedback.Should().Be("Make it shorter");

        var retrieved = await _repository.GetByIdAsync(child.Id);
        retrieved!.ParentDraftId.Should().Be(parent.Id);
        retrieved.Feedback.Should().Be("Make it shorter");
    }

    [Fact]
    public async Task SaveAsync_NullableFieldsAreNull_WhenNotSet()
    {
        var saved = await _repository.SaveAsync(MakeDraft());

        saved.ParentDraftId.Should().BeNull();
        saved.Feedback.Should().BeNull();
    }
}
