using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using DnDBattle.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Services.TileService
{
    /// <summary>
    /// Result of a roll (for UI display)
    /// </summary>
    public class RollResult
    {
        public int Roll { get; set; }
        public int Modifier { get; set; }
        public int Total => Roll + Modifier;
        public int DC { get; set; }
        public bool Success => Total >= DC;
    }

    /// <summary>
    /// Handles trap detection, disarming, and triggering
    /// </summary>
    public class TrapTriggerService
    {
        public event Action<string> LogMessage;
        public event Action<Token, TrapMetadata> TrapTriggered;
        public event Action<Token, TrapMetadata> TrapDetected;
        public event Action<Token, TrapMetadata> TrapDisarmed;

        // ===== NEW: Events for manual roll prompts =====
        public event Func<Token, TrapMetadata, string, int, (bool proceed, int roll)> RequestManualRoll;

        /// <summary>
        /// Check if a token triggers a trap when entering a tile
        /// </summary>
        public void CheckForTraps(Token token, Tile tile)
        {
            if (tile == null || token == null)
                return;

            var traps = tile.GetMetadata(TileMetadataType.Trap).OfType<TrapMetadata>().ToList();

            foreach (var trap in traps)
            {
                if (trap.CanTrigger())
                {
                    // Check trigger type
                    if (trap.TriggerType == TrapTriggerType.Pressure ||
                        trap.TriggerType == TrapTriggerType.Proximity)
                    {
                        // Automatic detection check (passive Perception)
                        bool autoDetected = token.PassivePerception >= trap.DetectionDC;

                        if (autoDetected && !trap.IsDetected)
                        {
                            trap.IsDetected = true;
                            LogMessage?.Invoke($"🔍 {token.Name} detected a trap! (Passive Perception {token.PassivePerception} vs DC {trap.DetectionDC})");
                            TrapDetected?.Invoke(token, trap);
                            return; // Don't trigger if detected
                        }

                        // Trigger the trap if not detected or disarmed
                        if (!trap.IsDetected && !trap.IsDisarmed)
                        {
                            TriggerTrap(token, trap);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to detect a trap (active Perception check)
        /// </summary>
        public (bool success, int roll, int total) AttemptDetection(Token token, TrapMetadata trap, int? manualRoll = null)
        {
            if (trap.IsDetected)
                return (true, 0, 0);

            int roll;
            int modifier = token.WisMod; // Perception uses WIS

            if (trap.AutoRollDetection)
            {
                // Auto-roll
                var diceRoll = DiceRoller.RollExpression("1d20");
                roll = diceRoll.Total;
            }
            else
            {
                // Manual roll - prompt DM if not provided
                if (manualRoll.HasValue)
                {
                    roll = manualRoll.Value;
                }
                else
                {
                    // Prompt for manual input via event
                    var result = RequestManualRoll?.Invoke(token, trap, "Perception", trap.DetectionDC);
                    if (result.HasValue && result.Value.proceed)
                    {
                        roll = result.Value.roll;
                    }
                    else
                    {
                        return (false, 0, 0); // Cancelled
                    }
                }
            }

            int total = roll + modifier;
            bool success = total >= trap.DetectionDC;

            if (success)
            {
                trap.IsDetected = true;
                LogMessage?.Invoke($"🔍 {token.Name} detected a trap! ({roll}+{modifier}={total} vs DC {trap.DetectionDC})");
                TrapDetected?.Invoke(token, trap);
            }
            else
            {
                LogMessage?.Invoke($"❌ {token.Name} failed to detect a trap. ({roll}+{modifier}={total} vs DC {trap.DetectionDC})");
            }

            return (success, roll, total);
        }

        /// <summary>
        /// Attempt to disarm a trap (Thieves' Tools or other skill)
        /// </summary>
        public (bool success, int roll, int total) AttemptDisarm(Token token, TrapMetadata trap, int? manualRoll = null)
        {
            if (!trap.CanBeDisarmed)
            {
                LogMessage?.Invoke($"⚠️ This trap cannot be disarmed!");
                return (false, 0, 0);
            }

            if (trap.IsDisarmed)
                return (true, 0, 0);

            int roll;
            int modifier = token.DexMod; // Assume DEX for thieves' tools

            if (trap.AutoRollDisarm)
            {
                var diceRoll = DiceRoller.RollExpression("1d20");
                roll = diceRoll.Total;
            }
            else
            {
                if (manualRoll.HasValue)
                {
                    roll = manualRoll.Value;
                }
                else
                {
                    var result = RequestManualRoll?.Invoke(token, trap, trap.DisarmSkill, trap.DisarmDC);
                    if (result.HasValue && result.Value.proceed)
                    {
                        roll = result.Value.roll;
                    }
                    else
                    {
                        return (false, 0, 0);
                    }
                }
            }

            int total = roll + modifier;
            bool success = total >= trap.DisarmDC;

            if (success)
            {
                trap.IsDisarmed = true;
                LogMessage?.Invoke($"✅ {token.Name} successfully disarmed the trap! ({roll}+{modifier}={total} vs DC {trap.DisarmDC})");
                TrapDisarmed?.Invoke(token, trap);
            }
            else
            {
                LogMessage?.Invoke($"❌ {token.Name} failed to disarm the trap. ({roll}+{modifier}={total} vs DC {trap.DisarmDC})");

                if (trap.FailedDisarmTriggersTrap)
                {
                    LogMessage?.Invoke($"💥 The trap is triggered by the failed attempt!");
                    TriggerTrap(token, trap);
                }
            }

            return (success, roll, total);
        }

        /// <summary>
        /// Trigger a trap and apply effects
        /// </summary>
        public void TriggerTrap(Token token, TrapMetadata trap, int? manualSaveRoll = null, int? manualDamageRoll = null)
        {
            if (!trap.CanTrigger())
                return;

            trap.IsTriggered = true;
            trap.TimesTriggered++;

            LogMessage?.Invoke($"⚠️ {trap.TriggerDescription}");
            LogMessage?.Invoke($"💥 {trap.EffectDescription}");

            // ===== Saving Throw =====
            int saveRoll;
            int saveMod = GetSaveModifier(token, trap.SaveAbility);

            if (trap.AutoRollSave)
            {
                var diceRoll = DiceRoller.RollExpression("1d20");
                saveRoll = diceRoll.Total;
            }
            else
            {
                if (manualSaveRoll.HasValue)
                {
                    saveRoll = manualSaveRoll.Value;
                }
                else
                {
                    var result = RequestManualRoll?.Invoke(token, trap, $"{trap.SaveAbility} Save", trap.SaveDC);
                    if (result.HasValue && result.Value.proceed)
                    {
                        saveRoll = result.Value.roll;
                    }
                    else
                    {
                        return; // Cancelled
                    }
                }
            }

            int saveTotal = saveRoll + saveMod;
            bool savedSuccessfully = saveTotal >= trap.SaveDC;

            LogMessage?.Invoke($"🎲 {token.Name} rolls {trap.SaveAbility} save: {saveRoll}+{saveMod}={saveTotal} vs DC {trap.SaveDC}");

            // ===== Calculate Damage =====
            int damage;

            if (trap.AutoRollDamage)
            {
                var damageRoll = DiceRoller.RollExpression(trap.DamageDice);
                damage = damageRoll.Total;
            }
            else
            {
                if (manualDamageRoll.HasValue)
                {
                    damage = manualDamageRoll.Value;
                }
                else
                {
                    // Prompt for damage
                    LogMessage?.Invoke($"💬 DM: Please roll {trap.DamageDice} for damage.");
                    // For now, auto-roll as fallback
                    var damageRoll = DiceRoller.RollExpression(trap.DamageDice);
                    damage = damageRoll.Total;
                }
            }

            if (savedSuccessfully && trap.HalfDamageOnSave)
            {
                damage = damage / 2;
                LogMessage?.Invoke($"✅ {token.Name} saved! Damage halved to {damage}.");
            }
            else if (!savedSuccessfully)
            {
                LogMessage?.Invoke($"❌ {token.Name} failed the save!");
            }

            // Apply damage
            var (effectiveDamage, desc) = token.TakeDamage(damage, trap.DamageType);
            LogMessage?.Invoke($"{trap.DamageType.GetIcon()} {token.Name} takes {effectiveDamage} {trap.DamageType.GetDisplayName()} damage!");

            // Apply additional effects
            foreach (var effect in trap.AdditionalEffects)
            {
                LogMessage?.Invoke($"✨ Additional effect: {effect}");
            }

            TrapTriggered?.Invoke(token, trap);
        }

        private int GetSaveModifier(Token token, string ability)
        {
            return ability.ToUpper() switch
            {
                "STR" => token.StrMod,
                "DEX" => token.DexMod,
                "CON" => token.ConMod,
                "INT" => token.IntMod,
                "WIS" => token.WisMod,
                "CHA" => token.ChaMod,
                _ => 0
            };
        }
    }
}