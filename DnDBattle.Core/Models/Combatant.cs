using CommunityToolkit.Mvvm.ComponentModel;
using DnDBattle.Core.Enums;

namespace DnDBattle.Core.Models;

public partial class Combatant : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _maxHitPoints;
    [ObservableProperty] private int _currentHitPoints;
    [ObservableProperty] private int _temporaryHitPoints;
    [ObservableProperty] private int _armorClass;
    [ObservableProperty] private int _initiativeRoll;
    [ObservableProperty] private double _positionX;
    [ObservableProperty] private double _positionY;
    [ObservableProperty] private bool _isPlayer;
    [ObservableProperty] private bool _isConcentrating;
    [ObservableProperty] private string _imagePath = string.Empty;
    [ObservableProperty] private CreatureSize _size = CreatureSize.Medium;
    [ObservableProperty] private Condition _activeCondition = Condition.None;

    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }
    public int ProficiencyBonus { get; set; } = 2;
    public int Speed { get; set; } = 30;

    public int GetAbilityModifier(Ability ability) => ability switch
    {
        Ability.Strength => (Strength - 10) / 2,
        Ability.Dexterity => (Dexterity - 10) / 2,
        Ability.Constitution => (Constitution - 10) / 2,
        Ability.Intelligence => (Intelligence - 10) / 2,
        Ability.Wisdom => (Wisdom - 10) / 2,
        Ability.Charisma => (Charisma - 10) / 2,
        _ => 0
    };

    public bool IsAlive => CurrentHitPoints > 0;
    public bool IsDowned => CurrentHitPoints <= 0 && MaxHitPoints > 0;

    public static Combatant FromRecord(CreatureRecord record) => new()
    {
        Name = record.Name,
        MaxHitPoints = record.MaxHitPoints,
        CurrentHitPoints = record.MaxHitPoints,
        ArmorClass = record.ArmorClass,
        Speed = record.Speed,
        Strength = record.Strength,
        Dexterity = record.Dexterity,
        Constitution = record.Constitution,
        Intelligence = record.Intelligence,
        Wisdom = record.Wisdom,
        Charisma = record.Charisma,
        ImagePath = record.ImagePath
    };
}
