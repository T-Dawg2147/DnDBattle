using DnDBattle.Models.Tiles;
using System;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Metadata for healing zones (shrines, fountains, etc.)
    /// </summary>
    public class HealingZoneMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Healing;

        #region Healing

        /// <summary>
        /// Healing dice expression
        /// </summary>
        public string HealingDice { get; set; } = "2d8+2";

        /// <summary>
        /// Description of the healing source
        /// </summary>
        public string Description { get; set; } = "A mystical fountain glows with healing energy.";

        /// <summary>
        /// When healing is applied
        /// </summary>
        public HealingTrigger HealingTrigger { get; set; } = HealingTrigger.OnActivation;

        #endregion

        #region Usage

        /// <summary>
        /// Whether zone can only heal once per creature
        /// </summary>
        public bool OncePerCreature { get; set; } = true;

        /// <summary>
        /// IDs of tokens that have already been healed
        /// </summary>
        public System.Collections.Generic.List<Guid> HealedTokens { get; set; } = new System.Collections.Generic.List<Guid>();

        /// <summary>
        /// Whether zone has limited charges
        /// </summary>
        public bool HasCharges { get; set; } = false;

        /// <summary>
        /// Number of charges remaining
        /// </summary>
        public int ChargesRemaining { get; set; } = 3;

        #endregion

        #region Additional Effects

        /// <summary>
        /// Whether healing also removes conditions
        /// </summary>
        public bool RemovesConditions { get; set; } = false;

        /// <summary>
        /// Conditions removed (poisoned, diseased, etc.)
        /// </summary>
        public string ConditionsRemoved { get; set; } = "Poisoned";

        #endregion

        /// <summary>
        /// Check if this zone can still heal
        /// </summary>
        public bool CanHeal(Guid tokenId)
        {
            if (!IsEnabled)
                return false;

            if (OncePerCreature && HealedTokens.Contains(tokenId))
                return false;

            if (HasCharges && ChargesRemaining <= 0)
                return false;

            return true;
        }
    }

    public enum HealingTrigger
    {
        OnEnter,         // Heals when entering the tile
        OnActivation,    // Requires explicit interaction
        StartOfTurn,     // Heals at start of turn if in tile
        EndOfTurn        // Heals at end of turn if in tile
    }
}