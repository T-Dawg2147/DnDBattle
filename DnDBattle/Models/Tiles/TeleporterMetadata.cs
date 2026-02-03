using DnDBattle.Models.Tiles;
using System;
using System.Windows;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Metadata for teleportation tiles
    /// </summary>
    public class TeleporterMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Teleporter;

        #region Destination

        /// <summary>
        /// Target X coordinate
        /// </summary>
        public int DestinationX { get; set; }

        /// <summary>
        /// Target Y coordinate
        /// </summary>
        public int DestinationY { get; set; }

        /// <summary>
        /// Description shown when teleporting
        /// </summary>
        public string TeleportDescription { get; set; } = "You are transported in a flash of light!";

        #endregion

        #region Activation

        /// <summary>
        /// Whether teleporter is visible or hidden
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Whether teleporter is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether teleport requires player consent
        /// </summary>
        public bool RequiresConsent { get; set; } = true;

        /// <summary>
        /// Whether teleport is one-way
        /// </summary>
        public bool IsOneWay { get; set; } = false;

        #endregion
    }
}