using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Services;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Combat
{
    public class TargetingState
    {
        public bool IsTargeting { get; set; }
        public Token SourceToken { get; set; }
        public Models.Combat.Action SelectedAction { get; set; }
        public int ActionRange { get; set; }
        public bool IsRangedAction { get; set; }
        public bool IsMeleeAction { get; set; }
        public DamageType DamageType { get; set; }
    }

    public class ActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? AttackRoll { get; set; }
        public int? AttackTotal { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalMiss { get; set; }
        public int? DamageRoll { get; set; }
        public int? EffectiveDamage { get; set; }
        public DamageType DamageType { get; set; }
        public string DamageModification { get; set; }
        public Token Source { get; set; }
        public Token Target { get; set; }
        public Models.Combat.Action Action { get; set; }

        public bool TargetWasConcentrating { get; set; }
        public int DamageForConcentration { get; set; }
    }

    public class ActionTargetingService
    {
        public TargetingState CurrentState { get; private set; } = new TargetingState();

        public event System.Action<TargetingState> TargetingStarted;
        public event System.Action TargetingCancelled;
        public event System.Action<ActionResult> ActionResolved;
        public event System.Action<string> LogMessage;

        public void StartTargeting(Token source, Models.Combat.Action action)
        {
            if (source == null || action == null) return;

            CurrentState = new TargetingState()
            {
                IsTargeting = true,
                SourceToken = source,
                SelectedAction = action,
                ActionRange = ParseRange(action.Range),
                IsRangedAction = IsRanged(action.Range),
                IsMeleeAction = IsMelee(action.Range),
                DamageType = ParseDamageType(action)
            };

            TargetingStarted?.Invoke(CurrentState);
            LogMessage?.Invoke($"🎯 Select a target for {action.Name}");
        }

        public void CancelTargeting()
        {
            CurrentState = new TargetingState { IsTargeting = false };
            TargetingCancelled?.Invoke();
        }

        public (bool isValid, string reason) ValidateTarget(Token target, double gridCellSize)
        {
            if (!CurrentState.IsTargeting)
                return (false, "Not in targeting mode");

            if (target == null)
                return (false, "No target selected");

            if (target.Id == CurrentState.SourceToken.Id)
                return (false, "Cannot target yourself");

            int distance = CalculateDistance(CurrentState.SourceToken, target);

            int effectiveRange = CurrentState.ActionRange;

            if (CurrentState.IsMeleeAction)
            {
                int meleeRange = CurrentState.ActionRange > 0 ? CurrentState.ActionRange : 1;

                if (distance > meleeRange)
                {
                    int movementNeeded = distance - meleeRange;
                    int movementRemaining = CurrentState.SourceToken.MovementRemainingThisTurn;

                    if (movementNeeded > movementRemaining)
                    {
                        return (false, $"Out of range! Need {movementNeeded * 5} ft movement, have {movementRemaining * 5} ft");
                    }
                }
            }
            else if (CurrentState.IsRangedAction)
            {
                if (distance > effectiveRange)
                    return (false, $"Out of range! Target is {distance * 5} ft away, max range is {effectiveRange * 5} ft");
            }

            return (true, null);
        }

        /// <summary>
        /// Attempts to use the current action on a target
        /// </summary>
        public ActionResult UseActionOnTarget(Token target, bool manualRollMode, int? manualAttackRoll = null, int? manualDamageRoll = null)
        {
            var result = new ActionResult
            {
                Source = CurrentState.SourceToken,
                Target = target,
                Action = CurrentState.SelectedAction,
                DamageType = CurrentState.DamageType
            };

            // Validate target first
            var (isValid, reason) = ValidateTarget(target, 1);
            if (!isValid)
            {
                result.Success = false;
                result.Message = reason;
                return result;
            }

            var action = CurrentState.SelectedAction;
            var source = CurrentState.SourceToken;

            // Calculate distance and use movement if needed (for melee)
            if (CurrentState.IsMeleeAction)
            {
                int distance = CalculateDistance(source, target);
                int meleeRange = CurrentState.ActionRange > 0 ? CurrentState.ActionRange : 1;

                if (distance > meleeRange)
                {
                    int movementNeeded = distance - meleeRange;
                    source.TryUseMovement(movementNeeded);
                }
            }

            // Roll attack if action has attack bonus
            if (action.AttackBonus != null && action.AttackBonus != 0)
            {
                int attackRoll;

                if (manualRollMode && manualAttackRoll.HasValue)
                {
                    attackRoll = manualAttackRoll.Value;
                }
                else
                {
                    var roll = Utils.DiceRoller.RollExpression("1d20");
                    attackRoll = roll.Total;
                }

                result.AttackRoll = attackRoll;
                result.AttackTotal = attackRoll + (action.AttackBonus ?? 0);
                result.IsCriticalHit = attackRoll == 20;
                result.IsCriticalMiss = attackRoll == 1;

                // Check if attack hits (compare to AC)
                if (result.IsCriticalMiss)
                {
                    result.Success = false;
                    result.Message = $"💀 Critical Miss! {source.Name} misses {target.Name}";
                    ActionResolved?.Invoke(result);
                    CancelTargeting();
                    return result;
                }

                bool hits = result.IsCriticalHit || result.AttackTotal >= target.ArmorClass;

                if (!hits)
                {
                    result.Success = false;
                    result.Message = $"🛡️ {source.Name} attacks {target.Name} with {action.Name}: {result.AttackTotal} vs AC {target.ArmorClass} - Miss!";
                    ActionResolved?.Invoke(result);
                    CancelTargeting();
                    return result;
                }
            }

            // Roll damage
            if (!string.IsNullOrEmpty(action.DamageExpression))
            {
                int damageRoll;

                if (manualRollMode && manualDamageRoll.HasValue)
                {
                    damageRoll = manualDamageRoll.Value;
                }
                else
                {
                    var roll = Utils.DiceRoller.RollExpression(action.DamageExpression);
                    damageRoll = roll.Total;

                    // Double dice on critical hit
                    if (result.IsCriticalHit)
                    {
                        var critRoll = Utils.DiceRoller.RollExpression(action.DamageExpression);
                        damageRoll += critRoll.Total;
                    }
                }

                result.DamageRoll = damageRoll;

                // Apply damage to target with damage type consideration
                var (effectiveDamage, damageDesc) = target.TakeDamage(damageRoll, CurrentState.DamageType);
                result.EffectiveDamage = effectiveDamage;
                result.DamageModification = damageDesc;

                // Build result message
                string critText = result.IsCriticalHit ? " ⚡CRITICAL HIT!" : "";
                string damageTypeIcon = CurrentState.DamageType.GetIcon();
                string damageTypeText = CurrentState.DamageType.GetDisplayName();

                if (!string.IsNullOrEmpty(damageDesc))
                {
                    result.Message = $"🎯 {source.Name} hits {target.Name} with {action.Name}!{critText}\n" +
                                    $"{damageTypeIcon} {damageRoll} {damageTypeText} damage → {effectiveDamage} ({damageDesc})";
                }
                else
                {
                    result.Message = $"🎯 {source.Name} hits {target.Name} with {action.Name}!{critText}\n" +
                                    $"{damageTypeIcon} {effectiveDamage} {damageTypeText} damage!";
                }

                result.Success = true;
            }
            else
            {
                // Action without damage (buff, utility, etc.)
                result.Success = true;
                result.Message = $"✨ {source.Name} uses {action.Name} on {target.Name}";
            }

            ActionResolved?.Invoke(result);
            CancelTargeting();
            return result;
        }

        public List<Token> GetValidTargets(IEnumerable<Token> allTokens, double gridCellSize)
        {
            if (!CurrentState.IsTargeting)
                return new List<Token>();

            return allTokens
                .Where(t => ValidateTarget(t, gridCellSize).isValid)
                .ToList();
        }

        #region Helpers

        private int CalculateDistance(Token a, Token b)
        {
            // D&D uses the "every other diagonal costs 2" rule, but for simplicity
            // we'll use Chebyshev distance (max of x/y difference)
            int dx = Math.Abs(a.GridX - b.GridX);
            int dy = Math.Abs(a.GridY - b.GridY);
            return Math.Max(dx, dy);
        }

        private int ParseRange(string range)
        {
            if (string.IsNullOrEmpty(range))
                return 1; // Default melee range

            range = range.ToLower();

            // Parse "X ft" or "X/Y ft" format
            var numbers = System.Text.RegularExpressions.Regex.Matches(range, @"\d+");
            if (numbers.Count > 0)
            {
                if (int.TryParse(numbers[0].Value, out int feet))
                {
                    return feet / 5; // Convert feet to squares
                }
            }

            // Check for reach
            if (range.Contains("reach"))
            {
                if (range.Contains("10")) return 2;
                if (range.Contains("15")) return 3;
                return 2; // Default reach
            }

            return 1; // Default
        }

        private bool IsRanged(string range)
        {
            if (string.IsNullOrEmpty(range))
                return false;

            range = range.ToLower();
            return range.Contains("range") ||
                   range.Contains("ranged") ||
                   (int.TryParse(System.Text.RegularExpressions.Regex.Match(range, @"\d+").Value, out int feet) && feet > 10);
        }

        private bool IsMelee(string range)
        {
            if (string.IsNullOrEmpty(range))
                return true;

            range = range.ToLower();
            return range.Contains("melee") || range.Contains("reach") || !IsRanged(range);
        }

        private DamageType ParseDamageType(Models.Combat.Action action)
        {
            // Try to infer damage type from action description or name
            string text = $"{action.Name} {action.Description} {action.DamageExpression}".ToLower();

            if (text.Contains("fire")) return DamageType.Fire;
            if (text.Contains("cold") || text.Contains("frost") || text.Contains("ice")) return DamageType.Cold;
            if (text.Contains("lightning") || text.Contains("electric")) return DamageType.Lightning;
            if (text.Contains("thunder")) return DamageType.Thunder;
            if (text.Contains("acid")) return DamageType.Acid;
            if (text.Contains("poison")) return DamageType.Poison;
            if (text.Contains("necrotic")) return DamageType.Necrotic;
            if (text.Contains("radiant") || text.Contains("holy")) return DamageType.Radiant;
            if (text.Contains("force")) return DamageType.Force;
            if (text.Contains("psychic")) return DamageType.Psychic;
            if (text.Contains("bludgeoning") || text.Contains("club") || text.Contains("hammer") || text.Contains("fist")) return DamageType.Bludgeoning;
            if (text.Contains("piercing") || text.Contains("arrow") || text.Contains("spear") || text.Contains("rapier")) return DamageType.Piercing;
            if (text.Contains("slashing") || text.Contains("sword") || text.Contains("claw") || text.Contains("axe")) return DamageType.Slashing;

            // Default to slashing for most attacks
            return DamageType.Slashing;
        }

        #endregion
    }
}
