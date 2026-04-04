using Microsoft.Data.Sqlite;
using Writegeist.Core.Interfaces;
using Writegeist.Core.Models;

namespace Writegeist.Infrastructure.Persistence;

public class SqlitePersonRepository : IPersonRepository
{
    private readonly string _connectionString;

    public SqlitePersonRepository(SqliteDatabase database)
    {
        _connectionString = database.ConnectionString;
    }

    public async Task<Person> CreateAsync(string name)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO persons (name) VALUES (@name);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@name", name);

        var id = (long)(await command.ExecuteScalarAsync())!;

        await using var readCmd = connection.CreateCommand();
        readCmd.CommandText = "SELECT id, name, created_at FROM persons WHERE id = @id;";
        readCmd.Parameters.AddWithValue("@id", id);

        await using var reader = await readCmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return ReadPerson(reader);
    }

    public async Task<Person?> GetByNameAsync(string name)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, created_at FROM persons WHERE name = @name COLLATE NOCASE;";
        command.Parameters.AddWithValue("@name", name);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return ReadPerson(reader);
    }

    public async Task<IReadOnlyList<Person>> GetAllAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, created_at FROM persons ORDER BY name;";

        await using var reader = await command.ExecuteReaderAsync();
        var persons = new List<Person>();
        while (await reader.ReadAsync())
            persons.Add(ReadPerson(reader));

        return persons;
    }

    public async Task<Person> GetOrCreateAsync(string name)
    {
        var existing = await GetByNameAsync(name);
        if (existing is not null)
            return existing;

        return await CreateAsync(name);
    }

    private static Person ReadPerson(SqliteDataReader reader)
    {
        return new Person
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            CreatedAt = DateTime.Parse(reader.GetString(2))
        };
    }
}
