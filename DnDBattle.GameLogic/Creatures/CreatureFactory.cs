using DnDBattle.Core.Models;

namespace DnDBattle.GameLogic.Creatures;

public static class CreatureFactory
{
    public static Combatant CreateFromRecord(CreatureRecord record) =>
        Combatant.FromRecord(record);

    public static Combatant CreatePlayer(string name, int armorClass, int maxHp,
        int str, int dex, int con, int intel, int wis, int cha, int speed = 30) => new()
    {
        Name = name,
        ArmorClass = armorClass,
        MaxHitPoints = maxHp,
        CurrentHitPoints = maxHp,
        Strength = str, Dexterity = dex, Constitution = con,
        Intelligence = intel, Wisdom = wis, Charisma = cha,
        Speed = speed,
        IsPlayer = true
    };

    public static Combatant CreateMonster(string name, int armorClass, int maxHp,
        int str = 10, int dex = 10, int con = 10, int intel = 10, int wis = 10, int cha = 10,
        int speed = 30, int proficiencyBonus = 2) => new()
    {
        Name = name,
        ArmorClass = armorClass,
        MaxHitPoints = maxHp,
        CurrentHitPoints = maxHp,
        Strength = str, Dexterity = dex, Constitution = con,
        Intelligence = intel, Wisdom = wis, Charisma = cha,
        Speed = speed,
        ProficiencyBonus = proficiencyBonus,
        IsPlayer = false
    };
}
