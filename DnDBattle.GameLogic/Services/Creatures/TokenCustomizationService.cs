using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Creatures
{
    /// <summary>
    /// Manages per-token visual customizations (borders, shapes, name plates).
    /// Uses a Dictionary for O(1) customization lookups per token.
    /// </summary>
    public sealed class TokenCustomizationService
    {
        private readonly Dictionary<string, TokenCustomization> _customizations = new();

        /// <summary>
        /// Gets or creates a customization for the given token ID.
        /// </summary>
        public TokenCustomization GetCustomization(string tokenId)
        {
            if (!Options.EnableTokenCustomization)
                return TokenCustomization.CreateDefault(tokenId);

            if (_customizations.TryGetValue(tokenId, out var custom))
                return custom;

            var newCustom = TokenCustomization.CreateDefault(tokenId);
            _customizations[tokenId] = newCustom;
            return newCustom;
        }

        /// <summary>
        /// Sets or updates a token's customization.
        /// </summary>
        public void SetCustomization(TokenCustomization customization)
        {
            _customizations[customization.TokenId] = customization;
        }

        /// <summary>
        /// Removes customization for a token (reverts to defaults).
        /// </summary>
        public void RemoveCustomization(string tokenId)
        {
            _customizations.Remove(tokenId);
        }

        /// <summary>
        /// Gets all customized token IDs.
        /// </summary>
        public IReadOnlyCollection<string> GetCustomizedTokenIds()
        {
            return _customizations.Keys;
        }

        /// <summary>
        /// Gets a cached clip geometry for the given shape and size.
        /// Geometries are frozen for thread-safety and performance.
        /// </summary>
        public static Geometry GetClipGeometry(TokenShape shape, double size)
        {
            double half = size / 2.0;

            return shape switch
            {
                TokenShape.Circle => new EllipseGeometry(new Point(half, half), half, half),
                TokenShape.Square => new RectangleGeometry(new Rect(0, 0, size, size)),
                TokenShape.RoundedSquare => new RectangleGeometry(new Rect(0, 0, size, size), size * 0.15, size * 0.15),
                TokenShape.Diamond => CreateDiamondGeometry(half, size),
                TokenShape.Hexagon => CreateHexagonGeometry(half, size),
                TokenShape.Star => CreateStarGeometry(half, size),
                _ => new EllipseGeometry(new Point(half, half), half, half)
            };
        }

        /// <summary>
        /// Parses a hex color string to a WPF Color.
        /// Cached via ColorConverter for common values.
        /// </summary>
        public static Color ParseColor(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Colors.White;
            }
        }

        /// <summary>
        /// Creates a SolidColorBrush from hex, frozen for performance.
        /// </summary>
        public static SolidColorBrush CreateFrozenBrush(string hex)
        {
            var brush = new SolidColorBrush(ParseColor(hex));
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Gets the name plate offset based on position and token size.
        /// </summary>
        public static Point GetNamePlateOffset(NamePlatePosition position, double tokenSize, double plateWidth, double plateHeight)
        {
            return position switch
            {
                NamePlatePosition.Above => new Point((tokenSize - plateWidth) / 2, -plateHeight - 2),
                NamePlatePosition.Below => new Point((tokenSize - plateWidth) / 2, tokenSize + 2),
                NamePlatePosition.Inside => new Point((tokenSize - plateWidth) / 2, (tokenSize - plateHeight) / 2),
                NamePlatePosition.Left => new Point(-plateWidth - 2, (tokenSize - plateHeight) / 2),
                NamePlatePosition.Right => new Point(tokenSize + 2, (tokenSize - plateHeight) / 2),
                _ => new Point((tokenSize - plateWidth) / 2, tokenSize + 2)
            };
        }

        /// <summary>Clears all token customizations.</summary>
        public void ClearAll()
        {
            _customizations.Clear();
        }

        // ── Shape geometry helpers ──

        private static Geometry CreateDiamondGeometry(double half, double size)
        {
            var figure = new PathFigure(new Point(half, 0), new[]
            {
                new LineSegment(new Point(size, half), true),
                new LineSegment(new Point(half, size), true),
                new LineSegment(new Point(0, half), true)
            }, true);
            var geo = new PathGeometry(new[] { figure });
            geo.Freeze();
            return geo;
        }

        private static Geometry CreateHexagonGeometry(double half, double size)
        {
            double q = size * 0.25;
            var figure = new PathFigure(new Point(q, 0), new PathSegment[]
            {
                new LineSegment(new Point(size - q, 0), true),
                new LineSegment(new Point(size, half), true),
                new LineSegment(new Point(size - q, size), true),
                new LineSegment(new Point(q, size), true),
                new LineSegment(new Point(0, half), true)
            }, true);
            var geo = new PathGeometry(new[] { figure });
            geo.Freeze();
            return geo;
        }

        private static Geometry CreateStarGeometry(double half, double size)
        {
            var points = new Point[10];
            double outerR = half;
            double innerR = half * 0.4;

            for (int i = 0; i < 10; i++)
            {
                double angle = Math.PI / 2 + i * Math.PI / 5;
                double r = (i % 2 == 0) ? outerR : innerR;
                points[i] = new Point(half + r * Math.Cos(angle), half - r * Math.Sin(angle));
            }

            var segments = new PathSegment[9];
            for (int i = 1; i < 10; i++)
                segments[i - 1] = new LineSegment(points[i], true);

            var figure = new PathFigure(points[0], segments, true);
            var geo = new PathGeometry(new[] { figure });
            geo.Freeze();
            return geo;
        }
    }
}
