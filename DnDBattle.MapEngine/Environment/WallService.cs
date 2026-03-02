using DnDBattle.Core.Models;
using System.Windows;

namespace DnDBattle.MapEngine.Environment;

public sealed class WallService
{
    private readonly List<WallSegment> _walls = new();

    public IReadOnlyList<WallSegment> Walls => _walls;

    public void AddWall(Point start, Point end, bool blocksVision = true) =>
        _walls.Add(new WallSegment(start, end, blocksVision));

    public void RemoveWallsNear(Point point, double threshold = 5.0) =>
        _walls.RemoveAll(w =>
            (w.Start - point).Length < threshold || (w.End - point).Length < threshold);

    public void Clear() => _walls.Clear();

    public IReadOnlyList<WallSegment> GetVisionBlockers() =>
        _walls.Where(w => w.BlocksVision).ToList();
}
