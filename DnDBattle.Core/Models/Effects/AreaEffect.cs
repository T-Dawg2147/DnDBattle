using System;
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

namespace DnDBattle.Models.Effects
{
    /// <summary>
    /// Represents an area of effect template on the battle grid
    /// </summary>
    public class AreaEffect
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public AreaEffectShape Shape { get; set; }

        /// <summary>
        /// Size in feet (radius for sphere/circle, length for line/cone, side for cube)
        /// </summary>
        public int SizeInFeet { get; set; }

        /// <summary>
        /// Width in feet (for lines)
        /// </summary>
        public int WidthInFeet { get; set; } = 5;

        /// <summary>
        /// Origin point in grid coordinates
        /// </summary>
        public Point Origin { get; set; }

        /// <summary>
        /// Direction angle in degrees (0 = right, 90 = down, 180 = left, 270 = up)
        /// </summary>
        public double DirectionAngle { get; set; }

        /// <summary>
        /// The color of the effect
        /// </summary>
        public Color Color { get; set; } = Color.FromArgb(100, 255, 100, 0);

        /// <summary>
        /// Whether this effect is currently being previewed (not yet placed)
        /// </summary>
        public bool IsPreview { get; set; }

        /// <summary>
        /// Optional: The token that created this effect
        /// </summary>
        public Guid? SourceTokenId { get; set; }

        // ── Phase 6.2: Duration Tracking ──

        /// <summary>
        /// Duration in rounds (0 = permanent/instantaneous)
        /// </summary>
        public int DurationRounds { get; set; }

        /// <summary>
        /// Remaining rounds before effect expires
        /// </summary>
        public int RoundsRemaining { get; set; }

        /// <summary>
        /// Whether this spell requires concentration
        /// </summary>
        public bool RequiresConcentration { get; set; }

        // ── Phase 6.3: Damage Over Time ──

        /// <summary>
        /// Dice expression for recurring damage (e.g. "3d8")
        /// </summary>
        public string DamageExpression { get; set; } = string.Empty;

        /// <summary>
        /// Type of damage dealt
        /// </summary>
        public DamageType DamageType { get; set; }

        /// <summary>
        /// When the damage is applied
        /// </summary>
        public DamageTiming DamageTiming { get; set; }

        // ── Phase 6.5: Effect Animations ──

        /// <summary>
        /// Animation style for this effect
        /// </summary>
        public EffectAnimationType AnimationType { get; set; }

        /// <summary>
        /// Current animation phase (radians for pulse, degrees for rotation)
        /// </summary>
        public double AnimationPhase { get; set; }

        /// <summary>
        /// Gets the size in grid squares (assuming 5ft per square)
        /// </summary>
        public double SizeInSquares => SizeInFeet / 5.0;

