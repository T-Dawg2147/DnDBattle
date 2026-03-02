using DnDBattle.Models;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
namespace DnDBattle.Models.Combat
{
    /// <summary>
    /// D&amp;D 5e cover levels that modify AC and DEX saves.
    /// </summary>
    public enum CoverLevel
    {
        /// <summary>No cover – no bonus.</summary>
        None,
        /// <summary>Half cover – +2 AC, +2 DEX saves.</summary>
        Half,
        /// <summary>Three-quarters cover – +5 AC, +5 DEX saves.</summary>
        ThreeQuarters,
        /// <summary>Full cover – cannot be targeted directly.</summary>
        Full
    }
}
