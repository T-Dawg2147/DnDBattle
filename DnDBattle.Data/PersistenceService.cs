using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using DnDBattle.Data.Serialization;
using DnDBattle.Data.Storage;
using Microsoft.Data.Sqlite;

namespace DnDBattle.Data;

public sealed class PersistenceService : IPersistenceService
{
    private readonly DatabaseContext _db;
    private readonly string _autosavePath;

    public PersistenceService(DatabaseContext db, string autosavePath)
    {
        _db = db;
        _autosavePath = autosavePath;
    }

    public Task SaveEncounterAsync(EncounterSnapshot snapshot, string filePath, CancellationToken ct = default) =>
        EncounterSerializer.SaveToFileAsync(snapshot, filePath, ct);

    public Task<EncounterSnapshot?> LoadEncounterAsync(string filePath, CancellationToken ct = default) =>
        EncounterSerializer.LoadFromFileAsync(filePath, ct);

    public async Task AutosaveAsync(EncounterSnapshot snapshot, CancellationToken ct = default)
    {
        var bytes = EncounterSerializer.Serialize(snapshot);
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Encounters(Id, Name, SavedAt, Data) VALUES(@id,@name,@date,@data)
            ON CONFLICT(Id) DO UPDATE SET Name=excluded.Name, SavedAt=excluded.SavedAt, Data=excluded.Data
            """;
        cmd.Parameters.AddWithValue("@id", snapshot.Id.ToString());
        cmd.Parameters.AddWithValue("@name", snapshot.Name);
        cmd.Parameters.AddWithValue("@date", snapshot.SavedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@data", bytes);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
