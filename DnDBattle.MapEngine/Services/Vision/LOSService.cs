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
using DnDBattle.Services.Grid;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Vision
{
    public static class LOSService
    {
        // Standard segment intersection
        private static bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double d1 = Cross(p4 - p3, p1 - p3);
            double d2 = Cross(p4 - p3, p2 - p3);
            double d3 = Cross(p2 - p1, p3 - p1);
            double d4 = Cross(p2 - p1, p4 - p1);
            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return true;

            // Colinear / endpoint checks
            if (Math.Abs(d1) < 1e-9 && OnSegment(p3, p4, p1)) return true;
            if (Math.Abs(d2) < 1e-9 && OnSegment(p3, p4, p2)) return true;
            if (Math.Abs(d3) < 1e-9 && OnSegment(p1, p2, p3)) return true;
            if (Math.Abs(d4) < 1e-9 && OnSegment(p1, p2, p4)) return true;
            return false;
        }

        private static double Cross(Vector a, Vector b) => a.X * b.Y - a.Y * b.X;

        private static bool OnSegment(Point a, Point b, Point p)
        {
            return Math.Min(a.X, b.X) - 1e-9 <= p.X && p.X <= Math.Max(a.X, b.X) + 1e-9 &&
                   Math.Min(a.Y, b.Y) - 1e-9 <= p.Y && p.Y <= Math.Max(a.Y, b.Y) + 1e-9 &&
                   Math.Abs(Cross(b - a, p - a)) < 1e-9;
        }

        // Point-in-polygon (winding number / ray casting)
        private static bool PointInPolygon(Point pt, List<Point> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var xi = poly[i].X; var yi = poly[i].Y;
                var xj = poly[j].X; var yj = poly[j].Y;
                bool intersect = ((yi > pt.Y) != (yj > pt.Y)) &&
                                 (pt.X < (xj - xi) * (pt.Y - yi) / (yj - yi + 1e-12) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
        }
    }
}
