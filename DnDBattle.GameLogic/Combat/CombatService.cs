using DnDBattle.Core.Enums;
using DnDBattle.Core.Events;
using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using CommunityToolkit.Mvvm.Messaging;

namespace DnDBattle.GameLogic.Combat;

public sealed class CombatService : ICombatService
{
    private readonly IDiceService _dice;
    private readonly IMessenger _messenger;

    public CombatService(IDiceService dice, IMessenger messenger)
    {
        _dice = dice;
        _messenger = messenger;
    }

    public AttackOutcome ResolveAttack(Combatant attacker, Combatant target)
    {
        int attackRoll = _dice.Roll(20);
        bool isCritical = attackRoll == 20;
        bool isFumble = attackRoll == 1;

        int attackBonus = attacker.GetAbilityModifier(Ability.Strength) + attacker.ProficiencyBonus;
        int total = attackRoll + attackBonus;
        bool hit = isCritical || (!isFumble && total >= target.ArmorClass);

        int damage = 0;
        if (hit)
        {
            damage = _dice.Roll(isCritical ? 2 : 1, 8) + attacker.GetAbilityModifier(Ability.Strength);
            damage = Math.Max(0, damage);
            ApplyDamage(target, damage);
        }

        var outcome = new AttackOutcome(hit, isCritical, total, damage,
            DamageType.Bludgeoning,
            FormatDescription(attacker.Name, target.Name, attackRoll, total, hit, isCritical, damage));

        _messenger.Send(new CreatureDamagedMessage(target.Id, damage, DamageType.Bludgeoning));

        return outcome;
    }

    public SavingThrowOutcome ResolveSavingThrow(Combatant creature, Ability ability, int dc)
    {
        int roll = _dice.Roll(20);
        int modifier = creature.GetAbilityModifier(ability);
        int total = roll + modifier;
        return new SavingThrowOutcome(total >= dc, roll, total, dc, ability);
    }

    public int CalculateDamage(string diceExpression, DamageType damageType, bool isCritical)
    {
        int result = _dice.ParseAndRoll(diceExpression);
        return isCritical ? result + _dice.ParseAndRoll(diceExpression) : result;
    }

    private void ApplyDamage(Combatant target, int amount)
    {
        if (target.TemporaryHitPoints > 0)
        {
            int absorbed = Math.Min(target.TemporaryHitPoints, amount);
            target.TemporaryHitPoints -= absorbed;
            amount -= absorbed;
        }
        target.CurrentHitPoints = Math.Max(0, target.CurrentHitPoints - amount);
        if (!target.IsAlive)
            _messenger.Send(new CreatureDiedMessage(target.Id));
    }

    private static string FormatDescription(string attacker, string target, int roll, int total,
        bool hit, bool crit, int damage) =>
        crit ? $"CRITICAL HIT! {attacker} rolls {roll} (total {total}) vs {target} — {damage} damage!"
        : hit ? $"{attacker} hits {target} with a {total} — {damage} damage!"
        : $"{attacker} misses {target} with a {total}.";
}
