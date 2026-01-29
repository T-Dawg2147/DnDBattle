using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DnDBattle.Models;

namespace DnDBattle.Services
{
    public class LightingService
    {
        // List of current lights (could also be managed elsewhere)
        public List<LightSource> Lights { get; } = new List<LightSource>();

        /// <summary>
        /// Calculate the lit region from a given light source, considering obstacles.
        /// </summary>
        /// <param name="center">Light center in pixel coordinates</param>
        /// <param name="radiusPx">Light radius in pixels</param>
        /// <param name="obstaclesPixelPolys">List of polygons representing obstacles (in pixel coords)</param>
        /// <param name="maxAngles">Ray count (quality/cost tradeoff)</param>
        /// <returns>Geometry representing the lit area</returns>
        public static StreamGeometry GetLitGeometry(Point center, double radiusPx, IEnumerable<List<Point>> obstaclesPixelPolys, int maxAngles = 512)
        {
            var obstacleSegments = new List<(Point a, Point b)>();
            var candidateAngles = new List<double>();

            if (obstaclesPixelPolys != null)
            {
                foreach (var poly in obstaclesPixelPolys)
                {
                    for (int i = 0; i < poly.Count; i++)
                    {
                        var v = poly[i];
                        double ang = Math.Atan2(v.Y - center.Y, v.X - center.X);
                        candidateAngles.Add(NormalizeAngle(ang - 1e-4));
                        candidateAngles.Add(NormalizeAngle(ang));
                        candidateAngles.Add(NormalizeAngle(ang + 1e-4));

                        var a = poly[i];
                        var b = poly[(i + 1) % poly.Count];
                        obstacleSegments.Add((a, b));
                    }
                }
            }

            // If too few, add uniform circle
            int baseSamples = Math.Min(Math.Max(16, maxAngles / 8), maxAngles);
            if (candidateAngles.Count < 16)
            {
                for (int i = 0; i < baseSamples; i++)
                    candidateAngles.Add(NormalizeAngle(2.0 * Math.PI * i / baseSamples));
            }
            var angles = candidateAngles.Distinct().OrderBy(a => a).ToList();
            if (angles.Count > maxAngles)
            {
                var reduced = new List<double>();
                for (int i = 0; i < maxAngles; i++)
                {
                    int idx = (int)Math.Floor(i * (angles.Count / (double)maxAngles));
                    reduced.Add(angles[idx]);
                }
                angles = reduced.Distinct().OrderBy(a => a).ToList();
            }

            var ptsOut = new List<Point>();
            foreach (var ang in angles)
            {
                var dir = new Vector(Math.Cos(ang), Math.Sin(ang));
                var rayEnd = center + dir * radiusPx;

                double nearestDist = double.PositiveInfinity;
                Point? nearestPt = null;

                foreach (var seg in obstacleSegments)
                {
                    if (SegmentsIntersect(center, rayEnd, seg.a, seg.b, out Point ip))
                    {
                        double d = (ip - center).Length;
                        if (d < nearestDist)
                        {
                            nearestDist = d;
                            nearestPt = ip;
                        }
                    }
                }

                if (nearestPt.HasValue)
                {
                    // clamp to radius
                    double d = (nearestPt.Value - center).Length;
                    if (d > radiusPx) nearestPt = center + dir * radiusPx;
                    ptsOut.Add(nearestPt.Value);
                }
                else
                {
                    ptsOut.Add(rayEnd);
                }
            }

            var geom = new StreamGeometry();
            using (var gc = geom.Open())
            {
                if (ptsOut.Count > 0)
                {
                    gc.BeginFigure(ptsOut[0], true, true);
                    gc.PolyLineTo(ptsOut.Skip(1).ToList(), true, false);
                }
            }
            geom.Freeze();
            return geom;
        }

        private static double NormalizeAngle(double a)
        {
            while (a < 0) a += 2 * Math.PI;
            while (a > 2 * Math.PI) a -= 2 * Math.PI;
            return a;
        }

        /// <summary>Ray segments intersection—returns true and outputs intersection point if intersects, else false.</summary>
        private static bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4, out Point intersection)
        {
            intersection = new Point();
            double dx1 = p2.X - p1.X, dy1 = p2.Y - p1.Y;
            double dx2 = p4.X - p3.X, dy2 = p4.Y - p3.Y;
            double denom = dx1 * dy2 - dy1 * dx2;
            if (Math.Abs(denom) < 1e-10) return false;

            double t = ((p3.X - p1.X) * dy2 - (p3.Y - p1.Y) * dx2) / denom;
            double u = ((p3.X - p1.X) * dy1 - (p3.Y - p1.Y) * dx1) / denom;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                intersection = new Point(p1.X + t * dx1, p1.Y + t * dy1);
                return true;
            }
            return false;
        }
    }
}