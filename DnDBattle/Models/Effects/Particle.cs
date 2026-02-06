using System.Windows;
using System.Windows.Media;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.Models.Effects;

namespace DnDBattle.Models.Effects
{
    /// <summary>
    /// Represents a single particle in an effect animation
    /// </summary>
    public class Particle
    {
        public Point Position { get; set; }
        public Vector Velocity { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }
        public double Lifetime { get; set; }
        public double Opacity { get; set; }
    }
}
