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
    /// Represents a visual aura around a token (e.g. Paladin Aura, Spirit Guardians).
    /// </summary>
    public class TokenAura
    {
        public string Name { get; set; } = "Aura";
        public int RadiusSquares { get; set; } = 2;
        public Color Color { get; set; } = Colors.Gold;
        public double Opacity { get; set; } = 0.3;
        public bool IsVisible { get; set; } = true;

        /// <summary>Pre-built aura templates.</summary>
        public static TokenAura PaladinAura() => new() { Name = "Aura of Protection", RadiusSquares = 2, Color = Colors.Gold, Opacity = 0.3 };
        public static TokenAura SpiritGuardians() => new() { Name = "Spirit Guardians", RadiusSquares = 3, Color = Color.FromRgb(100, 180, 255), Opacity = 0.25 };
        public static TokenAura Rage() => new() { Name = "Rage", RadiusSquares = 1, Color = Colors.Red, Opacity = 0.35 };
        public static TokenAura Bless() => new() { Name = "Bless", RadiusSquares = 6, Color = Colors.LimeGreen, Opacity = 0.2 };
    }
}
