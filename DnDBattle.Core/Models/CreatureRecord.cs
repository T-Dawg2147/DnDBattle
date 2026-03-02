namespace DnDBattle.Core.Models;

public record CreatureRecord(
    Guid Id,
    string Name,
    string CreatureType,
    Enums.CreatureSize Size,
    Enums.Alignment Alignment,
    int MaxHitPoints,
    int ArmorClass,
    int Speed,
    int Strength, int Dexterity, int Constitution,
    int Intelligence, int Wisdom, int Charisma,
    int ProficiencyBonus,
    string ImagePath = ""
);
