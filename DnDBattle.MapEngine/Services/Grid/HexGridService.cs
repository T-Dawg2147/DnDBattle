using System;
using System.Collections.Generic;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Services.Grid
{
    /// <summary>
    /// Provides hex grid geometry helpers: vertex computation, A* pathfinding,
    /// and area (radius) calculations for hexagonal grids.
    /// </summary>
    public static class HexGridService
    {
        // ── Vertex computation ──

        /// <summary>
        /// Returns the six corner points of a hexagon centered at (<paramref name="cx"/>, <paramref name="cy"/>).
        /// </summary>
        public static (double x, double y)[] GetHexVertices(double cx, double cy, double size, GridType type)
        {
            var pts = new (double x, double y)[6];
            double startAngle = type == GridType.HexFlatTop ? 0 : Math.PI / 6.0;
            for (int i = 0; i < 6; i++)
            {
                double angle = startAngle + Math.PI / 3.0 * i;
                pts[i] = (cx + size * Math.Cos(angle), cy + size * Math.Sin(angle));
            }
            return pts;
        }

        // ── A* pathfinding on hex grid ──

        /// <summary>
        /// Find the shortest path between two hex coordinates using A*.
        /// Returns null when no path exists.
        /// </summary>
        public static List<HexCoord> FindPath(
            HexCoord start,
            HexCoord goal,
            Func<HexCoord, bool> isBlocked,
            int maxDepth)
        {
            if (maxDepth <= 0) maxDepth = Options.PathfindingMaxDepth;

            var openSet = new SortedSet<(int f, int insertOrder, HexCoord coord)>();
            var gScore = new Dictionary<HexCoord, int>();
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            int insertCounter = 0;

            gScore[start] = 0;
            openSet.Add((start.DistanceTo(goal), insertCounter++, start));

            int visited = 0;

            while (openSet.Count > 0 && visited < maxDepth)
            {
                var (_, _, current) = openSet.Min;
                openSet.Remove(openSet.Min);

                if (current == goal)
                    return ReconstructPath(cameFrom, current);

                visited++;

                foreach (var neighbor in current.GetNeighbors())
                {
                    if (isBlocked(neighbor)) continue;

                    int tentativeG = gScore[current] + 1;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        int f = tentativeG + neighbor.DistanceTo(goal);
                        openSet.Add((f, insertCounter++, neighbor));
                    }
                }
            }

            return null; // no path
        }

        private static List<HexCoord> ReconstructPath(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord current)
        {
            var path = new List<HexCoord> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }

        // ── Area helpers ──

        /// <summary>
        /// Returns all hex coordinates within <paramref name="radius"/> steps of <paramref name="center"/>.
        /// </summary>
        public static List<HexCoord> GetHexesInRadius(HexCoord center, int radius)
        {
            var result = new List<HexCoord>();
            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Math.Max(-radius, -q - radius);
                int r2 = Math.Min(radius, -q + radius);
                for (int r = r1; r <= r2; r++)
                {
                    result.Add(new HexCoord(center.Q + q, center.R + r));
                }
            }
            return result;
        }
    }
}
