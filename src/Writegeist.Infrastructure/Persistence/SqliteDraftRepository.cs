using Microsoft.Data.Sqlite;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Persistence;

public class SqliteDraftRepository : IDraftRepository
{
    private readonly string _connectionString;

    public SqliteDraftRepository(SqliteDatabase database)
    {
        _connectionString = database.ConnectionString;
    }

    public async Task<GeneratedDraft> SaveAsync(GeneratedDraft draft)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO generated_drafts (person_id, style_profile_id, platform, topic, content, parent_draft_id, feedback, provider, model)
            VALUES (@personId, @styleProfileId, @platform, @topic, @content, @parentDraftId, @feedback, @provider, @model);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@personId", draft.PersonId);
        command.Parameters.AddWithValue("@styleProfileId", draft.StyleProfileId);
        command.Parameters.AddWithValue("@platform", draft.Platform.ToString());
        command.Parameters.AddWithValue("@topic", draft.Topic);
        command.Parameters.AddWithValue("@content", draft.Content);
        command.Parameters.AddWithValue("@parentDraftId", (object?)draft.ParentDraftId ?? DBNull.Value);
        command.Parameters.AddWithValue("@feedback", (object?)draft.Feedback ?? DBNull.Value);
        command.Parameters.AddWithValue("@provider", draft.Provider);
        command.Parameters.AddWithValue("@model", draft.Model);

        var id = (long)(await command.ExecuteScalarAsync())!;

        await using var readCmd = connection.CreateCommand();
        readCmd.CommandText = SelectColumns + " WHERE id = @id;";
        readCmd.Parameters.AddWithValue("@id", id);

        await using var reader = await readCmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return ReadDraft(reader);
    }

    public async Task<GeneratedDraft?> GetLatestAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + " ORDER BY created_at DESC, id DESC LIMIT 1;";

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return ReadDraft(reader);
    }

    public async Task<GeneratedDraft?> GetByIdAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + " WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return ReadDraft(reader);
    }

    private const string SelectColumns =
        "SELECT id, person_id, style_profile_id, platform, topic, content, parent_draft_id, feedback, provider, model, created_at FROM generated_drafts";

    private static GeneratedDraft ReadDraft(SqliteDataReader reader)
    {
        return new GeneratedDraft
        {
            Id = reader.GetInt32(0),
            PersonId = reader.GetInt32(1),
            StyleProfileId = reader.GetInt32(2),
            Platform = Enum.Parse<Platform>(reader.GetString(3)),
            Topic = reader.GetString(4),
            Content = reader.GetString(5),
            ParentDraftId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            Feedback = reader.IsDBNull(7) ? null : reader.GetString(7),
            Provider = reader.GetString(8),
            Model = reader.GetString(9),
            CreatedAt = DateTime.Parse(reader.GetString(10), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal)
        };
    }
}
