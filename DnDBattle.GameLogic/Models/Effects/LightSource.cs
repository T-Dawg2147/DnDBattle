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
    /// Types of light sources available on the battle grid.
    /// </summary>
    public enum LightType
    {
        Point,
        Directional,
        Ambient
    }

    /// <summary>
    /// Represents a light source placed on the battle grid.
    /// Supports point lights (torches, lanterns) and directional lights (spotlights, windows).
    /// </summary>
    public class LightSource
    {
        public Point CenterGrid { get; set; }

        /// <summary>Bright light radius in grid squares.</summary>
        public double BrightRadius { get; set; } = 4;

        /// <summary>Dim light radius in grid squares (extends beyond bright).</summary>
        public double DimRadius { get; set; } = 8;

        /// <summary>Overall radius used for rendering (max of bright and dim). Read-only.</summary>
        public double RadiusSquares => Math.Max(BrightRadius, DimRadius);

        /// <summary>Light intensity from 0.0 to 1.0.</summary>
        public double Intensity { get; set; } = 1.0;

        /// <summary>Color of the light.</summary>
        public Color LightColor { get; set; } = Color.FromRgb(255, 255, 200);

        /// <summary>Whether this light source is currently active.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Type of light (Point, Directional, Ambient).</summary>
        public LightType Type { get; set; } = LightType.Point;

        /// <summary>Direction angle in degrees (used for Directional lights).</summary>
        public double Direction { get; set; } = 0;

        /// <summary>Cone width in degrees (used for Directional lights).</summary>
        public double ConeWidth { get; set; } = 60;

        /// <summary>Optional label for identification.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// For directional lights, checks whether a grid point falls within the cone.
        /// </summary>
        public bool IsPointInCone(Point point)
        {
            if (Type != LightType.Directional) return true;

            double dx = point.X - CenterGrid.X;
            double dy = point.Y - CenterGrid.Y;
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;

            double diff = NormalizeAngleDiff(angle - Direction);
            return Math.Abs(diff) <= ConeWidth / 2.0;
        }

        private static double NormalizeAngleDiff(double diff)
        {
            while (diff > 180) diff -= 360;
            while (diff < -180) diff += 360;
            return diff;
        }
    }
}
