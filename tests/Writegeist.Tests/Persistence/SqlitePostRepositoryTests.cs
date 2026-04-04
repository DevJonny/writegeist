using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Writegeist.Core.Models;
using Writegeist.Infrastructure.Persistence;

namespace Writegeist.Tests.Persistence;

public class SqlitePostRepositoryTests : IDisposable
{
    private readonly SqlitePostRepository _repository;
    private readonly SqliteConnection _keepAlive;
    private readonly SqliteDatabase _database;

    public SqlitePostRepositoryTests()
    {
        var dbName = $"file:test_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var config = Substitute.For<IConfiguration>();
        config["Writegeist:DatabasePath"].Returns(dbName);

        _database = new SqliteDatabase(config);
        _database.EnsureCreated();

        _keepAlive = new SqliteConnection($"Data Source={dbName}");
        _keepAlive.Open();

        // Insert a test person
        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = "INSERT INTO persons (name) VALUES ('TestPerson');";
        cmd.ExecuteNonQuery();

        _repository = new SqlitePostRepository(_database);
    }

    public void Dispose()
    {
        _keepAlive.Dispose();
    }

    [Fact]
    public async Task AddAsync_NewPost_ReturnsTrueAndStoresPost()
    {
        var post = new RawPost
        {
            PersonId = 1,
            Platform = Platform.LinkedIn,
            Content = "Hello world"
        };

        var result = await _repository.AddAsync(post);

        result.Should().BeTrue();
        post.ContentHash.Should().NotBeNullOrEmpty();

        var posts = await _repository.GetByPersonIdAsync(1);
        posts.Should().HaveCount(1);
        posts[0].Content.Should().Be("Hello world");
    }

    [Fact]
    public async Task AddAsync_DuplicateContent_ReturnsFalse()
    {
        var post1 = new RawPost
        {
            PersonId = 1,
            Platform = Platform.X,
            Content = "Same content"
        };
        var post2 = new RawPost
        {
            PersonId = 1,
            Platform = Platform.X,
            Content = "Same content"
        };

        var first = await _repository.AddAsync(post1);
        var second = await _repository.AddAsync(post2);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ContentHashIsNormalised()
    {
        var post1 = new RawPost
        {
            PersonId = 1,
            Platform = Platform.LinkedIn,
            Content = "  Hello World  "
        };
        var post2 = new RawPost
        {
            PersonId = 1,
            Platform = Platform.LinkedIn,
            Content = "hello world"
        };

        await _repository.AddAsync(post1);
        var isDuplicate = await _repository.AddAsync(post2);

        isDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task GetByPersonIdAsync_ReturnsAllPostsForPerson()
    {
        await _repository.AddAsync(new RawPost { PersonId = 1, Platform = Platform.LinkedIn, Content = "First" });
        await _repository.AddAsync(new RawPost { PersonId = 1, Platform = Platform.X, Content = "Second" });

        var posts = await _repository.GetByPersonIdAsync(1);

        posts.Should().HaveCount(2);
        posts.Select(p => p.Content).Should().Contain("First").And.Contain("Second");
    }

    [Fact]
    public async Task GetCountByPersonIdAsync_ReturnsCorrectCount()
    {
        await _repository.AddAsync(new RawPost { PersonId = 1, Platform = Platform.LinkedIn, Content = "One" });
        await _repository.AddAsync(new RawPost { PersonId = 1, Platform = Platform.X, Content = "Two" });
        await _repository.AddAsync(new RawPost { PersonId = 1, Platform = Platform.Instagram, Content = "Three" });

        var count = await _repository.GetCountByPersonIdAsync(1);

        count.Should().Be(3);
    }

    [Fact]
    public async Task ExistsByHashAsync_ExistingHash_ReturnsTrue()
    {
        var post = new RawPost { PersonId = 1, Platform = Platform.LinkedIn, Content = "Check me" };
        await _repository.AddAsync(post);

        var exists = await _repository.ExistsByHashAsync(1, post.ContentHash);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByHashAsync_NonExistingHash_ReturnsFalse()
    {
        var exists = await _repository.ExistsByHashAsync(1, "nonexistenthash");

        exists.Should().BeFalse();
    }

    [Fact]
    public void ComputeHash_IsSha256OfNormalisedContent()
    {
        var hash1 = SqlitePostRepository.ComputeHash("  Hello World  ");
        var hash2 = SqlitePostRepository.ComputeHash("hello world");

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64); // SHA-256 hex string
    }
}
