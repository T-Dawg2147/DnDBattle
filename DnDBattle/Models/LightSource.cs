using System;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Models
{
    /// <summary>
    /// Represents a light source on the battle grid
    /// </summary>
    public class LightSource
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Point CenterGrid { get; set; }

        /// <summary>Bright light radius in squares</summary>
        public double RadiusSquares { get; set; } = 6;

        /// <summary>Dim light radius in squares (typically 2x bright radius)</summary>
        public double DimRadiusSquares { get; set; } = 12;

        /// <summary>Light intensity (0.0 to 1.0)</summary>
        public double Intensity { get; set; } = 1.0;

        /// <summary>Light color</summary>
        public Color Color { get; set; } = Colors.White;

        /// <summary>Type of light source</summary>
        public LightType LightType { get; set; } = LightType.Point;

        /// <summary>Direction angle for directional lights (in degrees)</summary>
        public double DirectionAngle { get; set; } = 0;

        /// <summary>Cone angle for spotlights (in degrees)</summary>
        public double ConeAngle { get; set; } = 90;

        /// <summary>Whether this light creates magical darkness (blocks darkvision)</summary>
        public bool IsMagicalDarkness { get; set; } = false;

        /// <summary>Token that owns this light source (if any)</summary>
        public Guid? OwnerTokenId { get; set; }

        /// <summary>Display name for the light source</summary>
        public string Name { get; set; } = "Light";

        /// <summary>Whether the light is currently enabled</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Gets the bright light radius in feet</summary>
        public int BrightRadiusFeet => (int)(RadiusSquares * 5);

        /// <summary>Gets the dim light radius in feet</summary>
        public int DimRadiusFeet => (int)(DimRadiusSquares * 5);

        /// <summary>Creates a torch light</summary>
        public static LightSource CreateTorch(Point center)
        {
            return new LightSource
            {
                Name = "Torch",
                CenterGrid = center,
                RadiusSquares = 4, // 20ft bright
                DimRadiusSquares = 8, // 40ft dim
                Color = Color.FromRgb(255, 200, 100),
                Intensity = 0.9
            };
        }

        /// <summary>Creates a lantern light</summary>
        public static LightSource CreateLantern(Point center, bool hooded = false)
        {
            return new LightSource
            {
                Name = hooded ? "Hooded Lantern" : "Lantern",
                CenterGrid = center,
                RadiusSquares = hooded ? 6 : 6, // 30ft bright
                DimRadiusSquares = hooded ? 12 : 12, // 60ft dim
                Color = Color.FromRgb(255, 220, 150),
                Intensity = 1.0,
                LightType = hooded ? LightType.Directional : LightType.Point,
                ConeAngle = hooded ? 120 : 360
            };
        }

        /// <summary>Creates a candle light</summary>
        public static LightSource CreateCandle(Point center)
        {
            return new LightSource
            {
                Name = "Candle",
                CenterGrid = center,
                RadiusSquares = 1, // 5ft bright
                DimRadiusSquares = 2, // 10ft dim
                Color = Color.FromRgb(255, 180, 80),
                Intensity = 0.5
            };
        }

        /// <summary>Creates a magical light (Light cantrip)</summary>
        public static LightSource CreateLightCantrip(Point center)
        {
            return new LightSource
            {
                Name = "Light (Cantrip)",
                CenterGrid = center,
                RadiusSquares = 4, // 20ft bright
                DimRadiusSquares = 8, // 40ft dim
                Color = Colors.White,
                Intensity = 1.0
            };
        }

        /// <summary>Creates a daylight spell</summary>
        public static LightSource CreateDaylight(Point center)
        {
            return new LightSource
            {
                Name = "Daylight",
                CenterGrid = center,
                RadiusSquares = 12, // 60ft bright
                DimRadiusSquares = 24, // 120ft dim
                Color = Colors.White,
                Intensity = 1.0
            };
        }

        /// <summary>Creates magical darkness</summary>
        public static LightSource CreateDarkness(Point center)
        {
            return new LightSource
            {
                Name = "Darkness",
                CenterGrid = center,
                RadiusSquares = 3, // 15ft radius
                DimRadiusSquares = 3,
                Color = Color.FromRgb(10, 10, 20),
                Intensity = 1.0,
                IsMagicalDarkness = true
            };
        }

        /// <summary>Creates a colored magical light</summary>
        public static LightSource CreateColoredLight(Point center, Color color, string name = "Magical Light")
        {
            return new LightSource
            {
                Name = name,
                CenterGrid = center,
                RadiusSquares = 4,
                DimRadiusSquares = 8,
                Color = color,
                Intensity = 1.0
            };
        }
    }

    /// <summary>
    /// Type of light source
    /// </summary>
    public enum LightType
    {
        /// <summary>Emits light in all directions</summary>
        Point,

        /// <summary>Emits light in a specific direction (spotlight)</summary>
        Directional,

        /// <summary>Light from a window or opening</summary>
        Window,

        /// <summary>Ambient light covering the whole area</summary>
        Ambient
    }
}

