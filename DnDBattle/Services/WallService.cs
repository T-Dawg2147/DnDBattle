using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DnDBattle.Services
{
    public class WallService
    {
        private readonly List<Wall> _walls = new List<Wall>();

        #region Spatial Index
        // Grid-based spatial index for fast wall queries
        private readonly Dictionary<(int, int), List<Wall>> _spatialIndex = new();
        private const int CellSize = 5; // Grid squares per index cell
        private bool _indexDirty = true;

        /// <summary>
        /// Rebuilds the spatial index. Called automatically when walls change.
        /// </summary>
        private void RebuildSpatialIndex()
        {
            _spatialIndex.Clear();

            foreach (var wall in _walls)
            {
                IndexWall(wall);
            }

            _indexDirty = false;
        }

        /// <summary>
        /// Adds a wall to the spatial index
        /// </summary>
        private void IndexWall(Wall wall)
        {
            // Get bounding box of the wall
            int minX = (int)Math.Floor(Math.Min(wall.StartPoint.X, wall.EndPoint.X) / CellSize);
            int maxX = (int)Math.Floor(Math.Max(wall.StartPoint.X, wall.EndPoint.X) / CellSize);
            int minY = (int)Math.Floor(Math.Min(wall.StartPoint.Y, wall.EndPoint.Y) / CellSize);
            int maxY = (int)Math.Floor(Math.Max(wall.StartPoint.Y, wall.EndPoint.Y) / CellSize);

            // Add wall to all cells it touches
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var key = (x, y);
                    if (!_spatialIndex.TryGetValue(key, out var list))
                    {
                        list = new List<Wall>();
                        _spatialIndex[key] = list;
                    }
                    if (!list.Contains(wall))
                    {
                        list.Add(wall);
                    }
                }
            }
        }

        /// <summary>
        /// Gets walls that might be in the given bounds (fast spatial query)
        /// </summary>
        private HashSet<Wall> GetWallsInBounds(double minX, double minY, double maxX, double maxY)
        {
            if (_indexDirty) RebuildSpatialIndex();

            var result = new HashSet<Wall>();

            int cellMinX = (int)Math.Floor(minX / CellSize);
            int cellMaxX = (int)Math.Floor(maxX / CellSize);
            int cellMinY = (int)Math.Floor(minY / CellSize);
            int cellMaxY = (int)Math.Floor(maxY / CellSize);

            for (int x = cellMinX; x <= cellMaxX; x++)
            {
                for (int y = cellMinY; y <= cellMaxY; y++)
                {
                    if (_spatialIndex.TryGetValue((x, y), out var list))
                    {
                        foreach (var wall in list)
                        {
                            result.Add(wall);
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        public IReadOnlyList<Wall> Walls => _walls.AsReadOnly();

        public event System.Action WallsChanged;

        public void AddWall(Wall wall)
        {
            _walls.Add(wall);
            _indexDirty = true;
            WallsChanged?.Invoke();
        }

        public void RemoveWall(Wall wall)
        {
            _walls.Remove(wall);
            _indexDirty = true;
            WallsChanged?.Invoke();
        }

        public void RemoveWall(Guid id)
        {
            var wall = _walls.FirstOrDefault(w => w.Id == id);
            if (wall != null)
            {
                _walls.Remove(wall);
                _indexDirty = true;
                WallsChanged?.Invoke();
            }
        }

        public void Clear()
        {
            _walls.Clear();
            _spatialIndex.Clear();
            _indexDirty = false;
            WallsChanged?.Invoke();
        }

        public Wall HitTest(Point gridPoint, double threshold = 0.5)
        {
            // Use spatial index for faster lookup
            var candidates = GetWallsInBounds(
                gridPoint.X - threshold, gridPoint.Y - threshold,
                gridPoint.X + threshold, gridPoint.Y + threshold);

            return candidates.FirstOrDefault(w => w.IsPointNear(gridPoint, threshold));
        }

        public Wall HitTestEndPoint(Point gridPoint, out bool isStart, double threshold = 0.3)
        {
            isStart = false;

            // Use spatial index for faster lookup
            var candidates = GetWallsInBounds(
                gridPoint.X - threshold, gridPoint.Y - threshold,
                gridPoint.X + threshold, gridPoint.Y + threshold);

            foreach (var wall in candidates)
            {
                var distToStart = Math.Sqrt(
                    Math.Pow(gridPoint.X - wall.StartPoint.X, 2) +
                    Math.Pow(gridPoint.Y - wall.StartPoint.Y, 2));
                if (distToStart <= threshold)
                {
                    isStart = true;
                    return wall;
                }

                var distToEnd = Math.Sqrt(
                    Math.Pow(gridPoint.X - wall.EndPoint.X, 2) +
                    Math.Pow(gridPoint.Y - wall.EndPoint.Y, 2));
                if (distToEnd <= threshold)
                {
                    isStart = false;
                    return wall;
                }
            }
            return null;
        }

        public bool Raycast(Point origin, Point target, out Point hitPoint, out Wall hitWall)
        {
            hitPoint = target;
            hitWall = null;
            double closestDist = double.MaxValue;

            // Get bounding box of the ray
            double minX = Math.Min(origin.X, target.X);
            double maxX = Math.Max(origin.X, target.X);
            double minY = Math.Min(origin.Y, target.Y);
            double maxY = Math.Max(origin.Y, target.Y);

            // Only check walls in the ray's bounding box
            var candidates = GetWallsInBounds(minX, minY, maxX, maxY);

            foreach (var wall in candidates)
            {
                if (!wall.BlocksLight) continue;

                if (wall.IntersectsLine(origin, target, out Point intersection))
                {
                    var dist = Math.Sqrt(
                        Math.Pow(intersection.X - origin.X, 2) +
                        Math.Pow(intersection.Y - origin.Y, 2));

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        hitPoint = intersection;
                        hitWall = wall;
                    }
                }
            }
            return hitWall != null;
        }

        public bool HasLineOfSight(Point from, Point to)
        {
            // Get bounding box of the line
            double minX = Math.Min(from.X, to.X);
            double maxX = Math.Max(from.X, to.X);
            double minY = Math.Min(from.Y, to.Y);
            double maxY = Math.Max(from.Y, to.Y);

            // Only check walls in the line's bounding box
            var candidates = GetWallsInBounds(minX, minY, maxX, maxY);

            foreach (var wall in candidates)
            {
                if (!wall.BlocksSight) continue;
                if (wall.IntersectsLine(from, to, out _))
                    return false;
            }
            return true;
        }

        public List<Wall> GetWallsInRadius(Point center, double radiusSquares)
        {
            // Use spatial index for initial filtering
            var candidates = GetWallsInBounds(
                center.X - radiusSquares - 1,
                center.Y - radiusSquares - 1,
                center.X + radiusSquares + 1,
                center.Y + radiusSquares + 1);

            // Fine-grained distance check
            return candidates.Where(w =>
            {
                var distStart = Math.Sqrt(
                    Math.Pow(w.StartPoint.X - center.X, 2) +
                    Math.Pow(w.StartPoint.Y - center.Y, 2));
                var distEnd = Math.Sqrt(
                    Math.Pow(w.EndPoint.X - center.X, 2) +
                    Math.Pow(w.EndPoint.Y - center.Y, 2));

                return distStart <= radiusSquares + 1 || distEnd <= radiusSquares + 1;
            }).ToList();
        }

        public List<Point> ComputeLitPolygon(Point lightCenter, double radiusSquares, int rayCount = 360)
        {
            var polygon = new List<Point>();
            var relevantWalls = GetWallsInRadius(lightCenter, radiusSquares);

            for (int i = 0; i < rayCount; i++)
            {
                double angle = (2 * Math.PI * i) / rayCount;
                var direction = new Point(
                    lightCenter.X + Math.Cos(angle) * radiusSquares,
                    lightCenter.Y + Math.Sin(angle) * radiusSquares);

                Point hitPoint = direction;
                double closestDist = radiusSquares;

                foreach (var wall in relevantWalls)
                {
                    if (!wall.BlocksLight) continue;

                    if (wall.IntersectsLine(lightCenter, direction, out Point intersection))
                    {
                        var dist = Math.Sqrt(
                            Math.Pow(intersection.X - lightCenter.X, 2) +
                            Math.Pow(intersection.Y - lightCenter.Y, 2));

                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            hitPoint = intersection;
                        }
                    }
                }
                polygon.Add(hitPoint);
            }
            return polygon;
        }
    }
}