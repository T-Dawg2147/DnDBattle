using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Models
{
    /// <summary>
    /// Represents an area of effect template on the battle grid
    /// </summary>
    public class AreaEffect
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
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

        /// <summary>
        /// Gets the size in grid squares (assuming 5ft per square)
        /// </summary>
        public double SizeInSquares => SizeInFeet / 5.0;

        /// <summary>
        /// Gets the width in grid squares
        /// </summary>
        public double WidthInSquares => WidthInFeet / 5.0;

        #region Duration Tracking (Phase 6)

        /// <summary>
        /// Duration type for the effect
        /// </summary>
        public EffectDurationType DurationType { get; set; } = EffectDurationType.Instantaneous;

        /// <summary>
        /// Number of rounds remaining (for round-based duration)
        /// </summary>
        public int RoundsRemaining { get; set; } = 0;

        /// <summary>
        /// The round when this effect was created
        /// </summary>
        public int CreatedOnRound { get; set; } = 0;

        /// <summary>
        /// Whether this effect requires concentration
        /// </summary>
        public bool RequiresConcentration { get; set; } = false;

        /// <summary>
        /// The name of the concentrating token (if any)
        /// </summary>
        public string? ConcentratingTokenName { get; set; }

        /// <summary>
        /// Whether the effect is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Damage dealt when entering/starting turn in effect
        /// </summary>
        public string? DamageOnEnter { get; set; }

        /// <summary>
        /// Damage type for damage over time
        /// </summary>
        public DamageType DamageType { get; set; } = DamageType.None;

        /// <summary>
        /// Saving throw type for the effect
        /// </summary>
        public string? SavingThrowType { get; set; }

        /// <summary>
        /// Saving throw DC
        /// </summary>
        public int SavingThrowDC { get; set; } = 0;

        /// <summary>
        /// Description shown to players
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether to show animated pulsing border
        /// </summary>
        public bool ShowPulsingBorder { get; set; } = false;

        /// <summary>
        /// Returns true if the effect has expired
        /// </summary>
        public bool HasExpired => DurationType == EffectDurationType.Rounds && RoundsRemaining <= 0;

        /// <summary>
        /// Decrements the round counter
        /// </summary>
        public void DecrementRound()
        {
            if (DurationType == EffectDurationType.Rounds && RoundsRemaining > 0)
            {
                RoundsRemaining--;
            }
        }

        /// <summary>
        /// Gets display text for duration
        /// </summary>
        public string DurationDisplay
        {
            get
            {
                return DurationType switch
                {
                    EffectDurationType.Instantaneous => "Instant",
                    EffectDurationType.Rounds => $"{RoundsRemaining} round{(RoundsRemaining != 1 ? "s" : "")}",
                    EffectDurationType.Minutes => "1 minute",
                    EffectDurationType.Concentration => $"Conc. ({ConcentratingTokenName ?? "?"})",
                    EffectDurationType.Permanent => "Permanent",
                    _ => ""
                };
            }
        }

        #endregion

        /// <summary>
        /// Creates a deep copy of the effect
        /// </summary>
        public AreaEffect Clone()
        {
            return new AreaEffect
            {
                Id = Guid.NewGuid(),
                Name = Name,
                Shape = Shape,
                SizeInFeet = SizeInFeet,
                WidthInFeet = WidthInFeet,
                Origin = Origin,
                DirectionAngle = DirectionAngle,
                Color = Color,
                IsPreview = IsPreview,
                SourceTokenId = SourceTokenId,
                DurationType = DurationType,
                RoundsRemaining = RoundsRemaining,
                CreatedOnRound = CreatedOnRound,
                RequiresConcentration = RequiresConcentration,
                ConcentratingTokenName = ConcentratingTokenName,
                IsActive = IsActive,
                DamageOnEnter = DamageOnEnter,
                DamageType = DamageType,
                SavingThrowType = SavingThrowType,
                SavingThrowDC = SavingThrowDC,
                Description = Description,
                ShowPulsingBorder = ShowPulsingBorder
            };
        }
    }

    public enum AreaEffectShape
    {
        Sphere,      // Circle on the grid (radius)
        Cube,        // Square on the grid (side length)
        Cone,        // Triangle emanating from a point
        Line,        // Rectangle from point in direction
        Cylinder,    // Same as sphere for 2D representation
        Square,      // Alias for cube (centered)
        
        // Advanced shapes (Phase 6)
        Wall,        // Line with thickness (Wall of Fire, etc.)
        Hemisphere,  // Dome effect (shown as circle with pattern)
        Ring,        // Ring/donut shape
        Custom       // Custom polygon shape
    }

    /// <summary>
    /// Duration types for area effects
    /// </summary>
    public enum EffectDurationType
    {
        Instantaneous,  // One-time effect
        Rounds,         // Lasts a number of rounds
        Minutes,        // Lasts 1 minute (10 rounds)
        Concentration,  // Lasts while caster concentrates
        Permanent       // Lasts until dispelled
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
            Color = Color.FromArgb(100, 200, 200, 200),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            Description = "Heavily obscures the area"
        };

        public static AreaEffect Silence => new AreaEffect
        {
            Name = "Silence",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 20,
            Color = Color.FromArgb(80, 75, 0, 130),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            Description = "No sound can be created or pass through"
        };

        public static AreaEffect Entangle => new AreaEffect
        {
            Name = "Entangle",
            Shape = AreaEffectShape.Square,
            SizeInFeet = 20,
            Color = Color.FromArgb(100, 34, 139, 34),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            SavingThrowType = "STR",
            Description = "Difficult terrain, creatures may be restrained"
        };

        public static AreaEffect CloudOfDaggers => new AreaEffect
        {
            Name = "Cloud of Daggers",
            Shape = AreaEffectShape.Cube,
            SizeInFeet = 5,
            Color = Color.FromArgb(120, 192, 192, 192),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            DamageOnEnter = "4d4",
            DamageType = DamageType.Slashing,
            ShowPulsingBorder = true,
            Description = "4d4 slashing damage when entering or starting turn"
        };

        public static AreaEffect MoonBeam => new AreaEffect
        {
            Name = "Moonbeam",
            Shape = AreaEffectShape.Cylinder,
            SizeInFeet = 5,
            Color = Color.FromArgb(100, 230, 230, 250),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            DamageOnEnter = "2d10",
            DamageType = DamageType.Radiant,
            SavingThrowType = "CON",
            ShowPulsingBorder = true,
            Description = "2d10 radiant damage, CON save for half"
        };

        // Breath Weapons
        public static AreaEffect DragonBreathCone => new AreaEffect
        {
            Name = "Dragon Breath (Cone)",
            Shape = AreaEffectShape.Cone,
            SizeInFeet = 30,
            Color = Color.FromArgb(120, 255, 69, 0),
            DurationType = EffectDurationType.Instantaneous,
            SavingThrowType = "DEX"
        };

        public static AreaEffect DragonBreathLine => new AreaEffect
        {
            Name = "Dragon Breath (Line)",
            Shape = AreaEffectShape.Line,
            SizeInFeet = 60,
            WidthInFeet = 5,
            Color = Color.FromArgb(120, 255, 69, 0),
            DurationType = EffectDurationType.Instantaneous,
            SavingThrowType = "DEX"
        };

        // Phase 6: Advanced spells with duration
        public static AreaEffect WallOfFire => new AreaEffect
        {
            Name = "Wall of Fire",
            Shape = AreaEffectShape.Wall,
            SizeInFeet = 60, // Length
            WidthInFeet = 1, // 1 ft thick
            Color = Color.FromArgb(150, 255, 69, 0),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            DamageOnEnter = "5d8",
            DamageType = DamageType.Fire,
            SavingThrowType = "DEX",
            ShowPulsingBorder = true,
            Description = "5d8 fire damage when passing through or starting turn within 10ft"
        };

        public static AreaEffect SpikeGrowth => new AreaEffect
        {
            Name = "Spike Growth",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 20,
            Color = Color.FromArgb(100, 139, 90, 43),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            Description = "Difficult terrain, 2d4 piercing per 5ft moved"
        };

        public static AreaEffect Stinking​Cloud => new AreaEffect
        {
            Name = "Stinking Cloud",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 20,
            Color = Color.FromArgb(120, 154, 205, 50),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            SavingThrowType = "CON",
            Description = "Heavily obscured, CON save or spend action retching"
        };

        public static AreaEffect Hypnotic​Pattern => new AreaEffect
        {
            Name = "Hypnotic Pattern",
            Shape = AreaEffectShape.Cube,
            SizeInFeet = 30,
            Color = Color.FromArgb(100, 255, 20, 147),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            SavingThrowType = "WIS",
            ShowPulsingBorder = true,
            Description = "WIS save or charmed, incapacitated, speed 0"
        };

        public static AreaEffect Sleet​Storm => new AreaEffect
        {
            Name = "Sleet Storm",
            Shape = AreaEffectShape.Cylinder,
            SizeInFeet = 40,
            Color = Color.FromArgb(100, 176, 224, 230),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            SavingThrowType = "DEX",
            Description = "Difficult terrain, heavily obscured, DEX save or fall prone"
        };

        public static AreaEffect Antimagic​Field => new AreaEffect
        {
            Name = "Antimagic Field",
            Shape = AreaEffectShape.Sphere,
            SizeInFeet = 10,
            Color = Color.FromArgb(100, 128, 128, 128),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            ShowPulsingBorder = true,
            Description = "Magic is suppressed within the sphere"
        };

        public static AreaEffect Blade​Barrier => new AreaEffect
        {
            Name = "Blade Barrier",
            Shape = AreaEffectShape.Wall,
            SizeInFeet = 100,
            WidthInFeet = 5,
            Color = Color.FromArgb(150, 192, 192, 192),
            DurationType = EffectDurationType.Concentration,
            RequiresConcentration = true,
            DamageOnEnter = "6d10",
            DamageType = DamageType.Slashing,
            SavingThrowType = "DEX",
            ShowPulsingBorder = true,
            Description = "6d10 slashing damage, DEX save for half"
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
                DragonBreathCone, DragonBreathLine,
                WallOfFire, SpikeGrowth, Stinking​Cloud, Hypnotic​Pattern, Sleet​Storm,
                Antimagic​Field, Blade​Barrier
            };
        }

        /// <summary>
        /// Gets presets organized by category
        /// </summary>
        public static Dictionary<string, AreaEffect[]> GetPresetsByCategory()
        {
            return new Dictionary<string, AreaEffect[]>
            {
                ["Damage (Instantaneous)"] = new[] { Fireball, BurningHands, LightningBolt, ConeOfCold, Thunderwave, Shatter },
                ["Damage (Concentration)"] = new[] { CloudOfDaggers, MoonBeam, WallOfFire, Blade​Barrier },
                ["Control"] = new[] { Web, Entangle, SpikeGrowth, Hypnotic​Pattern, Sleet​Storm },
                ["Buff/Utility"] = new[] { SpiritGuardians, Bless, Antimagic​Field },
                ["Obscurement"] = new[] { Darkness, FogCloud, Silence, Stinking​Cloud },
                ["Breath Weapons"] = new[] { DragonBreathCone, DragonBreathLine }
            };
        }
    }
}