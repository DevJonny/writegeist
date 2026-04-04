using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Writegeist.Infrastructure.Persistence;

namespace Writegeist.Tests.Persistence;

public class SqliteDatabaseTests : IDisposable
{
    private readonly string _dbName;
    private readonly SqliteDatabase _database;
    private readonly SqliteConnection _verifyConnection;

    public SqliteDatabaseTests()
    {
        _dbName = $"file:test_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var config = Substitute.For<IConfiguration>();
        config["Writegeist:DatabasePath"].Returns(_dbName);
        _database = new SqliteDatabase(config);

        // Keep a connection open so the shared in-memory DB persists
        _verifyConnection = new SqliteConnection($"Data Source={_dbName}");
        _verifyConnection.Open();
    }

    public void Dispose()
    {
        _verifyConnection.Dispose();
    }

    private List<string> GetTableNames()
    {
        using var cmd = _verifyConnection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
        using var reader = cmd.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read())
            tables.Add(reader.GetString(0));
        return tables;
    }

    private List<string> GetColumnNames(string tableName)
    {
        using var cmd = _verifyConnection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = cmd.ExecuteReader();
        var columns = new List<string>();
        while (reader.Read())
            columns.Add(reader.GetString(1));
        return columns;
    }

    [Fact]
    public void EnsureCreated_CreatesAllFourTables()
    {
        _database.EnsureCreated();

        var tables = GetTableNames();
        tables.Should().Contain("persons");
        tables.Should().Contain("raw_posts");
        tables.Should().Contain("style_profiles");
        tables.Should().Contain("generated_drafts");
    }

    [Fact]
    public void EnsureCreated_IsIdempotent()
    {
        _database.EnsureCreated();
        _database.EnsureCreated(); // should not throw

        GetTableNames().Should().Contain("persons");
    }

    [Fact]
    public void EnsureCreated_PersonsTable_HasExpectedColumns()
    {
        _database.EnsureCreated();

        var columns = GetColumnNames("persons");
        columns.Should().Contain("id");
        columns.Should().Contain("name");
        columns.Should().Contain("created_at");
    }

    [Fact]
    public void EnsureCreated_RawPostsTable_HasExpectedColumns()
    {
        _database.EnsureCreated();

        var columns = GetColumnNames("raw_posts");
        columns.Should().Contain("id");
        columns.Should().Contain("person_id");
        columns.Should().Contain("platform");
        columns.Should().Contain("content");
        columns.Should().Contain("content_hash");
        columns.Should().Contain("source_url");
        columns.Should().Contain("fetched_at");
    }

    [Fact]
    public void EnsureCreated_StyleProfilesTable_HasExpectedColumns()
    {
        _database.EnsureCreated();

        var columns = GetColumnNames("style_profiles");
        columns.Should().Contain("id");
        columns.Should().Contain("person_id");
        columns.Should().Contain("profile_json");
        columns.Should().Contain("provider");
        columns.Should().Contain("model");
        columns.Should().Contain("created_at");
    }

    [Fact]
    public void EnsureCreated_GeneratedDraftsTable_HasExpectedColumns()
    {
        _database.EnsureCreated();

        var columns = GetColumnNames("generated_drafts");
        columns.Should().Contain("id");
        columns.Should().Contain("person_id");
        columns.Should().Contain("style_profile_id");
        columns.Should().Contain("platform");
        columns.Should().Contain("topic");
        columns.Should().Contain("content");
        columns.Should().Contain("parent_draft_id");
        columns.Should().Contain("feedback");
        columns.Should().Contain("provider");
        columns.Should().Contain("model");
        columns.Should().Contain("created_at");
    }
}
