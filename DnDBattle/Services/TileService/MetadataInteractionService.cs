using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using DnDBattle.Utils;
using System;
using System.Linq;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
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
    /// Handles all metadata interactions (secrets, interactives, hazards, etc.)
    /// </summary>
    public class MetadataInteractionService
    {
        public event Action<string> LogMessage;
        public event Action<Token, int> TokenTeleported;
        public event Action<Token, int> TokenHealed;
        public event Action<Token, HazardMetadata> HazardTriggered;
        public event Action<SecretMetadata> SecretDiscovered;
        public event Action<InteractiveMetadata> ObjectActivated;
        public event Func<Token, TileMetadata, string, int, (bool proceed, int roll)> RequestManualRoll;

        #region Secret Handling

        public (bool success, int roll, int total) AttemptSecretDiscovery(Token token, SecretMetadata secret, int? manualRoll = null)
        {
            if (secret.IsDiscovered)
                return (true, 0, 0);

            int roll;
            int modifier = token.IntMod; // Investigation uses INT

            if (manualRoll.HasValue)
            {
                roll = manualRoll.Value;
            }
            else
            {
                var result = RequestManualRoll?.Invoke(token, secret, "Investigation", secret.InvestigationDC);
                if (result.HasValue && result.Value.proceed)
                {
                    roll = result.Value.roll;
                }
                else
                {
                    return (false, 0, 0);
                }
            }

            int total = roll + modifier;
            bool success = total >= secret.InvestigationDC;

            if (success)
            {
                secret.IsDiscovered = true;
                secret.IsVisibleToPlayers = true;
                LogMessage?.Invoke($"🔍 {token.Name} discovered a secret! ({roll}+{modifier}={total} vs DC {secret.InvestigationDC})");
                LogMessage?.Invoke($"✨ {secret.DiscoveryDescription}");
                SecretDiscovered?.Invoke(secret);
            }
            else
            {
                LogMessage?.Invoke($"❌ {token.Name} failed to find anything. ({roll}+{modifier}={total} vs DC {secret.InvestigationDC})");
            }

            return (success, roll, total);
        }

        #endregion

        #region Interactive Object Handling

        public void InteractWithObject(Token token, InteractiveMetadata interactive)
        {
            if (!interactive.IsEnabled)
            {
                LogMessage?.Invoke($"⚙️ {interactive.Name} is inactive.");
                return;
            }

            if (interactive.SingleUse && interactive.TimesActivated > 0)
            {
                LogMessage?.Invoke($"⚙️ {interactive.Name} has already been used.");
                return;
            }

            if (interactive.IsLocked)
            {
                LogMessage?.Invoke($"🔒 {interactive.LockedDescription}");
                return;
            }

            // Check if requires skill check
            if (interactive.RequiresCheck)
            {
                var result = RequestManualRoll?.Invoke(token, interactive, interactive.RequiredSkill, interactive.CheckDC);
                if (result.HasValue && result.Value.proceed)
                {
                    int roll = result.Value.roll;
                    int mod = GetSkillModifier(token, interactive.RequiredSkill);
                    int total = roll + mod;

                    if (total < interactive.CheckDC)
                    {
                        LogMessage?.Invoke($"❌ {token.Name} failed to activate {interactive.Name}. ({roll}+{mod}={total} vs DC {interactive.CheckDC})");
                        return;
                    }
                }
                else
                {
                    return; // Cancelled
                }
            }

            // Activate the object
            interactive.TimesActivated++;
            interactive.IsTriggered = true;

            LogMessage?.Invoke($"⚙️ {token.Name} activates {interactive.Name}!");
            LogMessage?.Invoke($"✨ {interactive.ActivationEffect}");

            // Handle chest looting
            if (interactive.ObjectType == InteractiveType.Chest && !interactive.HasBeenLooted)
            {
                interactive.HasBeenLooted = true;

                if (interactive.GoldPieces > 0)
                {
                    LogMessage?.Invoke($"💰 The chest contains {interactive.GoldPieces} gold pieces!");
                }

                if (!string.IsNullOrWhiteSpace(interactive.ContainedItems))
                {
                    LogMessage?.Invoke($"🎒 Items found: {interactive.ContainedItems}");
                }
            }

            ObjectActivated?.Invoke(interactive);
        }

        #endregion

        #region Hazard Handling

        public void TriggerHazard(Token token, HazardMetadata hazard)
        {
            if (!hazard.IsEnabled)
                return;

            LogMessage?.Invoke($"☠️ {token.Name} enters {hazard.Description}");

            // Saving throw if allowed
            bool savedSuccessfully = false;
            if (hazard.AllowsSave)
            {
                var result = RequestManualRoll?.Invoke(token, hazard, $"{hazard.SaveAbility} Save", hazard.SaveDC);
                if (result.HasValue && result.Value.proceed)
                {
                    int roll = result.Value.roll;
                    int mod = GetSaveModifier(token, hazard.SaveAbility);
                    int total = roll + mod;

                    savedSuccessfully = total >= hazard.SaveDC;

                    LogMessage?.Invoke($"🎲 {token.Name} rolls {hazard.SaveAbility} save: {roll}+{mod}={total} vs DC {hazard.SaveDC}");
                }
            }

            // Calculate damage
            var damageRoll = DiceRoller.RollExpression(hazard.DamageDice);
            int damage = damageRoll.Total;

            if (savedSuccessfully && hazard.SaveNegatesDamage)
            {
                LogMessage?.Invoke($"✅ {token.Name} saved! No damage taken.");
                return;
            }
            else if (savedSuccessfully && hazard.SaveHalvesDamage)
            {
                damage = damage / 2;
                LogMessage?.Invoke($"✅ {token.Name} saved! Damage halved to {damage}.");
            }
            else if (!savedSuccessfully)
            {
                LogMessage?.Invoke($"❌ {token.Name} failed the save!");
            }

            // Apply damage
            var (effectiveDamage, desc) = token.TakeDamage(damage, hazard.DamageType);
            LogMessage?.Invoke($"{hazard.DamageType.GetIcon()} {token.Name} takes {effectiveDamage} {hazard.DamageType.GetDisplayName()} damage!");

            hazard.IsTriggered = true;
            HazardTriggered?.Invoke(token, hazard);
        }

        #endregion

        #region Teleporter Handling

        public void TriggerTeleporter(Token token, TeleporterMetadata teleporter)
        {
            if (!teleporter.IsActive || !teleporter.IsEnabled)
            {
                LogMessage?.Invoke($"🌀 The portal is inactive.");
                return;
            }

            if (teleporter.RequiresConsent)
            {
                // Request confirmation from DM/player
                var consent = System.Windows.MessageBox.Show(
                    $"{token.Name} stands on a teleporter.\n\nDestination: ({teleporter.DestinationX}, {teleporter.DestinationY})\n\nTeleport now?",
                    "🌀 Teleporter",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (consent != System.Windows.MessageBoxResult.Yes)
                    return;
            }

            // Teleport!
            token.GridX = teleporter.DestinationX;
            token.GridY = teleporter.DestinationY;

            LogMessage?.Invoke($"🌀 {teleporter.TeleportDescription}");
            LogMessage?.Invoke($"📍 {token.Name} teleported to ({teleporter.DestinationX}, {teleporter.DestinationY})");

            teleporter.IsTriggered = true;
            TokenTeleported?.Invoke(token, 0);
        }

        #endregion

        #region Healing Zone Handling

        /// <summary>
        /// Trigger a healing zone and apply effects with condition removal
        /// </summary>
        public void TriggerHealingZone(Token token, HealingZoneMetadata healingZone)
        {
            if (!healingZone.CanHeal(token.Id))
            {
                if (healingZone.OncePerCreature && healingZone.HealedTokens.Contains(token.Id))
                {
                    LogMessage?.Invoke($"💚 {token.Name} has already been healed by this source.");
                }
                else if (healingZone.HasCharges && healingZone.ChargesRemaining <= 0)
                {
                    LogMessage?.Invoke($"💚 The healing source has been depleted.");
                }
                return;
            }

            LogMessage?.Invoke($"💚 {token.Name} is bathed in healing light!");

            // Roll healing
            var healRoll = Utils.DiceRoller.RollExpression(healingZone.HealingDice);
            int healing = healRoll.Total;

            int oldHP = token.HP;
            token.HP = Math.Min(token.HP + healing, token.MaxHP);
            int actualHealing = token.HP - oldHP;

            LogMessage?.Invoke($"💚 {token.Name} heals {actualHealing} HP! ({healRoll.Expression} = {healRoll.Total})");

            // Mark as healed
            if (healingZone.OncePerCreature)
            {
                healingZone.HealedTokens.Add(token.Id);
            }

            // Use charge
            if (healingZone.HasCharges)
            {
                healingZone.ChargesRemaining--;
                if (healingZone.ChargesRemaining > 0)
                {
                    LogMessage?.Invoke($"💚 {healingZone.ChargesRemaining} charges remaining.");
                }
                else
                {
                    LogMessage?.Invoke($"💚 Healing source depleted!");
                }
            }

            // NEW: Remove conditions if applicable
            if (healingZone.RemovesConditions && !string.IsNullOrWhiteSpace(healingZone.ConditionsRemoved))
            {
                var conditionsBefore = token.Conditions;
                token.RemoveConditionsByName(healingZone.ConditionsRemoved);

                if (conditionsBefore != token.Conditions)
                {
                    LogMessage?.Invoke($"✨ Conditions removed: {healingZone.ConditionsRemoved}");
                }
            }

            healingZone.IsTriggered = true;
            TokenHealed?.Invoke(token, actualHealing);
        }

        #endregion

        #region Helper Methods

        private int GetSkillModifier(Token token, string skill)
        {
            return skill.ToUpper() switch
            {
                "STRENGTH" or "STR" => token.StrMod,
                "DEXTERITY" or "DEX" => token.DexMod,
                "CONSTITUTION" or "CON" => token.ConMod,
                "INTELLIGENCE" or "INT" => token.IntMod,
                "WISDOM" or "WIS" => token.WisMod,
                "CHARISMA" or "CHA" => token.ChaMod,
                "LOCKPICKING" or "THIEVES' TOOLS" => token.DexMod,
                "ATHLETICS" => token.StrMod,
                "ACROBATICS" => token.DexMod,
                "INVESTIGATION" => token.IntMod,
                "PERCEPTION" => token.WisMod,
                _ => 0
            };
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

        #endregion
    }
}