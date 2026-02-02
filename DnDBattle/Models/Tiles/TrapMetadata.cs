using System;
using System.Collections.Generic;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Comprehensive trap metadata with D&D 5e mechanics
    /// </summary>
    public class TrapMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Trap;

        #region Roll Settings

        /// <summary>
        /// Whether the app should auto-roll for detection, or DM inputs player rolls
        /// </summary>
        public bool AutoRollDetection { get; set; } = true;

        /// <summary>
        /// Whether the app should auto-roll for disarm, or DM inputs player rolls
        /// </summary>
        public bool AutoRollDisarm { get; set; } = true;

        /// <summary>
        /// Whether the app should auto-roll saving throws, or DM inputs player rolls
        /// </summary>
        public bool AutoRollSave { get; set; } = true;

        /// <summary>
        /// Whether the app should auto-roll damage, or DM inputs manually
        /// </summary>
        public bool AutoRollDamage { get; set; } = true;

        #endregion

        #region Detection

        /// <summary>
        /// DC for Perception check to detect the trap
        /// </summary>
        public int DetectionDC { get; set; } = 15;

        /// <summary>
        /// Whether the trap has been detected by the party
        /// </summary>
        public bool IsDetected { get; set; } = false;

        /// <summary>
        /// Description of what players see when they detect it
        /// </summary>
        public string DetectionDescription { get; set; } = "You notice something unusual about this area.";

        #endregion

        #region Disarm

        /// <summary>
        /// Whether this trap can be disarmed
        /// </summary>
        public bool CanBeDisarmed { get; set; } = true;

        /// <summary>
        /// Skill required to disarm (Thieves' Tools, Sleight of Hand, etc.)
        /// </summary>
        public string DisarmSkill { get; set; } = "Thieves' Tools";

        /// <summary>
        /// DC for disarming the trap
        /// </summary>
        public int DisarmDC { get; set; } = 15;

        /// <summary>
        /// Whether the trap has been disarmed
        /// </summary>
        public bool IsDisarmed { get; set; } = false;

        /// <summary>
        /// What happens if disarm attempt fails
        /// </summary>
        public bool FailedDisarmTriggersTrap { get; set; } = true;

        #endregion

        #region Trigger

        /// <summary>
        /// How the trap is triggered
        /// </summary>
        public TrapTriggerType TriggerType { get; set; } = TrapTriggerType.Pressure;

        /// <summary>
        /// Save type to avoid the trap (DEX, WIS, etc.)
        /// </summary>
        public string SaveAbility { get; set; } = "DEX";

        /// <summary>
        /// DC for the saving throw
        /// </summary>
        public int SaveDC { get; set; } = 13;

        /// <summary>
        /// Area of effect in grid squares (0 = single target)
        /// </summary>
        public int AreaOfEffect { get; set; } = 0;

        #endregion

        #region Damage

        /// <summary>
        /// Damage dice expression (e.g., "4d6", "2d10+5")
        /// </summary>
        public string DamageDice { get; set; } = "2d6";

        /// <summary>
        /// Type of damage dealt
        /// </summary>
        public DamageType DamageType { get; set; } = DamageType.Piercing;

        /// <summary>
        /// Whether damage is halved on successful save
        /// </summary>
        public bool HalfDamageOnSave { get; set; } = true;

        /// <summary>
        /// Additional effects (poisoned, restrained, etc.)
        /// </summary>
        public List<string> AdditionalEffects { get; set; } = new List<string>();

        #endregion

        #region Reusability

        /// <summary>
        /// Whether the trap can trigger multiple times
        /// </summary>
        public bool IsReusable { get; set; } = false;

        /// <summary>
        /// Reset time in rounds (for reusable traps)
        /// </summary>
        public int ResetTimeRounds { get; set; } = 0;

        /// <summary>
        /// Number of times this trap can be triggered (0 = unlimited if reusable)
        /// </summary>
        public int MaxTriggers { get; set; } = 1;

        /// <summary>
        /// Number of times already triggered
        /// </summary>
        public int TimesTriggered { get; set; } = 0;

        #endregion

        #region Flavor

        /// <summary>
        /// Description when trap triggers
        /// </summary>
        public string TriggerDescription { get; set; } = "The trap is sprung!";

        /// <summary>
        /// Sound effect or visual for the trap
        /// </summary>
        public string EffectDescription { get; set; } = "Darts shoot from the walls!";

        #endregion

        /// <summary>
        /// Check if trap can still be triggered
        /// </summary>
        public bool CanTrigger()
        {
            if (IsDisarmed || !IsEnabled)
                return false;

            if (!IsReusable && IsTriggered)
                return false;

            if (MaxTriggers > 0 && TimesTriggered >= MaxTriggers)
                return false;

            return true;
        }
    }

    /// <summary>
    /// How a trap is activated
    /// </summary>
    public enum TrapTriggerType
    {
        Pressure,      // Step on it
        Tripwire,      // Walk through it
        Proximity,     // Get near it
        Manual,        // DM-triggered
        Timed,         // After X rounds
        Interactive    // Player interacts with object
    }
}