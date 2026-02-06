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
    /// A background image layer that sits beneath the tile grid.
    /// Supports opacity, visibility, and grid-aligned positioning.
    /// </summary>
    public class BackgroundLayer
    {
        /// <summary>Path (or embedded resource URI) to the image file.</summary>
        public string ImagePath { get; set; }

        /// <summary>Opacity of this layer (0.0 – 1.0).</summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>Whether the layer is currently visible.</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>Draw order (lower = further back).</summary>
        public int ZOrder { get; set; }

        // ── Grid alignment ──

        /// <summary>Top-left grid coordinate the image maps to.</summary>
        public double TopLeftX { get; set; }

        /// <summary>Top-left grid coordinate the image maps to.</summary>
        public double TopLeftY { get; set; }

        /// <summary>Bottom-right grid coordinate the image maps to.</summary>
        public double BottomRightX { get; set; } = 50;

        /// <summary>Bottom-right grid coordinate the image maps to.</summary>
        public double BottomRightY { get; set; } = 50;
    }
}
