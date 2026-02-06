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
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.Combat;

namespace DnDBattle.Services.Combat
{
    /// <summary>
    /// Handles saving throw automation including batch saves, legendary resistance,
    /// condition-based auto-failures, and half-damage-on-success logic.
    /// </summary>
    public class SavingThrowSystem
    {
        /// <summary>
        /// Rolls a single saving throw for <paramref name="target"/>.
        /// </summary>
        public SavingThrowResult RollSave(Token target, Ability ability, int DC)
        {
            if (!Options.EnableSavingThrowAutomation)
                return new SavingThrowResult { Target = target, Ability = ability, DC = DC, Success = false };

            var result = new SavingThrowResult
            {
                Target = target,
                Ability = ability,
                DC = DC
            };

            // ── Auto-fail from conditions ──
            if (Options.EnableConditionAutomation && CombatConditionHelper.AutoFailSave(target, ability))
            {
                result.AutoFailed = true;
                result.Success = false;
                return result;
            }

            // ── Roll d20 ──
            var roll = DiceRoller.RollExpression("1d20");
            result.D20Roll = roll.Total;

            // ── Modifier ──
            int modifier = GetSaveModifier(target, ability);
            result.Modifier = modifier;
            result.Total = roll.Total + modifier;

            result.IsNaturalOne = roll.Total == 1;
            result.IsNaturalTwenty = roll.Total == 20;

            // ── Success determination ──
            result.Success = result.IsNaturalTwenty ||
                             (!result.IsNaturalOne && result.Total >= DC);

            // ── Legendary resistance ──
            if (!result.Success && target.LegendaryActionsMax > 0 && target.LegendaryActionsRemaining > 0)
            {
                // In automated mode, always use if available for non-player tokens
                if (!target.IsPlayer && Options.AutoRollMonsterSaves)
                {
                    target.LegendaryActionsRemaining--;
                    result.UsedLegendaryResistance = true;
                    result.Success = true;
                }
                // For player tokens or manual mode, this would be prompted via UI
            }

            return result;
        }

        /// <summary>
        /// Rolls saves for all tokens affected by an area effect.
        /// Returns damage dealt per token after save success/failure adjustments.
        /// </summary>
        public List<(SavingThrowResult save, int damage)> ResolveAreaSave(
            Ability saveAbility,
            int DC,
            string damageExpression,
            DamageType damageType,
            bool halfDamageOnSuccess,
            List<Token> affectedTokens)
        {
            var results = new List<(SavingThrowResult, int)>();

            // Roll damage once for all targets
            var dmgRoll = DiceRoller.RollExpression(damageExpression);
            int baseDamage = dmgRoll.Total;

            foreach (var token in affectedTokens)
            {
                var saveResult = RollSave(token, saveAbility, DC);

                int damage = baseDamage;
                if (saveResult.Success)
                {
                    damage = halfDamageOnSuccess ? baseDamage / 2 : 0;
                }

                // Apply resistances/vulnerabilities
                if (damage > 0)
                {
                    var (actualDamage, _) = token.TakeDamage(damage, damageType);
                    damage = actualDamage;
                }

                results.Add((saveResult, damage));
            }

            return results;
        }

        /// <summary>
        /// Gets the saving throw modifier for a given ability.
        /// </summary>
        public static int GetSaveModifier(Token token, Ability ability)
        {
            int score = ability switch
            {
                Ability.Strength => token.Str,
                Ability.Dexterity => token.Dex,
                Ability.Constitution => token.Con,
                Ability.Intelligence => token.Int,
                Ability.Wisdom => token.Wis,
                Ability.Charisma => token.Cha,
                _ => 10
            };
            return (score - 10) / 2;
        }
    }
}
