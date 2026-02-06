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
    /// Describes a link from one map tile (e.g. door/stairs) to a position on another map.
    /// </summary>
    public class MapLink
    {
        public string TargetMapId { get; set; }
        public int TargetX { get; set; }
        public int TargetY { get; set; }
    }
}
