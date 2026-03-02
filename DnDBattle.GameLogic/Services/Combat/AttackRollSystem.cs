using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Utils;
using DnDBattle.Services;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using Action = DnDBattle.Models.Combat.Action;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Combat
{
    /// <summary>
    /// Handles attack rolls including advantage/disadvantage, critical hits,
    /// condition modifiers, cover, and damage application.
    /// </summary>
    public class AttackRollSystem
    {
        /// <summary>
        /// Rolls an attack from <paramref name="attacker"/> against <paramref name="defender"/>
        /// using the supplied <paramref name="attack"/> action.
        /// </summary>
        public AttackResult RollAttack(
            Token attacker,
            Token defender,
            Models.Combat.Action attack,
            AttackMode mode = AttackMode.Normal,
            CoverLevel cover = CoverLevel.None)
        {
            if (!Options.EnableAttackRollSystem)
                return new AttackResult { Attacker = attacker, Defender = defender, Attack = attack };

            var result = new AttackResult
            {
                Attacker = attacker,
                Defender = defender,
                Attack = attack,
                Mode = mode,
                Cover = cover
            };

            // ── Apply condition modifiers ──
            if (Options.EnableConditionAutomation)
            {
                var attackerMod = CombatConditionHelper.GetAttackModifier(attacker, defender);
                var defenderMod = CombatConditionHelper.GetDefenseModifier(defender);
                mode = CombineModifiers(mode, attackerMod, defenderMod);
            }

            // ── Roll d20 ──
            var roll1 = DiceRoller.RollExpression("1d20");
            var roll2 = DiceRoller.RollExpression("1d20");

            int d20Roll = mode switch
            {
                AttackMode.Advantage => Math.Max(roll1.Total, roll2.Total),
                AttackMode.Disadvantage => Math.Min(roll1.Total, roll2.Total),
                _ => roll1.Total
            };

            result.D20Roll = d20Roll;
            result.IsCriticalHit = d20Roll == 20;
            result.IsCriticalFumble = d20Roll == 1;

            // ── Auto-crit from conditions (paralyzed/unconscious within 5ft) ──
            if (Options.EnableConditionAutomation && CombatConditionHelper.IsAutoCrit(attacker, defender))
            {
                result.IsCriticalHit = true;
            }

            // ── Calculate total ──
            int attackBonus = attack.AttackBonus ?? 0;
            result.AttackBonus = attackBonus;
            result.TotalAttack = d20Roll + attackBonus;

            // ── Determine target AC with cover ──
            int targetAC = defender.ArmorClass;
            if (Options.EnableCoverSystem && cover != CoverLevel.None)
            {
                targetAC += cover switch
                {
                    CoverLevel.Half => 2,
                    CoverLevel.ThreeQuarters => 5,
                    _ => 0
                };
            }
            result.TargetAC = targetAC;

            // ── Hit determination ──
            result.Hit = result.IsCriticalHit ||
                         (!result.IsCriticalFumble && result.TotalAttack >= targetAC);

            // ── Full cover blocks targeting ──
            if (cover == CoverLevel.Full)
            {
                result.Hit = false;
            }

            // ── Roll damage if hit ──
            if (result.Hit && !string.IsNullOrWhiteSpace(attack.DamageExpression))
            {
                var dmgResult = DiceRoller.RollExpression(attack.DamageExpression);
                int damageRoll = dmgResult.Total;

                // Critical = double dice damage
                if (result.IsCriticalHit)
                {
                    var critExtra = DiceRoller.RollExpression(attack.DamageExpression);
                    damageRoll += critExtra.Total;
                    result.IsCriticalDamage = true;
                }

                result.DamageRoll = damageRoll;

                // Apply resistances/vulnerabilities
                var damageType = attack.GetEffectiveDamageType();
                var (actualDamage, desc) = defender.TakeDamage(damageRoll, damageType);
                result.ActualDamage = actualDamage;
                result.DamageDescription = desc;

                // ── Concentration check on damage ──
                if (Options.EnableConcentrationTracking && defender.IsConcentrating && actualDamage > 0)
                {
                    // Concentration check is handled externally; we note damage was dealt
                }
            }

            return result;
        }

        /// <summary>
        /// Combines player-chosen mode with condition-derived modifiers.
        /// Advantage + Disadvantage = Normal (5e rules).
        /// </summary>
        private static AttackMode CombineModifiers(AttackMode chosen, AttackMode attackerCondition, AttackMode defenderCondition)
        {
            bool hasAdvantage = chosen == AttackMode.Advantage
                || attackerCondition == AttackMode.Advantage
                || defenderCondition == AttackMode.Advantage;

            bool hasDisadvantage = chosen == AttackMode.Disadvantage
                || attackerCondition == AttackMode.Disadvantage
                || defenderCondition == AttackMode.Disadvantage;

            if (hasAdvantage && hasDisadvantage) return AttackMode.Normal;
            if (hasAdvantage) return AttackMode.Advantage;
            if (hasDisadvantage) return AttackMode.Disadvantage;
            return AttackMode.Normal;
        }
    }
}
