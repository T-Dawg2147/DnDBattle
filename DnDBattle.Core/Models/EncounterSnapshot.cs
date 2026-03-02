namespace DnDBattle.Core.Models;

public record EncounterSnapshot(
    Guid Id,
    string Name,
    DateTime SavedAt,
    IReadOnlyList<CombatantSnapshot> Combatants,
    int CurrentRound,
    Guid? ActiveCombatantId,
    TileMapSnapshot? Map
);

public record CombatantSnapshot(
    Guid Id, string Name, int MaxHp, int CurrentHp, int TempHp,
    int ArmorClass, int Initiative, double X, double Y, bool IsPlayer,
    string ImagePath
);

public record TileMapSnapshot(int Width, int Height, string SerializedData);
