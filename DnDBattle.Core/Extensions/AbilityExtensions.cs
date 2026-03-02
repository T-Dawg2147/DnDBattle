using DnDBattle.Core.Enums;

namespace DnDBattle.Core.Extensions;

public static class AbilityExtensions
{
    public static int ToModifier(this int abilityScore) => (abilityScore - 10) / 2;

    public static string ToModifierString(this int abilityScore)
    {
        int mod = abilityScore.ToModifier();
        return mod >= 0 ? $"+{mod}" : mod.ToString();
    }

    public static Ability GetSavingThrowAbility(this Enums.Skill skill) => skill switch
    {
        Enums.Skill.Athletics => Ability.Strength,
        Enums.Skill.Acrobatics or Enums.Skill.SleightOfHand or Enums.Skill.Stealth => Ability.Dexterity,
        Enums.Skill.Arcana or Enums.Skill.History or Enums.Skill.Investigation
            or Enums.Skill.Nature or Enums.Skill.Religion => Ability.Intelligence,
        Enums.Skill.AnimalHandling or Enums.Skill.Insight or Enums.Skill.Medicine
            or Enums.Skill.Perception or Enums.Skill.Survival => Ability.Wisdom,
        Enums.Skill.Deception or Enums.Skill.Intimidation
            or Enums.Skill.Performance or Enums.Skill.Persuasion => Ability.Charisma,
        _ => Ability.Strength
    };
}
