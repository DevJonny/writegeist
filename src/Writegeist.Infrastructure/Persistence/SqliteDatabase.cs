using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Writegeist.Infrastructure.Persistence;

public class SqliteDatabase
{
    private readonly string _connectionString;

    public SqliteDatabase(IConfiguration configuration)
    {
        var dbPath = configuration["Writegeist:DatabasePath"] ?? "writegeist.db";
        _connectionString = $"Data Source={dbPath}";
    }

    public string ConnectionString => _connectionString;

    public void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS persons (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS raw_posts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                person_id INTEGER NOT NULL,
                platform TEXT NOT NULL,
                content TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                source_url TEXT,
                fetched_at TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (person_id) REFERENCES persons(id),
                UNIQUE (person_id, content_hash)
            );

            CREATE TABLE IF NOT EXISTS style_profiles (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                person_id INTEGER NOT NULL,
                profile_json TEXT NOT NULL,
                provider TEXT NOT NULL,
                model TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (person_id) REFERENCES persons(id)
            );

            CREATE TABLE IF NOT EXISTS generated_drafts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                person_id INTEGER NOT NULL,
                style_profile_id INTEGER NOT NULL,
                platform TEXT NOT NULL,
                topic TEXT NOT NULL,
                content TEXT NOT NULL,
                parent_draft_id INTEGER,
                feedback TEXT,
                provider TEXT NOT NULL,
                model TEXT NOT NULL,
                created_at TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (person_id) REFERENCES persons(id),
                FOREIGN KEY (style_profile_id) REFERENCES style_profiles(id),
                FOREIGN KEY (parent_draft_id) REFERENCES generated_drafts(id)
            );
            """;

        command.ExecuteNonQuery();
    }
}