        /// <summary>
        /// Gets the width in grid squares
        /// </summary>
        public double WidthInSquares => WidthInFeet / 5.0;
    }

    public enum AreaEffectShape
    {
        Sphere,      // Circle on the grid (radius)
        Cube,        // Square on the grid (side length)
        Cone,        // Triangle emanating from a point
        Line,        // Rectangle from point in direction
        Cylinder,    // Same as sphere for 2D representation
        Square       // Alias for cube (centered)
    }

    /// <summary>
    /// Preset area effect templates for common spells
    /// </summary>
    public static class AreaEffectPresets
    {
        // Damage Spells - Red/Orange
        public static AreaEffect Fireball => new AreaEffect
        {
            Name = "Fireball",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 20,
            Color = Color.FromArgb(120, 255, 69, 0)
        };

        public static AreaEffect BurningHands => new AreaEffect
        {
            Name = "Burning Hands",
            Shape = AreaEffectShape.Cone,
            SizeInFeet = 15,
            Color = Color.FromArgb(120, 255, 140, 0)
        };

        public static AreaEffect LightningBolt => new AreaEffect
        {
            Name = "Lightning Bolt",
            Shape = AreaEffectShape.Line,
            SizeInFeet = 100,
            WidthInFeet = 5,
            Color = Color.FromArgb(120, 135, 206, 250)
        };

        public static AreaEffect ConeOfCold => new AreaEffect
        {
            Name = "Cone of Cold",
            Shape = AreaEffectShape.Cone,
            SizeInFeet = 60,
            Color = Color.FromArgb(120, 173, 216, 230)
        };

        public static AreaEffect Thunderwave => new AreaEffect
        {
            Name = "Thunderwave",
            Shape = AreaEffectShape.Cube,
            SizeInFeet = 15,
            Color = Color.FromArgb(120, 100, 149, 237)
        };

        public static AreaEffect Shatter => new AreaEffect
        {
            Name = "Shatter",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 10,
            Color = Color.FromArgb(120, 186, 85, 211)
        };

        // Healing/Buff - Green/Gold
        public static AreaEffect SpiritGuardians => new AreaEffect
        {
            Name = "Spirit Guardians",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 15,
            Color = Color.FromArgb(100, 255, 215, 0)
        };

        public static AreaEffect Bless => new AreaEffect
        {
            Name = "Bless",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 30,
            Color = Color.FromArgb(80, 255, 223, 0)
        };

        // Control - Purple/Blue
        public static AreaEffect Web => new AreaEffect
        {
            Name = "Web",
            Shape = AreaEffectShape.Cube,
            SizeInFeet = 20,
            Color = Color.FromArgb(100, 220, 220, 220)
        };

        public static AreaEffect Darkness => new AreaEffect
        {
            Name = "Darkness",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 15,
            Color = Color.FromArgb(150, 20, 20, 40)
        };

        public static AreaEffect FogCloud => new AreaEffect
        {
            Name = "Fog Cloud",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 20,
            Color = Color.FromArgb(100, 200, 200, 200)
        };

        public static AreaEffect Silence => new AreaEffect
        {
            Name = "Silence",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 20,
            Color = Color.FromArgb(80, 75, 0, 130)
        };

        public static AreaEffect Entangle => new AreaEffect
        {
            Name = "Entangle",
            Shape = AreaEffectShape.Square,
            SizeInFeet = 20,
            Color = Color.FromArgb(100, 34, 139, 34)
        };

        public static AreaEffect CloudOfDaggers => new AreaEffect
        {
            Name = "Cloud of Daggers",
            Shape = AreaEffectShape.Cube,
            SizeInFeet = 5,
            Color = Color.FromArgb(120, 192, 192, 192)
        };

        public static AreaEffect MoonBeam => new AreaEffect
        {
            Name = "Moonbeam",
            Shape = AreaEffectShape.Cylinder,
            SizeInFeet = 5,
            Color = Color.FromArgb(100, 230, 230, 250)
        };

        // Breath Weapons
        public static AreaEffect DragonBreathCone => new AreaEffect
        {
            Name = "Dragon Breath (Cone)",
            Shape = AreaEffectShape.Cone,
            SizeInFeet = 30,
            Color = Color.FromArgb(120, 255, 69, 0)
        };

        public static AreaEffect DragonBreathLine => new AreaEffect
        {
            Name = "Dragon Breath (Line)",
            Shape = AreaEffectShape.Line,
            SizeInFeet = 60,
            WidthInFeet = 5,
            Color = Color.FromArgb(120, 255, 69, 0)
        };

        /// <summary>
        /// Gets all preset area effects
        /// </summary>
        public static AreaEffect[] GetAllPresets()
        {
            return new[]
            {
                Fireball, BurningHands, LightningBolt, ConeOfCold, Thunderwave, Shatter,
                SpiritGuardians, Bless,
                Web, Darkness, FogCloud, Silence, Entangle, CloudOfDaggers, MoonBeam,
                DragonBreathCone, DragonBreathLine
            };
        }
    }
}