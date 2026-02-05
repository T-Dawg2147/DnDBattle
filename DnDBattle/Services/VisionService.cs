using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Condition = DnDBattle.Models.Condition;

namespace DnDBattle.Services
{
    /// <summary>
    /// Service for managing token vision, line of sight, and visibility calculations.
    /// Implements Phase 4 vision features.
    /// </summary>
    public class VisionService
    {
        private readonly List<LightSource> _lightSources = new();

        public event System.Action? VisionChanged;

        public IReadOnlyList<LightSource> LightSources => _lightSources.AsReadOnly();

        #region Light Source Management

        public void AddLightSource(LightSource light)
        {
            _lightSources.Add(light);
            VisionChanged?.Invoke();
        }

        public void RemoveLightSource(Guid id)
        {
            _lightSources.RemoveAll(l => l.Id == id);
            VisionChanged?.Invoke();
        }

        public void RemoveLightSourcesForToken(Guid tokenId)
        {
            _lightSources.RemoveAll(l => l.OwnerTokenId == tokenId);
            VisionChanged?.Invoke();
        }

        public void UpdateLightSourcePosition(Guid id, Point newCenter)
        {
            var light = _lightSources.FirstOrDefault(l => l.Id == id);
            if (light != null)
            {
                light.CenterGrid = newCenter;
                VisionChanged?.Invoke();
            }
        }

        public void ClearAllLightSources()
        {
            _lightSources.Clear();
            VisionChanged?.Invoke();
        }

        #endregion

        #region Visibility Calculations

        /// <summary>
        /// Gets the light level at a specific grid cell
        /// </summary>
        public LightLevel GetLightLevelAtCell(int cellX, int cellY, IEnumerable<(Point a, Point b)>? wallSegments = null)
        {
            var devSettings = DevSettings.Instance;
            if (!devSettings.EnableTokenVision)
                return LightLevel.Bright; // Vision system disabled, everything visible

            Point cellCenter = new Point(cellX + 0.5, cellY + 0.5);
            double maxBrightness = 0;
            bool hasMagicalDarkness = false;

            foreach (var light in _lightSources.Where(l => l.IsEnabled))
            {
                if (!devSettings.EnableWallOcclusion || wallSegments == null || !IsBlocked(light.CenterGrid, cellCenter, wallSegments))
                {
                    double distance = GetDistance(light.CenterGrid, cellCenter);

                    if (light.IsMagicalDarkness)
                    {
                        if (distance <= light.RadiusSquares)
                        {
                            hasMagicalDarkness = true;
                        }
                    }
                    else
                    {
                        if (distance <= light.RadiusSquares)
                        {
                            // Bright light
                            double brightness = light.Intensity * (1.0 - (distance / light.RadiusSquares) * 0.3);
                            maxBrightness = Math.Max(maxBrightness, brightness);
                        }
                        else if (distance <= light.DimRadiusSquares)
                        {
                            // Dim light
                            double brightness = light.Intensity * 0.5 * (1.0 - ((distance - light.RadiusSquares) / (light.DimRadiusSquares - light.RadiusSquares)));
                            maxBrightness = Math.Max(maxBrightness, brightness);
                        }
                    }
                }
            }

            if (hasMagicalDarkness)
                return LightLevel.MagicalDarkness;

            if (maxBrightness >= 0.7)
                return LightLevel.Bright;
            if (maxBrightness >= 0.3)
                return LightLevel.Dim;
            return LightLevel.Darkness;
        }

        /// <summary>
        /// Determines if a token can see a specific cell based on their vision capabilities
        /// </summary>
        public bool CanTokenSeeCell(Token token, int cellX, int cellY, 
            IEnumerable<(Point a, Point b)>? wallSegments = null)
        {
            var devSettings = DevSettings.Instance;
            if (!devSettings.EnableTokenVision)
                return true;

            Point tokenCenter = new Point(token.GridX + 0.5, token.GridY + 0.5);
            Point cellCenter = new Point(cellX + 0.5, cellY + 0.5);
            double distance = GetDistance(tokenCenter, cellCenter);

            // Check if token is blind (condition)
            if (token.HasCondition(Condition.Blinded))
                return false;

            // Check line of sight if enabled
            if (devSettings.EnableLineOfSight && wallSegments != null)
            {
                // Tremorsense doesn't need line of sight but only works on ground
                if (devSettings.EnableTremorsense && token.HasTremorsense && 
                    distance <= token.TremorsenseRangeSquares)
                {
                    return true;
                }

                // Check for wall blocking
                if (IsBlocked(tokenCenter, cellCenter, wallSegments))
                {
                    // Truesight can see through illusions but not walls
                    // Blindsight doesn't need line of sight
                    if (devSettings.EnableBlindsight && token.HasBlindsight && 
                        distance <= token.BlindsightRangeSquares)
                    {
                        return true;
                    }
                    return false;
                }
            }

            // Get light level at target cell
            var lightLevel = GetLightLevelAtCell(cellX, cellY, wallSegments);

            // Check vision type capabilities
            switch (lightLevel)
            {
                case LightLevel.Bright:
                    return distance <= token.VisionRangeSquares;

                case LightLevel.Dim:
                    // Darkvision sees dim light as bright
                    if (devSettings.EnableDarkvision && token.HasDarkvision && 
                        distance <= token.DarkvisionRangeSquares)
                        return true;
                    // Normal vision can see in dim light (with disadvantage on Perception)
                    return distance <= token.VisionRangeSquares;

                case LightLevel.Darkness:
                    // Darkvision sees darkness as dim light
                    if (devSettings.EnableDarkvision && token.HasDarkvision && 
                        distance <= token.DarkvisionRangeSquares)
                        return true;
                    // Blindsight doesn't need light
                    if (devSettings.EnableBlindsight && token.HasBlindsight && 
                        distance <= token.BlindsightRangeSquares)
                        return true;
                    // Truesight can see in darkness
                    if (devSettings.EnableTruesight && token.HasTruesight && 
                        distance <= token.TruesightRangeSquares)
                        return true;
                    return false;

                case LightLevel.MagicalDarkness:
                    // Devil's Sight can see through magical darkness
                    if (devSettings.EnableDevilsSight && token.HasDevilsSight)
                        return distance <= token.VisionRangeSquares;
                    // Truesight can see through magical darkness
                    if (devSettings.EnableTruesight && token.HasTruesight && 
                        distance <= token.TruesightRangeSquares)
                        return true;
                    // Blindsight doesn't rely on light
                    if (devSettings.EnableBlindsight && token.HasBlindsight && 
                        distance <= token.BlindsightRangeSquares)
                        return true;
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Gets all cells visible to a token
        /// </summary>
        public HashSet<(int x, int y)> GetVisibleCells(Token token, int gridWidth, int gridHeight,
            IEnumerable<(Point a, Point b)>? wallSegments = null)
        {
            var visibleCells = new HashSet<(int x, int y)>();
            int maxRange = Math.Max(token.VisionRangeSquares, 
                Math.Max(token.DarkvisionRangeSquares,
                Math.Max(token.BlindsightRangeSquares,
                Math.Max(token.TruesightRangeSquares, token.TremorsenseRangeSquares))));

            int minX = Math.Max(0, token.GridX - maxRange);
            int maxX = Math.Min(gridWidth - 1, token.GridX + maxRange);
            int minY = Math.Max(0, token.GridY - maxRange);
            int maxY = Math.Min(gridHeight - 1, token.GridY + maxRange);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (CanTokenSeeCell(token, x, y, wallSegments))
                    {
                        visibleCells.Add((x, y));
                    }
                }
            }

            return visibleCells;
        }

