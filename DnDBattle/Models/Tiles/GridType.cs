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
    /// Defines the type of grid used on the map.
    /// </summary>
    public enum GridType
    {
        /// <summary>Standard square grid (default D&amp;D).</summary>
        Square,

        /// <summary>Hexagonal grid with flat top orientation.</summary>
        HexFlatTop,

        /// <summary>Hexagonal grid with pointy top orientation.</summary>
        HexPointyTop
    }
}
