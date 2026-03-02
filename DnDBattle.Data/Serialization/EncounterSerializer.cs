using DnDBattle.Core.Models;
using System.Text.Json;

namespace DnDBattle.Data.Serialization;

public static class EncounterSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static byte[] Serialize(EncounterSnapshot snapshot) =>
        JsonSerializer.SerializeToUtf8Bytes(new EncounterSerializable(snapshot), JsonOptions);

    public static EncounterSnapshot? Deserialize(byte[] data)
    {
        var serializable = JsonSerializer.Deserialize<EncounterSerializable>(data, JsonOptions);
        return serializable?.ToSnapshot();
    }

    public static async Task SaveToFileAsync(EncounterSnapshot snapshot, string path, CancellationToken ct = default)
    {
        var bytes = Serialize(snapshot);
        await File.WriteAllBytesAsync(path, bytes, ct);
    }

    public static async Task<EncounterSnapshot?> LoadFromFileAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return null;
        var bytes = await File.ReadAllBytesAsync(path, ct);
        return Deserialize(bytes);
    }
}

file sealed class EncounterSerializable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; }
    public List<CombatantSerializable> Combatants { get; set; } = new();
    public int CurrentRound { get; set; }
    public string? ActiveCombatantId { get; set; }

    public EncounterSerializable() { }

    public EncounterSerializable(EncounterSnapshot snap)
    {
        Id = snap.Id.ToString();
        Name = snap.Name;
        SavedAt = snap.SavedAt;
        Combatants = snap.Combatants.Select(c => new CombatantSerializable(c)).ToList();
        CurrentRound = snap.CurrentRound;
        ActiveCombatantId = snap.ActiveCombatantId?.ToString();
    }

    public EncounterSnapshot ToSnapshot() => new(
        Guid.Parse(Id), Name, SavedAt,
        Combatants.Select(c => c.ToRecord()).ToList(),
        CurrentRound,
        string.IsNullOrEmpty(ActiveCombatantId) ? null : Guid.Parse(ActiveCombatantId),
        null
    );
}

file sealed class CombatantSerializable
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int TempHp { get; set; }
    public int ArmorClass { get; set; }
    public int Initiative { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsPlayer { get; set; }
    public string ImagePath { get; set; } = string.Empty;

    public CombatantSerializable() { }

    public CombatantSerializable(CombatantSnapshot c)
    {
        Id = c.Id.ToString(); Name = c.Name; MaxHp = c.MaxHp;
        CurrentHp = c.CurrentHp; TempHp = c.TempHp; ArmorClass = c.ArmorClass;
        Initiative = c.Initiative; X = c.X; Y = c.Y; IsPlayer = c.IsPlayer;
        ImagePath = c.ImagePath;
    }

    public CombatantSnapshot ToRecord() => new(
        Guid.Parse(Id), Name, MaxHp, CurrentHp, TempHp,
        ArmorClass, Initiative, X, Y, IsPlayer, ImagePath
    );
}
