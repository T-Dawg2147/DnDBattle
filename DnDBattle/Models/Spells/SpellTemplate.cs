using System.Windows.Media;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Tiles;
using DnDBattle.Models.Spells;

namespace DnDBattle.Models.Spells
{
    /// <summary>
    /// Represents a D&D spell template for quick area effect placement
    /// </summary>
    public class SpellTemplate
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string School { get; set; } = string.Empty;
        public AreaEffectShape Shape { get; set; }
        public int Size { get; set; } // In feet
        public int Width { get; set; } = 5; // For lines/walls
        public DamageType DamageType { get; set; }
        public Color Color { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; } // In rounds (0 = instantaneous)
        public bool RequiresConcentration { get; set; }
        public string DamageExpression { get; set; } = string.Empty; // e.g. "8d6"
        public DamageTiming DamageTiming { get; set; } = DamageTiming.OnEnter;
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Level display string (Cantrip, 1st, 2nd, etc.)
        /// </summary>
        public string LevelDisplay => Level switch
        {
            0 => "Cantrip",
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{Level}th"
        };

        /// <summary>
        /// Creates an AreaEffect from this spell template
        /// </summary>
        public AreaEffect ToAreaEffect()
        {
            return new AreaEffect
            {
                Name = Name,
                Shape = Shape,
                SizeInFeet = Size,
                WidthInFeet = Width,
                Color = Color,
                DurationRounds = Duration,
                RoundsRemaining = Duration,
                DamageExpression = DamageExpression,
                DamageType = DamageType,
                DamageTiming = DamageTiming,
                RequiresConcentration = RequiresConcentration
            };
        }
    }

    /// <summary>
    /// When damage from an area effect is applied
    /// </summary>
    public enum DamageTiming
    {
        OnEnter,
        StartOfTurn,
        EndOfTurn
    }

    /// <summary>
    /// Animation style for area effects
    /// </summary>
    public enum EffectAnimationType
    {
        None,
        Pulse,
        Particle,
        Rotate
    }
}
