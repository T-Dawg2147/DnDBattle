using DnDBattle.Core.Models;

namespace DnDBattle.Core.Interfaces;

public interface ICombatService
{
    AttackOutcome ResolveAttack(Combatant attacker, Combatant target);
    SavingThrowOutcome ResolveSavingThrow(Combatant creature, Enums.Ability ability, int dc);
    int CalculateDamage(string diceExpression, Enums.DamageType damageType, bool isCritical);
}
