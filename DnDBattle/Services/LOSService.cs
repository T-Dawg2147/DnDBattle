using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DnDBattle.Services
{
    public static class LOSService
    {
        // Simple segment vs polygon intersection-based LOS checker.
        // Points are in grid coordinates (cells). Obstacles are polygons in grid coords.
        public static bool HasLineOfSight(Point from, Point to, IEnumerable<Obstacle> obstacles)
        {
            // If any obstacle polygon intersects the segment, LOS is blocked.
            foreach (var obs in obstacles)
            {
                if (PolygonIntersectsSegment(obs.PolygonGridPoints, from, to)) return false;
            }
            return true;
        }

        // Check if segment intersects polygon (including edges)
        private static bool PolygonIntersectsSegment(List<Point> poly, Point a, Point b)
        {
            int n = poly.Count;
            if (n < 2) return false;
            for (int i = 0; i < n; i++)
            {
                var p1 = poly[i];
                var p2 = poly[(i + 1) % n];
                if (SegmentsIntersect(a, b, p1, p2)) return true;
            }
            // Additionally, if segment midpoint is inside polygon, treat as intersection
            var mid = new Point((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);
            if (PointInPolygon(mid, poly)) return true;
            return false;
        }

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


        // Helper method
        // Expose a reusable point-in-polygon test for other services.
        public static bool PointInPolygonInternal(Point pt, List<Point> poly)
        {
            if (poly == null || poly.Count == 0) return false;
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var xi = poly[i].X; var yi = poly[i].Y;
                var xj = poly[i].X; var yj = poly[i].Y;
                bool intersect = ((yi > pt.Y) != (yj > pt.Y)) &&
                                 (pt.X < (xj - xi) * (pt.Y - yi) / (yj - yi + 1e-12) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
        }
    }
}
