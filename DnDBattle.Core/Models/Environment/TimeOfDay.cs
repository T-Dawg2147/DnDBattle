using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
namespace DnDBattle.Models.Environment
{
    /// <summary>
    /// Time of day for the day/night cycle system.
    /// </summary>
    public enum TimeOfDay
    {
        Dawn = 0,
        Day,
        Dusk,
        Night
    }
}
