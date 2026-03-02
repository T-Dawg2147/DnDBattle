using Microsoft.Data.Sqlite;

namespace DnDBattle.Data.Storage;

public sealed class DatabaseContext : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public DatabaseContext(string databasePath)
    {
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        InitializeSchema();
    }

    public SqliteConnection Connection => _connection;

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Creatures (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                CreatureType TEXT NOT NULL,
                Size INTEGER NOT NULL,
                Alignment INTEGER NOT NULL,
                MaxHitPoints INTEGER NOT NULL,
                ArmorClass INTEGER NOT NULL,
                Speed INTEGER NOT NULL,
                Strength INTEGER NOT NULL,
                Dexterity INTEGER NOT NULL,
                Constitution INTEGER NOT NULL,
                Intelligence INTEGER NOT NULL,
                Wisdom INTEGER NOT NULL,
                Charisma INTEGER NOT NULL,
                ProficiencyBonus INTEGER NOT NULL,
                ImagePath TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS Encounters (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                SavedAt TEXT NOT NULL,
                Data BLOB NOT NULL
            );

            CREATE TABLE IF NOT EXISTS TileMaps (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Width INTEGER NOT NULL,
                Height INTEGER NOT NULL,
                Data BLOB NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }
}
