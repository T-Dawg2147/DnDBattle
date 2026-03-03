using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Grid
{
    public class WallService
    {
        private readonly List<Wall> _walls = new List<Wall>();

        public IReadOnlyList<Wall> Walls => _walls.AsReadOnly();

        public event System.Action WallsChanged;

        public void AddWall(Wall wall)
        {
            _walls.Add(wall);
            WallsChanged?.Invoke();
        }

        public void RemoveWall(Wall wall)
        {
            _walls.Remove(wall);
            WallsChanged?.Invoke();
        }

        public void RemoveWall(Guid id)
        {
            var wall = _walls.FirstOrDefault(w => w.Id == id);
            if (wall != null)
            {
                _walls.Remove(wall);
                WallsChanged?.Invoke();
            }
        }

        public void Clear()
        {
            _walls.Clear();
            WallsChanged?.Invoke();
        }

        public Wall HitTest(Point gridPoint, double threshold = 0.5) =>
            _walls.FirstOrDefault(w => w.IsPointNear(gridPoint, threshold));

        public Wall HitTestEndPoint(Point gridPoint, out bool isStart, double threshold = 0.3)
        {
            isStart = false;
            foreach (var wall in _walls)
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

            foreach (var wall in _walls)
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
            foreach (var wall in _walls)
            {
                if (!wall.BlocksSight) continue;
                if (wall.IntersectsLine(from, to, out _))
                    return false;
            }
            return true;
        }

        public List<Wall> GetWallsInRadius(Point center, double radiusSquares)
        {
            return _walls.Where(w =>
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
