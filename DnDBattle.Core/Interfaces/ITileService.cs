using DnDBattle.Core.Models;

namespace DnDBattle.Core.Interfaces;

public interface ITileService
{
    TileMap? CurrentMap { get; }
    void LoadMap(TileMap map);
    void PlaceTile(int col, int row, int layer, TileDefinition tile);
    void RemoveTile(int col, int row, int layer);
    TileDefinition? GetTile(int col, int row, int layer);
    Task SaveMapAsync(string filePath, CancellationToken ct = default);
}
