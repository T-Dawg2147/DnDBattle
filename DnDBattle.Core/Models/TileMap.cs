namespace DnDBattle.Core.Models;

public class TileMap
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "Unnamed Map";
    public int Width { get; init; }
    public int Height { get; init; }
    public double CellSizePx { get; set; } = 50.0;
    public Enums.GridType GridType { get; set; } = Enums.GridType.Square;
    public Dictionary<(int Col, int Row, int Layer), TileDefinition> Tiles { get; } = new();
    public List<WallSegment> Walls { get; } = new();
    public List<MapNote> Notes { get; } = new();
}

public record WallSegment(System.Windows.Point Start, System.Windows.Point End, bool BlocksVision = true);
public record MapNote(Guid Id, int Col, int Row, string Text, bool IsSecret = false);
