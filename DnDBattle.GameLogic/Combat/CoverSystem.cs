using DnDBattle.Core.Enums;
using DnDBattle.Core.Models;

namespace DnDBattle.GameLogic.Combat;

public static class CoverSystem
{
    public static CoverType DetermineCover(
        System.Windows.Point attacker,
        System.Windows.Point target,
        IEnumerable<Obstacle> walls)
    {
        var wallList = walls.ToList();
        if (wallList.Count == 0) return CoverType.None;

        int intersections = wallList.Count(w =>
            SegmentsIntersect(attacker, target, w.Start, w.End));

        return intersections switch
        {
            0 => CoverType.None,
            1 => CoverType.Half,
            2 => CoverType.ThreeQuarters,
            _ => CoverType.Full
        };
    }

    // Using int.MaxValue to represent that full cover makes a target unattackable
    public static int GetCoverBonus(CoverType cover) => cover switch
    {
        CoverType.Half => 2,
        CoverType.ThreeQuarters => 5,
        CoverType.Full => int.MaxValue,
        _ => 0
    };

    private static bool SegmentsIntersect(
        System.Windows.Point p1, System.Windows.Point p2,
        System.Windows.Point p3, System.Windows.Point p4)
    {
        double d1 = Cross(p3, p4, p1);
        double d2 = Cross(p3, p4, p2);
        double d3 = Cross(p1, p2, p3);
        double d4 = Cross(p1, p2, p4);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        return false;
    }

    private static double Cross(System.Windows.Point o, System.Windows.Point a, System.Windows.Point b) =>
        (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
}
