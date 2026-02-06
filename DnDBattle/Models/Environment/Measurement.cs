using System;
using System.Collections.Generic;
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
    /// A saved measurement on the map (distance, area, etc.).
    /// </summary>
    public class Measurement
    {
        /// <summary>Unique identifier.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>User-defined label (e.g., "Range to gate").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Measurement type.</summary>
        public MeasurementType Type { get; set; } = MeasurementType.Distance;

        /// <summary>Grid coordinates forming the measurement path/shape.</summary>
        public List<(int X, int Y)> Points { get; set; } = new();

        /// <summary>Display color hex (e.g., "#FF0000" for danger).</summary>
        public string ColorHex { get; set; } = "#4FC3F7";

        /// <summary>Whether this measurement is visible on the map.</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>Purpose category for color-coding.</summary>
        public MeasurementPurpose Purpose { get; set; } = MeasurementPurpose.Info;

        /// <summary>Line thickness in pixels.</summary>
        public double LineThickness { get; set; } = 2.0;

        /// <summary>Whether to show the distance/area label on the map.</summary>
        public bool ShowLabel { get; set; } = true;

        /// <summary>Feet per square for distance calculations.</summary>
        public int FeetPerSquare { get; set; } = 5;

        /// <summary>Created timestamp.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Type of measurement.
    /// </summary>
    public enum MeasurementType
    {
        /// <summary>Distance between two points.</summary>
        Distance,

        /// <summary>Multi-point path distance.</summary>
        Path,

        /// <summary>Radius from center point.</summary>
        Radius,

        /// <summary>Rectangular area.</summary>
        Area,

        /// <summary>Arbitrary polygon perimeter/area.</summary>
        Polygon
    }

    /// <summary>
    /// Purpose category for measurement color-coding.
    /// </summary>
    public enum MeasurementPurpose
    {
        Info,
        Danger,
        Safe,
        Spell,
        Movement,
        Custom
    }
}
