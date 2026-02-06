using System;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.Models.Creatures;

namespace DnDBattle.Models.Creatures
{
    /// <summary>
    /// Defines the type of vision a creature has.
    /// </summary>
    public enum VisionType
    {
        Normal,
        Darkvision,
        Blindsight,
        Truesight
    }

    /// <summary>
    /// Vision properties for a token on the battle grid.
    /// Mirrors D&amp;D 5e vision rules.
    /// </summary>
    public class TokenVision
    {
        /// <summary>Primary vision type.</summary>
        public VisionType Type { get; set; } = VisionType.Normal;

        /// <summary>Normal vision range in grid squares (default 60 ft = 12 squares).</summary>
        public int NormalRange { get; set; } = 12;

        /// <summary>Darkvision range in grid squares (0 if none).</summary>
        public int DarkvisionRange { get; set; } = 0;

        /// <summary>Blindsight range in grid squares (0 if none).</summary>
        public int BlindsightRange { get; set; } = 0;

        /// <summary>Truesight range in grid squares (0 if none).</summary>
        public int TruesightRange { get; set; } = 0;

        /// <summary>Whether the token has a directional vision cone.</summary>
        public bool HasVisionCone { get; set; } = false;

        /// <summary>Width of the vision cone in degrees (default 180).</summary>
        public double VisionConeAngle { get; set; } = 180;

        /// <summary>Direction the token is facing in degrees.</summary>
        public double FacingAngle { get; set; } = 0;

        /// <summary>
        /// Returns the maximum effective vision range across all vision types.
        /// </summary>
        public int MaxRange => Math.Max(NormalRange,
            Math.Max(DarkvisionRange,
                Math.Max(BlindsightRange, TruesightRange)));
    }
}
