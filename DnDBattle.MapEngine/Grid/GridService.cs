using DnDBattle.Core.Enums;
using DnDBattle.Core.Interfaces;
using DnDBattle.GameLogic.Movement;
using System.Windows;

namespace DnDBattle.MapEngine.Grid;

public sealed class GridService : IGridService
{
    private readonly PathfindingService _pathfinding;
    private bool[,]? _walkabilityMap;

    public GridService(PathfindingService pathfinding, double cellSize = 50.0, GridType gridType = GridType.Square)
    {
        _pathfinding = pathfinding;
        CellSize = cellSize;
        GridType = gridType;
    }

    public double CellSize { get; set; }
    public GridType GridType { get; set; }
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }

    public void Initialize(int width, int height)
    {
        GridWidth = width;
        GridHeight = height;
        _walkabilityMap = new bool[width, height];
        for (int c = 0; c < width; c++)
            for (int r = 0; r < height; r++)
                _walkabilityMap[c, r] = true;
    }

    public void SetWalkable(int col, int row, bool walkable)
    {
        if (_walkabilityMap != null && col >= 0 && col < GridWidth && row >= 0 && row < GridHeight)
            _walkabilityMap[col, row] = walkable;
    }

    public Point CellToWorld(int col, int row) =>
        GridType == GridType.Hex
            ? HexCellToWorld(col, row)
            : new Point(col * CellSize + CellSize / 2, row * CellSize + CellSize / 2);

    public (int col, int row) WorldToCell(Point worldPosition) =>
        GridType == GridType.Hex
            ? WorldToHexCell(worldPosition)
            : ((int)(worldPosition.X / CellSize), (int)(worldPosition.Y / CellSize));

    public double DistanceBetween(Point a, Point b)
    {
        var (colA, rowA) = WorldToCell(a);
        var (colB, rowB) = WorldToCell(b);
        double dx = colB - colA;
        double dy = rowB - rowA;
        return Math.Sqrt(dx * dx + dy * dy) * 5.0; // feet
    }

    public IEnumerable<(int col, int row)> GetPathCells((int col, int row) from, (int col, int row) to)
    {
        if (_walkabilityMap == null) return Enumerable.Empty<(int, int)>();
        return _pathfinding.FindPath((from.col, from.row), (to.col, to.row),
            (c, r) => _walkabilityMap[c, r], GridWidth, GridHeight);
    }

    private Point HexCellToWorld(int col, int row)
    {
        double x = CellSize * (3.0 / 2.0 * col);
        double y = CellSize * (Math.Sqrt(3) * row + (col % 2 == 1 ? Math.Sqrt(3) / 2 : 0));
        return new Point(x, y);
    }

    private (int, int) WorldToHexCell(Point p)
    {
        int col = (int)(p.X / (CellSize * 1.5));
        int row = (int)(p.Y / (CellSize * Math.Sqrt(3)));
        return (col, row);
    }
}
