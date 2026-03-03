using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Vision
{
    /// <summary>
    /// Calculates visible cells for tokens based on their vision properties,
    /// lighting state, and wall obstructions.
    /// </summary>
    public class VisionService
    {
        private readonly WallService _wallService;
        private readonly Dictionary<Guid, HashSet<(int x, int y)>> _visionCache = new();
        private readonly Dictionary<Guid, (int gx, int gy)> _lastPositions = new();

        public VisionService(WallService wallService)
        {
            _wallService = wallService;
        }

        /// <summary>
        /// Parses vision properties from the token's Senses string into a TokenVision object.
        /// </summary>
        public static TokenVision ParseVisionFromSenses(string senses)
        {
            var vision = new TokenVision();
            if (string.IsNullOrWhiteSpace(senses)) return vision;

            string lower = senses.ToLowerInvariant();

            // Darkvision
            var dvMatch = Regex.Match(lower, @"darkvision\s*(\d+)");
            if (dvMatch.Success && int.TryParse(dvMatch.Groups[1].Value, out int dvFeet))
            {
                vision.DarkvisionRange = dvFeet / 5;
                vision.Type = VisionType.Darkvision;
            }

            // Blindsight
            var bsMatch = Regex.Match(lower, @"blindsight\s*(\d+)");
            if (bsMatch.Success && int.TryParse(bsMatch.Groups[1].Value, out int bsFeet))
            {
                vision.BlindsightRange = bsFeet / 5;
                vision.Type = VisionType.Blindsight;
            }

            // Truesight
            var tsMatch = Regex.Match(lower, @"truesight\s*(\d+)");
            if (tsMatch.Success && int.TryParse(tsMatch.Groups[1].Value, out int tsFeet))
            {
                vision.TruesightRange = tsFeet / 5;
                vision.Type = VisionType.Truesight;
            }

            return vision;
        }

        /// <summary>
        /// Calculates visible cells for a token considering vision type, walls, and lighting.
        /// </summary>
        public HashSet<(int x, int y)> CalculateVisibleCells(
            Token token,
            IReadOnlyList<LightSource> lights,
            int gridWidth,
            int gridHeight)
        {
            if (!Options.EnableTokenVision)
                return GetAllCells(token, gridWidth, gridHeight);

            // Check cache
            if (_lastPositions.TryGetValue(token.Id, out var lastPos) &&
                lastPos == (token.GridX, token.GridY) &&
                _visionCache.TryGetValue(token.Id, out var cached))
            {
                return cached;
            }

            var visible = new HashSet<(int x, int y)>();
            var vision = token.Vision ?? new TokenVision();

            // Merge parsed senses into vision if vision hasn't been explicitly set
            if (vision.DarkvisionRange == 0 && vision.BlindsightRange == 0 && vision.TruesightRange == 0)
            {
                var parsed = ParseVisionFromSenses(token.Senses);
                vision.DarkvisionRange = parsed.DarkvisionRange;
                vision.BlindsightRange = parsed.BlindsightRange;
                vision.TruesightRange = parsed.TruesightRange;
                if (parsed.Type != VisionType.Normal)
                    vision.Type = parsed.Type;
            }

            int maxRange = Math.Max(vision.NormalRange,
                Math.Max(vision.DarkvisionRange,
                    Math.Max(vision.BlindsightRange, vision.TruesightRange)));

            if (maxRange <= 0) maxRange = Options.DefaultTokenVisionRange;

            var origin = new Point(token.GridX + 0.5, token.GridY + 0.5);

            for (int dx = -maxRange; dx <= maxRange; dx++)
            {
                for (int dy = -maxRange; dy <= maxRange; dy++)
                {
                    int tx = token.GridX + dx;
                    int ty = token.GridY + dy;

                    if (tx < 0 || ty < 0 || tx >= gridWidth || ty >= gridHeight)
                        continue;

                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist > maxRange) continue;

                    // Vision cone check
                    if (vision.HasVisionCone)
                    {
                        double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                        double diff = angle - vision.FacingAngle;
                        while (diff > 180) diff -= 360;
                        while (diff < -180) diff += 360;
                        if (Math.Abs(diff) > vision.VisionConeAngle / 2.0) continue;
                    }

                    var target = new Point(tx + 0.5, ty + 0.5);

                    // Blindsight ignores walls within range
                    bool inBlindsight = vision.BlindsightRange > 0 && dist <= vision.BlindsightRange;
                    if (!inBlindsight)
                    {
                        // Line of sight check through walls
                        if (!_wallService.HasLineOfSight(origin, target))
                            continue;
                    }

                    // Check if cell is lit (for normal vision)
                    bool cellLit = IsCellLit(tx, ty, lights);

                    // Normal vision: needs light
                    if (dist <= vision.NormalRange && cellLit)
                    {
                        visible.Add((tx, ty));
                        continue;
                    }

                    // Darkvision: sees in darkness within range
                    if (vision.DarkvisionRange > 0 && dist <= vision.DarkvisionRange)
                    {
                        visible.Add((tx, ty));
                        continue;
                    }

                    // Blindsight: sees within range regardless
                    if (inBlindsight)
                    {
                        visible.Add((tx, ty));
                        continue;
                    }

                    // Truesight: sees within range
                    if (vision.TruesightRange > 0 && dist <= vision.TruesightRange)
                    {
                        visible.Add((tx, ty));
                        continue;
                    }

                    // If cell is lit and within any range, it's visible
                    if (cellLit)
                    {
                        visible.Add((tx, ty));
                    }
                }
            }

            // Update cache
            _lastPositions[token.Id] = (token.GridX, token.GridY);
            _visionCache[token.Id] = visible;

            return visible;
        }

        /// <summary>
        /// Checks whether a cell is lit by any active light source.
        /// If no lights exist on the map, assumes ambient lighting (lit).
        /// </summary>
        public static bool IsCellLit(int x, int y, IReadOnlyList<LightSource> lights)
        {
            if (lights == null || lights.Count == 0)
                return true; // No lights placed = ambient lighting assumed

            // If all lights are disabled, still assume ambient
            bool anyEnabled = false;
            foreach (var light in lights)
            {
                if (!light.IsEnabled) continue;
                anyEnabled = true;

                double dx = x + 0.5 - light.CenterGrid.X;
                double dy = y + 0.5 - light.CenterGrid.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist <= light.RadiusSquares)
                {
                    if (light.Type == LightType.Directional)
                    {
                        if (!light.IsPointInCone(new Point(x + 0.5, y + 0.5)))
                            continue;
                    }
                    return true;
                }
            }

            // No enabled lights = ambient lighting
            if (!anyEnabled) return true;

            return false;
        }

        /// <summary>
        /// Gets the lighting level at a cell: 0 = darkness, 1 = dim, 2 = bright.
        /// </summary>
        public static int GetLightLevel(int x, int y, IReadOnlyList<LightSource> lights)
        {
            if (lights == null || lights.Count == 0) return 2;

            int best = 0;
            foreach (var light in lights)
            {
                if (!light.IsEnabled) continue;

                double dx = x + 0.5 - light.CenterGrid.X;
                double dy = y + 0.5 - light.CenterGrid.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (light.Type == LightType.Directional && !light.IsPointInCone(new Point(x + 0.5, y + 0.5)))
                    continue;

                if (dist <= light.BrightRadius)
                    return 2; // Bright
                if (dist <= light.DimRadius)
                    best = Math.Max(best, 1); // Dim
            }
            return best;
        }

        /// <summary>
        /// Clears all cached vision data.
        /// </summary>
        public void ClearCache()
        {
            _visionCache.Clear();
            _lastPositions.Clear();
        }

        /// <summary>
        /// Invalidates the cache for a specific token.
        /// </summary>
        public void InvalidateToken(Guid tokenId)
        {
            _visionCache.Remove(tokenId);
            _lastPositions.Remove(tokenId);
        }

        private static HashSet<(int x, int y)> GetAllCells(Token token, int gridWidth, int gridHeight)
        {
            var all = new HashSet<(int x, int y)>();
            int range = Options.DefaultTokenVisionRange;
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    int tx = token.GridX + dx;
                    int ty = token.GridY + dy;
                    if (tx >= 0 && ty >= 0 && tx < gridWidth && ty < gridHeight &&
                        Math.Sqrt(dx * dx + dy * dy) <= range)
                    {
                        all.Add((tx, ty));
                    }
                }
            }
            return all;
        }
    }
}
