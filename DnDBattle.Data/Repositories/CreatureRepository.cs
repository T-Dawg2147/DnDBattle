using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using DnDBattle.Core.Enums;
using DnDBattle.Data.Storage;
using Microsoft.Data.Sqlite;

namespace DnDBattle.Data.Repositories;

public sealed class CreatureRepository : ICreatureRepository
{
    private readonly DatabaseContext _db;

    public CreatureRepository(DatabaseContext db) => _db = db;

    public async Task<IReadOnlyList<CreatureRecord>> GetAllAsync(CancellationToken ct = default)
    {
        var list = new List<CreatureRecord>();
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Creatures ORDER BY Name";
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(MapToRecord(reader));
        return list;
    }

    public async Task<CreatureRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Creatures WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapToRecord(reader) : null;
    }

    public async Task SaveAsync(CreatureRecord creature, CancellationToken ct = default)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Creatures VALUES (@id,@name,@type,@size,@align,@maxhp,@ac,@speed,
                @str,@dex,@con,@int,@wis,@cha,@prof,@img)
            ON CONFLICT(Id) DO UPDATE SET
                Name=excluded.Name, CreatureType=excluded.CreatureType,
                Size=excluded.Size, Alignment=excluded.Alignment,
                MaxHitPoints=excluded.MaxHitPoints, ArmorClass=excluded.ArmorClass,
                Speed=excluded.Speed, Strength=excluded.Strength,
                Dexterity=excluded.Dexterity, Constitution=excluded.Constitution,
                Intelligence=excluded.Intelligence, Wisdom=excluded.Wisdom,
                Charisma=excluded.Charisma, ProficiencyBonus=excluded.ProficiencyBonus,
                ImagePath=excluded.ImagePath
            """;
        cmd.Parameters.AddWithValue("@id", creature.Id.ToString());
        cmd.Parameters.AddWithValue("@name", creature.Name);
        cmd.Parameters.AddWithValue("@type", creature.CreatureType);
        cmd.Parameters.AddWithValue("@size", (int)creature.Size);
        cmd.Parameters.AddWithValue("@align", (int)creature.Alignment);
        cmd.Parameters.AddWithValue("@maxhp", creature.MaxHitPoints);
        cmd.Parameters.AddWithValue("@ac", creature.ArmorClass);
        cmd.Parameters.AddWithValue("@speed", creature.Speed);
        cmd.Parameters.AddWithValue("@str", creature.Strength);
        cmd.Parameters.AddWithValue("@dex", creature.Dexterity);
        cmd.Parameters.AddWithValue("@con", creature.Constitution);
        cmd.Parameters.AddWithValue("@int", creature.Intelligence);
        cmd.Parameters.AddWithValue("@wis", creature.Wisdom);
        cmd.Parameters.AddWithValue("@cha", creature.Charisma);
        cmd.Parameters.AddWithValue("@prof", creature.ProficiencyBonus);
        cmd.Parameters.AddWithValue("@img", creature.ImagePath);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Creatures WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static CreatureRecord MapToRecord(SqliteDataReader r) => new(
        Guid.Parse(r.GetString(0)),
        r.GetString(1),
        r.GetString(2),
        (CreatureSize)r.GetInt32(3),
        (Alignment)r.GetInt32(4),
        r.GetInt32(5), r.GetInt32(6), r.GetInt32(7),
        r.GetInt32(8), r.GetInt32(9), r.GetInt32(10),
        r.GetInt32(11), r.GetInt32(12), r.GetInt32(13),
        r.GetInt32(14), r.GetString(15)
    );
}
