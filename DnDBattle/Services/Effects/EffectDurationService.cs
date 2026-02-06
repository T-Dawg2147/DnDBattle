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
using System;
using System.Collections.Generic;
using System.Linq;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Effects
{
    /// <summary>
    /// Manages duration tracking and damage-over-time for area effects.
    /// Integrates with initiative/round tracking.
    /// </summary>
    public class EffectDurationService
    {
        private readonly AreaEffectService _areaEffectService;

        /// <summary>
        /// Fired when a message should be logged (source, message)
        /// </summary>
        public event Action<string, string> LogMessage;

        /// <summary>
        /// Fired when an effect expires
        /// </summary>
        public event Action<AreaEffect> EffectExpired;

        /// <summary>
        /// Fired when an effect is about to expire (1 round remaining)
        /// </summary>
        public event Action<AreaEffect> EffectExpiringSoon;

        public EffectDurationService(AreaEffectService areaEffectService)
        {
            _areaEffectService = areaEffectService;
        }

        /// <summary>
        /// Called at the end of each round to tick down durations.
        /// Returns list of expired effect names.
        /// </summary>
        public List<string> OnRoundEnd()
        {
            if (!Options.EnableDurationTracking) return new List<string>();

            var expired = new List<string>();
            var toRemove = new List<AreaEffect>();

            foreach (var effect in _areaEffectService.ActiveEffects.ToList())
            {
                if (effect.DurationRounds <= 0) continue;

                effect.RoundsRemaining--;

                if (effect.RoundsRemaining == 1)
                {
                    LogMessage?.Invoke("Effect", $"⚠️ {effect.Name} expires next round!");
                    EffectExpiringSoon?.Invoke(effect);
                }
                else if (effect.RoundsRemaining <= 0)
                {
                    toRemove.Add(effect);
                    expired.Add(effect.Name ?? "Unknown");
                }
            }

            foreach (var effect in toRemove)
            {
                LogMessage?.Invoke("Effect", $"⏱️ {effect.Name} has expired.");
                EffectExpired?.Invoke(effect);
                _areaEffectService.RemoveEffect(effect);
            }

            return expired;
        }

        /// <summary>
        /// Processes damage-over-time for a token at the start of its turn.
        /// Returns list of damage descriptions.
        /// </summary>
        public List<string> OnTokenTurnStart(Token token, double gridCellSize)
        {
            if (!Options.EnableDamageOverTime) return new List<string>();

            var messages = new List<string>();

            foreach (var effect in _areaEffectService.ActiveEffects)
            {
                if (effect.DamageTiming != DamageTiming.StartOfTurn) continue;
                if (string.IsNullOrEmpty(effect.DamageExpression)) continue;

                if (AreaEffectService.IsCellInEffect(effect, token.GridX, token.GridY, gridCellSize))
                {
                    var result = ApplyEffectDamage(token, effect);
                    messages.Add(result);
                }
            }

            return messages;
        }

        /// <summary>
        /// Processes damage-over-time for a token at the end of its turn.
        /// Returns list of damage descriptions.
        /// </summary>
        public List<string> OnTokenTurnEnd(Token token, double gridCellSize)
        {
            if (!Options.EnableDamageOverTime) return new List<string>();

            var messages = new List<string>();

            foreach (var effect in _areaEffectService.ActiveEffects)
            {
                if (effect.DamageTiming != DamageTiming.EndOfTurn) continue;
                if (string.IsNullOrEmpty(effect.DamageExpression)) continue;

                if (AreaEffectService.IsCellInEffect(effect, token.GridX, token.GridY, gridCellSize))
                {
                    var result = ApplyEffectDamage(token, effect);
                    messages.Add(result);
                }
            }

            return messages;
        }

        /// <summary>
        /// Checks if a token entering a cell triggers any OnEnter damage.
        /// </summary>
        public List<string> OnTokenEnterCell(Token token, int gridX, int gridY, double gridCellSize)
        {
            if (!Options.EnableDamageOverTime) return new List<string>();

            var messages = new List<string>();

            foreach (var effect in _areaEffectService.ActiveEffects)
            {
                if (effect.DamageTiming != DamageTiming.OnEnter) continue;
                if (string.IsNullOrEmpty(effect.DamageExpression)) continue;

                if (AreaEffectService.IsCellInEffect(effect, gridX, gridY, gridCellSize))
                {
                    var result = ApplyEffectDamage(token, effect);
                    messages.Add(result);
                }
            }

            return messages;
        }

        private string ApplyEffectDamage(Token token, AreaEffect effect)
        {
            var roll = DiceRoller.RollExpression(effect.DamageExpression);
            var (damageTaken, description) = token.TakeDamage(roll.Total, effect.DamageType);

            string msg = $"🔥 {token.Name} takes {roll.Total} {effect.DamageType.GetDisplayName()} damage from {effect.Name}.";
            if (description != null)
                msg += $" ({description})";
            msg += $" HP: {token.HP}/{token.MaxHP}";

            LogMessage?.Invoke("DoT", msg);
            return msg;
        }

        /// <summary>
        /// Gets all effects at a specific grid position
        /// </summary>
        public List<AreaEffect> GetEffectsAtPosition(int gridX, int gridY, double gridCellSize)
        {
            var results = new List<AreaEffect>();
            foreach (var effect in _areaEffectService.ActiveEffects)
            {
                if (AreaEffectService.IsCellInEffect(effect, gridX, gridY, gridCellSize))
                {
                    results.Add(effect);
                }
            }
            return results;
        }
    }
}
