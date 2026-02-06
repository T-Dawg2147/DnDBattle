using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.Models.Environment;
namespace DnDBattle.Models.Environment
{
    /// <summary>
    /// Available weather effect types for the dynamic weather system.
    /// </summary>
    public enum WeatherType
    {
        None = 0,
        Rain,
        Snow,
        Fog,
        Storm,
        Sandstorm
    }
}
