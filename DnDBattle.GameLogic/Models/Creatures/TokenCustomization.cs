using System;
using System.Windows.Media;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Models.Creatures
{
    /// <summary>
    /// Customization settings for a token's visual appearance.
    /// </summary>
    public class TokenCustomization
    {
        /// <summary>Token ID this customization applies to.</summary>
        public string TokenId { get; set; } = string.Empty;

        // ── Border ──
        /// <summary>Border color (hex string for serialization, e.g. "#FF0000").</summary>
        public string BorderColorHex { get; set; } = "#FFFFFF";

        /// <summary>Border thickness in pixels.</summary>
        public double BorderThickness { get; set; } = 2.0;

        /// <summary>Border style.</summary>
        public TokenBorderStyle BorderStyle { get; set; } = TokenBorderStyle.Solid;

        /// <summary>Whether to show a glow effect around the token border.</summary>
        public bool ShowGlow { get; set; } = false;

        /// <summary>Glow color hex (defaults to same as border).</summary>
        public string GlowColorHex { get; set; } = "#FFFFFF";

        /// <summary>Glow radius in pixels.</summary>
        public double GlowRadius { get; set; } = 8.0;

        // ── Shape ──
        /// <summary>Token shape for clipping/rendering.</summary>
        public TokenShape Shape { get; set; } = TokenShape.Circle;

        // ── Name Plate ──
        /// <summary>Whether the name plate is always visible (false = hover only).</summary>
        public bool NamePlateAlwaysVisible { get; set; } = false;

        /// <summary>Position of the name plate relative to the token.</summary>
        public NamePlatePosition NamePlatePosition { get; set; } = NamePlatePosition.Below;

        /// <summary>Name plate background color hex.</summary>
        public string NamePlateBackgroundHex { get; set; } = "#CC000000";

        /// <summary>Name plate text color hex.</summary>
        public string NamePlateTextColorHex { get; set; } = "#FFFFFF";

        /// <summary>Name plate font size.</summary>
        public double NamePlateFontSize { get; set; } = 10.0;

        // ── Status Overlay ──
        /// <summary>Whether to show condition icons as overlay badges.</summary>
        public bool ShowConditionOverlay { get; set; } = true;

        /// <summary>Maximum number of condition icons to show before collapsing.</summary>
        public int MaxConditionIcons { get; set; } = 4;

        /// <summary>
        /// Creates a default customization with sensible values.
        /// </summary>
        public static TokenCustomization CreateDefault(string tokenId)
        {
            return new TokenCustomization { TokenId = tokenId };
        }
    }

    /// <summary>Token border rendering styles.</summary>
    public enum TokenBorderStyle
    {
        None,
        Solid,
        Dashed,
        Double,
        Glow
    }

    /// <summary>Token clip/render shapes.</summary>
    public enum TokenShape
    {
        Circle,
        Square,
        RoundedSquare,
        Hexagon,
        Diamond,
        Star
    }

    /// <summary>Position of the name plate relative to the token.</summary>
    public enum NamePlatePosition
    {
        Above,
        Below,
        Inside,
        Left,
        Right
    }
}
