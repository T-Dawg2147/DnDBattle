using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;

namespace DnDBattle.MapEngine.Tiles;

public sealed class TileService : ITileService
{
    public TileMap? CurrentMap { get; private set; }

    public void LoadMap(TileMap map) => CurrentMap = map;

    public void PlaceTile(int col, int row, int layer, TileDefinition tile)
    {
        if (CurrentMap is null) return;
        CurrentMap.Tiles[(col, row, layer)] = tile;
    }

    public void RemoveTile(int col, int row, int layer)
    {
        CurrentMap?.Tiles.Remove((col, row, layer));
    }

    public TileDefinition? GetTile(int col, int row, int layer) =>
        CurrentMap?.Tiles.GetValueOrDefault((col, row, layer));

    public Task SaveMapAsync(string filePath, CancellationToken ct = default)
    {
        // Serialization is delegated to IPersistenceService in the App layer
        return Task.CompletedTask;
    }
}
