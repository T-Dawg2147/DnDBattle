using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.Vision;

namespace DnDBattle.Services.Vision
{
    class LightingService
    {
        /*public static StreamGeometry ComputeLitGeometry(Point center, double radiusPx, IEnumerable<Obstacle> obstaclesPixelPolys, int maxAngles = 512)
        {
            var obstacleSegments = new List<(Point a, Point b)>();

            // gather obstacle vertices as candidate angles
            var candidateAngles = new List<double>();
            if (obstaclesPixelPolys != null)
            {
                foreach (var obs in obstaclesPixelPolys)
                {
                    var pts = obs.PolygonGridPoints;
                    for (int i = 0; i < pts.Count; i++)
                    {
                        var v = pts[i];
                        double ang = Math.Atan2(v.Y - center.Y, v.X - center.X);
                        // add small offsets to handle grazing edges
                        candidateAngles.Add(NormalizeAngle(ang - 1e-4));
                        candidateAngles.Add(NormalizeAngle(ang));
                        candidateAngles.Add(NormalizeAngle(ang + 1e-4));
                    }

                    // add segments for intersection tests
                    for (int i = 0; i < pts.Count; i++)
                    {
                        var a = pts[i];
                        var b = pts[(i + 1) % pts.Count];
                        obstacleSegments.Add((a, b));
                    }
                }
            }

            // If not enough vertex-driven angles, sample the circle uniformly
            int vertexAngleCount = candidateAngles.Count;
            if (vertexAngleCount < 16)
            {
                // create a base set of circle samples
                int circleSamples = Math.Min(Math.Max(16, maxAngles / 8), maxAngles);
                for (int i = 0; i < circleSamples; i++)
                {
                    candidateAngles.Add(NormalizeAngle(2.0 * Math.PI * i / circleSamples));
                }
            }

            // Deduplicate and sort angles
            var angles = candidateAngles.Distinct().OrderBy(a => a).ToList();

            // If we have too many angles, downsample evenly to maxAngles
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
                    if (SegmentIntersection(center, rayEnd, seg.a, seg.b, out Point ip))
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

            // Build geometry
            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                if (ptsOut.Count > 0)
                {
                    ctx.BeginFigure(ptsOut[0], true, true);
                    for (int i = 1; i < ptsOut.Count; i++)
                        ctx.LineTo(ptsOut[i], true, false);
                }
            }
            geom.Freeze();
            return geom;
        }*/

        private static double NormalizeAngle(double a)
        {
            while (a <= -Math.PI) a += 2 * Math.PI;
            while (a > Math.PI) a -= 2 * Math.PI;
            return a;
        }

        private static bool SegmentIntersection(Point p, Point q, Point r, Point s, out Point ip)
        {
            ip = new Point();
            var rvec = q - p;
            var svec = s - r;
            double rxs = Cross(rvec, svec);
            if (Math.Abs(rxs) < 1e-9) return false;

            double t = Cross(r - p, svec) / rxs;
            double u = Cross(r - p, rvec) / rxs;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                ip = p + t * rvec;
                return true;
            }
            return false;
        }

        private static double Cross(Vector a, Vector b) => a.X * b.Y - a.Y * b.X;
    }
}
