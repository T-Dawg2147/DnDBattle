using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace DnDBattle.Services
{
    public class SpatialIndex
    {
        private readonly Dictionary<(int, int), List<object>> _cells = new Dictionary<(int, int), List<object>>();
        private readonly int _cellResolution;

        public SpatialIndex(int cellResolution = 1)
        {
            _cellResolution = Math.Max(1, cellResolution);
        }

        public void Clear() => _cells.Clear();

        public void IndexObstacle(Obstacle obs)
        {
            if (obs?.PolygonGridPoints == null || obs.PolygonGridPoints.Count == 0) return;
            var bbox = GetBoundingBox(obs.PolygonGridPoints);
            for (int gx = (int)Math.Floor(bbox.minX); gx <= (int)Math.Ceiling(bbox.maxX); gx++)
            {
                for (int gy = (int)Math.Floor(bbox.minY); gy <= (int)Math.Ceiling(bbox.maxY); gy++)
                {
                    var key = (gx / _cellResolution, gy / _cellResolution);
                    if (!_cells.TryGetValue(key, out var list))
                    {
                        list = new List<object>();
                        _cells[key] = list;
                    }
                    if (!list.Contains(obs)) list.Add(obs);
                }
            }
        }

        public void IndexLight(LightSource light)
        {
            var bboxMinX = light.CenterGrid.X - light.RadiusSquares;
            var bboxMaxX = light.CenterGrid.X + light.RadiusSquares;
            var bboxMinY = light.CenterGrid.Y - light.RadiusSquares;
            var bboxMaxY = light.CenterGrid.Y + light.RadiusSquares;

            for (int gx = (int)Math.Floor(bboxMinX); gx <= (int)Math.Ceiling(bboxMaxX); gx++)
            {
                for (int gy = (int)Math.Floor(bboxMinY); gy <= (int)Math.Ceiling(bboxMaxY); gy++)
                {
                    var key = (gx / _cellResolution, gy / _cellResolution);
                    if (!_cells.TryGetValue(key, out var list)) { list = new List<object>(); _cells[key] = list; }
                    if (!list.Contains(light)) list.Add(light);
                }
            }
        }

        public IEnumerable<Obstacle> QueryObstaclesInCell(int gx, int gy)
        {
            var key = (gx / _cellResolution, gy / _cellResolution);
            if (_cells.TryGetValue(key, out var list)) return list.OfType<Obstacle>();
            return Enumerable.Empty<Obstacle>();
        }

        public IEnumerable<Obstacle> QueryObstaclesInBounds(double minX, double minY, double maxX, double maxY)
        {
            var results = new HashSet<Obstacle>();
            for (int gx = (int)Math.Floor(minX); gx <= (int)Math.Ceiling(maxX); gx++)
                for (int gy = (int)Math.Floor(minY); gy <= (int)Math.Ceiling(maxY); gy++)
                {
                    foreach (var o in QueryObstaclesInCell(gx, gy)) results.Add(o);
                }
            return results;
        }

        public IEnumerable<LightSource> QueryLightsInBounds(double minX, double minY, double maxX, double maxY)
        {
            var results = new HashSet<LightSource>();
            for (int gx = (int)Math.Floor(minX); gx <= (int)Math.Ceiling(maxX); gx++)
                for (int gy = (int)Math.Floor(minY); gy <= (int)Math.Ceiling(maxY); gy++)
                {
                    var key = (gx / _cellResolution, gy / _cellResolution);
                    if (_cells.TryGetValue(key, out var list))
                    {
                        foreach (var l in list.OfType<LightSource>()) results.Add(l);
                    }
                }
            return results;
        }

        private (double minX, double minY, double maxX, double maxY) GetBoundingBox(List<Point> pts)
        {
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity, maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
            foreach (var p in pts)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
            return (minX, minY, maxX, maxY);
        }
    }
}
