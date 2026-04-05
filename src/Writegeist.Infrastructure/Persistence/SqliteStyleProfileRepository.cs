using Microsoft.Data.Sqlite;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Persistence;

public class SqliteStyleProfileRepository : IStyleProfileRepository
{
    private readonly string _connectionString;

    public SqliteStyleProfileRepository(SqliteDatabase database)
    {
        _connectionString = database.ConnectionString;
    }

    public async Task<StyleProfile> SaveAsync(StyleProfile profile)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO style_profiles (person_id, profile_json, provider, model)
            VALUES (@personId, @profileJson, @provider, @model);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@personId", profile.PersonId);
        command.Parameters.AddWithValue("@profileJson", profile.ProfileJson);
        command.Parameters.AddWithValue("@provider", profile.Provider);
        command.Parameters.AddWithValue("@model", profile.Model);

        var id = (long)(await command.ExecuteScalarAsync())!;

        await using var readCmd = connection.CreateCommand();
        readCmd.CommandText = "SELECT id, person_id, profile_json, provider, model, created_at FROM style_profiles WHERE id = @id;";
        readCmd.Parameters.AddWithValue("@id", id);

        await using var reader = await readCmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return ReadProfile(reader);
    }

    public async Task<StyleProfile?> GetLatestByPersonIdAsync(int personId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, person_id, profile_json, provider, model, created_at
            FROM style_profiles
            WHERE person_id = @personId
            ORDER BY created_at DESC, id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@personId", personId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return ReadProfile(reader);
    }

    public async Task<IReadOnlyList<StyleProfile>> GetAllByPersonIdAsync(int personId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, person_id, profile_json, provider, model, created_at
            FROM style_profiles
            WHERE person_id = @personId
            ORDER BY created_at DESC, id DESC;
            """;
        command.Parameters.AddWithValue("@personId", personId);

        await using var reader = await command.ExecuteReaderAsync();
        var profiles = new List<StyleProfile>();
        while (await reader.ReadAsync())
            profiles.Add(ReadProfile(reader));

        return profiles;
    }

    private static StyleProfile ReadProfile(SqliteDataReader reader)
    {
        return new StyleProfile
        {
            Id = reader.GetInt32(0),
            PersonId = reader.GetInt32(1),
            ProfileJson = reader.GetString(2),
            Provider = reader.GetString(3),
            Model = reader.GetString(4),
            CreatedAt = DateTime.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal)
        };
    }
}
