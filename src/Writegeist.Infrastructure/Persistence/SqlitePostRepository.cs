using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Persistence;

public class SqlitePostRepository : IPostRepository
{
    private readonly string _connectionString;

    public SqlitePostRepository(SqliteDatabase database)
    {
        _connectionString = database.ConnectionString;
    }

    public async Task<bool> AddAsync(RawPost post)
    {
        post.ContentHash = ComputeHash(post.Content);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO raw_posts (person_id, platform, content, content_hash, source_url)
            VALUES (@personId, @platform, @content, @contentHash, @sourceUrl);
            """;
        command.Parameters.AddWithValue("@personId", post.PersonId);
        command.Parameters.AddWithValue("@platform", post.Platform.ToString());
        command.Parameters.AddWithValue("@content", post.Content);
        command.Parameters.AddWithValue("@contentHash", post.ContentHash);
        command.Parameters.AddWithValue("@sourceUrl", (object?)post.SourceUrl ?? DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<IReadOnlyList<RawPost>> GetByPersonIdAsync(int personId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, person_id, platform, content, content_hash, source_url, fetched_at
            FROM raw_posts
            WHERE person_id = @personId
            ORDER BY fetched_at;
            """;
        command.Parameters.AddWithValue("@personId", personId);

        await using var reader = await command.ExecuteReaderAsync();
        var posts = new List<RawPost>();
        while (await reader.ReadAsync())
            posts.Add(ReadPost(reader));

        return posts;
    }

    public async Task<int> GetCountByPersonIdAsync(int personId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM raw_posts WHERE person_id = @personId;";
        command.Parameters.AddWithValue("@personId", personId);

        var count = (long)(await command.ExecuteScalarAsync())!;
        return (int)count;
    }

    public async Task<bool> ExistsByHashAsync(int personId, string contentHash)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM raw_posts
            WHERE person_id = @personId AND content_hash = @contentHash;
            """;
        command.Parameters.AddWithValue("@personId", personId);
        command.Parameters.AddWithValue("@contentHash", contentHash);

        var count = (long)(await command.ExecuteScalarAsync())!;
        return count > 0;
    }

    public static string ComputeHash(string content)
    {
        var normalised = content.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        return Convert.ToHexStringLower(bytes);
    }

    private static RawPost ReadPost(SqliteDataReader reader)
    {
        return new RawPost
        {
            Id = reader.GetInt32(0),
            PersonId = reader.GetInt32(1),
            Platform = Enum.Parse<Platform>(reader.GetString(2)),
            Content = reader.GetString(3),
            ContentHash = reader.GetString(4),
            SourceUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
            FetchedAt = DateTime.Parse(reader.GetString(6))
        };
    }
}
