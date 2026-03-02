namespace DnDBattle.Data.Entities;

public sealed class CreatureEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CreatureType { get; set; } = string.Empty;
    public int Size { get; set; }
    public int Alignment { get; set; }
    public int MaxHitPoints { get; set; }
    public int ArmorClass { get; set; }
    public int Speed { get; set; }
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }
    public int ProficiencyBonus { get; set; }
    public string ImagePath { get; set; } = string.Empty;
}
