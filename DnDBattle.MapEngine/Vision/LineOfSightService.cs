using DnDBattle.Core.Models;
using System.Windows;

namespace DnDBattle.MapEngine.Vision;

public sealed class LineOfSightService
{
    private IReadOnlyList<WallSegment> _walls = Array.Empty<WallSegment>();

    public void SetWalls(IReadOnlyList<WallSegment> walls) => _walls = walls;

    public bool HasLineOfSight(Point from, Point to)
    {
        foreach (var wall in _walls)
        {
            if (SegmentsIntersect(from, to, wall.Start, wall.End))
                return false;
        }
        return true;
    }

    public IReadOnlyList<Point> GetVisiblePoints(Point origin, double radiusPx, int rayCount = 360)
    {
        var visible = new List<Point>();
        for (int i = 0; i < rayCount; i++)
        {
            double angle = 2.0 * Math.PI * i / rayCount;
            var target = new Point(origin.X + Math.Cos(angle) * radiusPx,
                                   origin.Y + Math.Sin(angle) * radiusPx);
            if (HasLineOfSight(origin, target))
                visible.Add(target);
        }
        return visible;
    }

    private static bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
    {
        double d1 = Cross(p3, p4, p1);
        double d2 = Cross(p3, p4, p2);
        double d3 = Cross(p1, p2, p3);
        double d4 = Cross(p1, p2, p4);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
               ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    private static double Cross(Point o, Point a, Point b) =>
        (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
}
