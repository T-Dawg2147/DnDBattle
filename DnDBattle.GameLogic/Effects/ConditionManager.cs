using DnDBattle.Core.Enums;
using DnDBattle.Core.Models;

namespace DnDBattle.GameLogic.Effects;

public static class ConditionManager
{
    public static void ApplyCondition(Combatant target, Condition condition) =>
        target.ActiveCondition = condition;

    public static void RemoveCondition(Combatant target) =>
        target.ActiveCondition = Condition.None;

    public static bool HasCondition(Combatant target, Condition condition) =>
        target.ActiveCondition == condition;

    public static bool GrantsAdvantageToAttackers(Condition condition) =>
        condition is Condition.Paralyzed or Condition.Unconscious or Condition.Stunned
            or Condition.Petrified;

    public static bool GivesDisadvantageOnAttacks(Condition condition) =>
        condition is Condition.Blinded or Condition.Poisoned or Condition.Frightened
            or Condition.Prone or Condition.Restrained;

    public static string GetConditionDescription(Condition condition) => condition switch
    {
        Condition.Blinded => "Can't see, fails sight-based checks, attacks have disadvantage.",
        Condition.Charmed => "Can't attack charmer, charmer has advantage on social checks.",
        Condition.Frightened => "Disadvantage on ability checks and attacks while source is visible.",
        Condition.Grappled => "Speed becomes 0.",
        Condition.Incapacitated => "Can't take actions or reactions.",
        Condition.Invisible => "Impossible to see without special sense; attacks have advantage.",
        Condition.Paralyzed => "Incapacitated, can't move/speak; auto-fail Str/Dex saves; hit attacks crit.",
        Condition.Petrified => "Transformed to stone, incapacitated, immune to poison and disease.",
        Condition.Poisoned => "Disadvantage on attack rolls and ability checks.",
        Condition.Prone => "Attacks against have advantage if adjacent, disadvantage if ranged.",
        Condition.Restrained => "Speed 0, attacks have disadvantage, Dex saves disadvantage.",
        Condition.Stunned => "Incapacitated, can't move, auto-fail Str/Dex saves.",
        Condition.Unconscious => "Incapacitated, can't move/speak, unaware of surroundings.",
        _ => "No special effect."
    };
}
