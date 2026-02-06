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
    /// Determines how d20 rolls are made for attacks and saves.
    /// </summary>
    public enum AttackMode
    {
        Normal,
        Advantage,
        Disadvantage
    }
}
