namespace DnDBattle.Core.Interfaces;

public interface IGridService
{
    double CellSize { get; }
    Enums.GridType GridType { get; }
    System.Windows.Point CellToWorld(int col, int row);
    (int col, int row) WorldToCell(System.Windows.Point worldPosition);
    double DistanceBetween(System.Windows.Point a, System.Windows.Point b);
    IEnumerable<(int col, int row)> GetPathCells((int col, int row) from, (int col, int row) to);
}
