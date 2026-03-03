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

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Metadata for secret doors, hidden passages, and concealed items
    /// </summary>
    public class SecretMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Secret;

        #region Discovery

        /// <summary>
        /// DC for Investigation check to discover the secret
        /// </summary>
        public int InvestigationDC { get; set; } = 15;

        /// <summary>
        /// Whether the secret has been discovered
        /// </summary>
        public bool IsDiscovered { get; set; } = false;

        /// <summary>
        /// Description shown when discovered
        /// </summary>
        public string DiscoveryDescription { get; set; } = "You discover a hidden passage!";

        #endregion

        #region Secret Type

        /// <summary>
        /// What kind of secret this is
        /// </summary>
        public SecretType SecretKind { get; set; } = SecretType.HiddenDoor;

        /// <summary>
        /// For hidden doors - direction they open/lead
        /// </summary>
        public string Direction { get; set; } = "North";

        /// <summary>
        /// For hidden treasure - description of what's found
        /// </summary>
        public string TreasureDescription { get; set; }

        #endregion

        #region Interaction

        /// <summary>
        /// Whether this secret requires an action to activate (e.g., push a wall)
        /// </summary>
        public bool RequiresActivation { get; set; } = false;

        /// <summary>
        /// Description of how to activate
        /// </summary>
        public string ActivationDescription { get; set; } = "You push on the wall and it swings open.";

        #endregion
    }

    public enum SecretType
    {
        HiddenDoor,
        SecretPassage,
        HiddenTreasure,
        Compartment,
        Illusion,
        FalseDoor
    }
}