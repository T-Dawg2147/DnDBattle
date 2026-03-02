using DnDBattle.Core.Models;

namespace DnDBattle.MapEngine.Tiles;

public sealed class TilePalette
{
    private readonly List<TileDefinition> _tiles = new();

    public IReadOnlyList<TileDefinition> Tiles => _tiles;

    public TilePalette AddTile(TileDefinition tile) { _tiles.Add(tile); return this; }

    public TilePalette RemoveTile(Guid id) { _tiles.RemoveAll(t => t.Id == id); return this; }

    public TileDefinition? FindById(Guid id) => _tiles.FirstOrDefault(t => t.Id == id);

    public static TilePalette CreateDefault() => new TilePalette()
        .AddTile(new TileDefinition(Guid.NewGuid(), "Stone Floor",
            "Resources/Tiles/Floor/dungeon_floor_01.png", true, false, false, false, false, 1.0))
        .AddTile(new TileDefinition(Guid.NewGuid(), "Stone Wall",
            "Resources/Tiles/Wall/stone_wall.png", false, true, true, false, false, 0.0))
        .AddTile(new TileDefinition(Guid.NewGuid(), "Wooden Door",
            "Resources/Tiles/Door/door.png", true, false, false, true, false, 1.0))
        .AddTile(new TileDefinition(Guid.NewGuid(), "Difficult Terrain",
            "Resources/Tiles/Floor/dungeon_floor_01.png", true, false, false, false, true, 2.0));
}
