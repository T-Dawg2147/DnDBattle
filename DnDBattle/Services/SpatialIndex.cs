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

        public IEnumerable<LightSource> QueryLightsInBounds(double minX, double minY, double maxX, double maxY)
        {
            var results = new HashSet<LightSource>();
            for (int gx = (int)Math.Floor(minX); gx <= (int)Math.Ceiling(maxX); gx++)
                for (int gy = (int)Math.Floor(minY); gy <= (int)Math.Ceiling(maxY); gy++)
                {
                    var key = (gx / _cellResolution, gy / _cellResolution);
                    if (_cells.TryGetValue(key, out var list))
                    {
                        foreach (var item in list)
                        {
                            if (item is LightSource l)
                                results.Add(l);
                        }
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