        /// <summary>
        /// Checks if one token can see another token
        /// </summary>
        public bool CanTokenSeeToken(Token viewer, Token target, 
            IEnumerable<(Point a, Point b)>? wallSegments = null)
        {
            var devSettings = DevSettings.Instance;

            // Check if target is invisible
            if (target.HasCondition(Condition.Invisible))
            {
                // Truesight can see invisible creatures
                if (devSettings.EnableTruesight && viewer.HasTruesight)
                {
                    Point viewerCenter = new Point(viewer.GridX + 0.5, viewer.GridY + 0.5);
                    Point targetCenter = new Point(target.GridX + 0.5, target.GridY + 0.5);
                    double distance = GetDistance(viewerCenter, targetCenter);
                    if (distance > viewer.TruesightRangeSquares)
                        return false;
                }
                // Blindsight can detect invisible creatures
                else if (devSettings.EnableBlindsight && viewer.HasBlindsight)
                {
                    Point viewerCenter = new Point(viewer.GridX + 0.5, viewer.GridY + 0.5);
                    Point targetCenter = new Point(target.GridX + 0.5, target.GridY + 0.5);
                    double distance = GetDistance(viewerCenter, targetCenter);
                    if (distance > viewer.BlindsightRangeSquares)
                        return false;
                }
                else
                {
                    return false;
                }
            }

            return CanTokenSeeCell(viewer, target.GridX, target.GridY, wallSegments);
        }

        #endregion

        #region Helper Methods

        private static double GetDistance(Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool IsBlocked(Point from, Point to, IEnumerable<(Point a, Point b)> wallSegments)
        {
            foreach (var wall in wallSegments)
            {
                if (SegmentsIntersect(from, to, wall.a, wall.b))
                    return true;
            }
            return false;
        }

        private static bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double d1 = CrossProduct(p4 - p3, p1 - p3);
            double d2 = CrossProduct(p4 - p3, p2 - p3);
            double d3 = CrossProduct(p2 - p1, p3 - p1);
            double d4 = CrossProduct(p2 - p1, p4 - p1);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            const double epsilon = 1e-9;
            if (Math.Abs(d1) < epsilon && OnSegment(p3, p4, p1)) return true;
            if (Math.Abs(d2) < epsilon && OnSegment(p3, p4, p2)) return true;
            if (Math.Abs(d3) < epsilon && OnSegment(p1, p2, p3)) return true;
            if (Math.Abs(d4) < epsilon && OnSegment(p1, p2, p4)) return true;

            return false;
        }

        private static double CrossProduct(Vector a, Vector b) => a.X * b.Y - a.Y * b.X;

        private static bool OnSegment(Point a, Point b, Point p)
        {
            const double epsilon = 1e-9;
            return Math.Min(a.X, b.X) - epsilon <= p.X && p.X <= Math.Max(a.X, b.X) + epsilon &&
                   Math.Min(a.Y, b.Y) - epsilon <= p.Y && p.Y <= Math.Max(a.Y, b.Y) + epsilon;
        }

        #endregion
    }

    /// <summary>
    /// Light levels for visibility calculations
    /// </summary>
    public enum LightLevel
    {
        /// <summary>Fully lit - normal vision works</summary>
        Bright,

        /// <summary>Partially lit - disadvantage on Perception, darkvision sees as bright</summary>
        Dim,

        /// <summary>No light - darkvision sees as dim, normal vision fails</summary>
        Darkness,

        /// <summary>Magical darkness - blocks darkvision, only devil's sight/truesight/blindsight work</summary>
        MagicalDarkness
    }
}
