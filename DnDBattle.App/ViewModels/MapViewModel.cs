using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using DnDBattle.MapEngine.Tiles;

namespace DnDBattle.App.ViewModels;

public sealed partial class MapViewModel : ObservableObject
{
    private readonly ITileService _tiles;
    private readonly TilePalette _palette;
    private readonly IGridService _grid;

    [ObservableProperty] private TileDefinition? _selectedTile;
    [ObservableProperty] private bool _isPlacingTiles;
    [ObservableProperty] private int _activeLayer;

    public IReadOnlyList<TileDefinition> PaletteTiles => _palette.Tiles;

    public MapViewModel(ITileService tiles, TilePalette palette, IGridService grid)
    {
        _tiles = tiles;
        _palette = palette;
        _grid = grid;
    }

    [RelayCommand]
    private void CreateNewMap()
    {
        var map = new TileMap { Name = "New Map", Width = 24, Height = 16 };
        _tiles.LoadMap(map);
    }

    [RelayCommand]
    private void PlaceTileAt(System.Windows.Point worldPos)
    {
        if (SelectedTile is null || _tiles.CurrentMap is null) return;
        var (col, row) = _grid.WorldToCell(worldPos);
        _tiles.PlaceTile(col, row, ActiveLayer, SelectedTile);
    }
}
