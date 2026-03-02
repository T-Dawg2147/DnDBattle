using DnDBattle.Core.Enums;

namespace DnDBattle.App.Configuration;

public sealed class GridSettings
{
    public double CellSizePx { get; set; } = 50.0;
    public GridType GridType { get; set; } = GridType.Square;
    public bool ShowGrid { get; set; } = true;
    public bool SnapToGrid { get; set; } = true;
    public int DefaultMapWidth { get; set; } = 24;
    public int DefaultMapHeight { get; set; } = 16;
}
