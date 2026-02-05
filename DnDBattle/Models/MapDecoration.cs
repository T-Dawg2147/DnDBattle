using System;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Models
{
    /// <summary>
    /// Represents a map decoration/annotation such as text labels, markers, or notes.
    /// Implements Phase 8 map decoration features.
    /// </summary>
    public class MapDecoration
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Type of decoration
        /// </summary>
        public MapDecorationType Type { get; set; }

        /// <summary>
        /// Position in grid coordinates
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// End position for lines/measurements
        /// </summary>
        public Point? EndPosition { get; set; }

        /// <summary>
        /// Text content for labels and notes
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// Color of the decoration
        /// </summary>
        public Color Color { get; set; } = Colors.White;

        /// <summary>
        /// Font size for text
        /// </summary>
        public double FontSize { get; set; } = 12;

        /// <summary>
        /// Whether this decoration is visible only to the DM
        /// </summary>
        public bool IsDMOnly { get; set; } = false;

        /// <summary>
        /// Whether to show the decoration
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Optional icon/emoji to display
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Size/radius for markers and areas
        /// </summary>
        public double Size { get; set; } = 1;

        /// <summary>
        /// Layer order (higher = on top)
        /// </summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>
        /// Creates a text label decoration
        /// </summary>
        public static MapDecoration CreateLabel(Point position, string text, Color? color = null)
        {
            return new MapDecoration
            {
                Type = MapDecorationType.Label,
                Position = position,
                Text = text,
                Color = color ?? Colors.White,
                FontSize = 14
            };
        }

        /// <summary>
        /// Creates a DM-only note
        /// </summary>
        public static MapDecoration CreateDMNote(Point position, string text)
        {
            return new MapDecoration
            {
                Type = MapDecorationType.Note,
                Position = position,
                Text = text,
                Color = Color.FromRgb(255, 200, 100),
                IsDMOnly = true,
                Icon = "📝"
            };
        }

        /// <summary>
        /// Creates a measurement line
        /// </summary>
        public static MapDecoration CreateMeasurementLine(Point start, Point end, Color? color = null)
        {
            return new MapDecoration
            {
                Type = MapDecorationType.MeasurementLine,
                Position = start,
                EndPosition = end,
                Color = color ?? Colors.Yellow
            };
        }

        /// <summary>
        /// Creates an area marker
        /// </summary>
        public static MapDecoration CreateAreaMarker(Point position, double radiusSquares, string name, Color color)
        {
            return new MapDecoration
            {
                Type = MapDecorationType.AreaMarker,
                Position = position,
                Size = radiusSquares,
                Text = name,
                Color = color
            };
        }

        /// <summary>
        /// Creates a point of interest marker
        /// </summary>
        public static MapDecoration CreatePOI(Point position, string name, string icon = "📍")
        {
            return new MapDecoration
            {
                Type = MapDecorationType.PointOfInterest,
                Position = position,
                Text = name,
                Icon = icon,
                Color = Colors.Red
            };
        }
    }

    /// <summary>
    /// Types of map decorations
    /// </summary>
    public enum MapDecorationType
    {
        /// <summary>Simple text label on the map</summary>
        Label,

        /// <summary>DM or player note</summary>
        Note,

        /// <summary>Persistent measurement line</summary>
        MeasurementLine,

        /// <summary>Circular area marker</summary>
        AreaMarker,

        /// <summary>Point of interest marker</summary>
        PointOfInterest,

        /// <summary>Custom polygon area</summary>
        CustomArea,

        /// <summary>Arrow indicator</summary>
        Arrow,

        /// <summary>Number marker (1, 2, 3...)</summary>
        NumberMarker
    }

    /// <summary>
    /// Represents a map link for multi-map support
    /// </summary>
    public class MapLink
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Position on the source map
        /// </summary>
        public Point SourcePosition { get; set; }

        /// <summary>
        /// Path to the target map file
        /// </summary>
        public string TargetMapPath { get; set; } = "";

        /// <summary>
        /// Position on the target map where tokens should appear
        /// </summary>
        public Point TargetPosition { get; set; }

        /// <summary>
        /// Display name for the link
        /// </summary>
        public string Name { get; set; } = "Map Link";

        /// <summary>
        /// Type of connection
        /// </summary>
        public MapLinkType LinkType { get; set; } = MapLinkType.Door;

        /// <summary>
        /// Whether the link is two-way
        /// </summary>
        public bool IsBidirectional { get; set; } = true;

        /// <summary>
        /// Icon to display
        /// </summary>
        public string Icon => LinkType switch
        {
            MapLinkType.Door => "🚪",
            MapLinkType.Stairs => "🪜",
            MapLinkType.Ladder => "🪜",
            MapLinkType.Portal => "🌀",
            MapLinkType.Trapdoor => "⬛",
            MapLinkType.Secret => "❓",
            _ => "🔗"
        };
    }

    public enum MapLinkType
    {
        Door,
        Stairs,
        Ladder,
        Portal,
        Trapdoor,
        Secret
    }
}
