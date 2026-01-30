using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.Mapping_Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DnDBattle.ViewModels
{
    public partial class TileMapBuilderViewModel : ObservableObject
    {
        private readonly TilePaletteService _paletteService;
        private readonly TileMapPersistenceService _persistenceService;
        private readonly TileImageCacheService _imageCache;

        public TileMapBuilderViewModel()
        {
            _paletteService = new TilePaletteService();
            _persistenceService = new TileMapPersistenceService();
            _imageCache = TileImageCacheService.Instance;

            _paletteService.TilesReloaded += OnTilesReloaded;

            CurrentMap = new TileMap();
            UpdateMapTilesCollection();
        }

        #region Observable Properties

        [ObservableProperty]
        private TileMap _currentMap;

        [ObservableProperty]
        private TileDefinition _selectedTileDefinition;

        [ObservableProperty]
        private string _selectedCategory = "All";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private EditorTool _currentTool = EditorTool.Paint;

        [ObservableProperty]
        private int _currentLayer = 0;

        [ObservableProperty]
        private int _brushRotation = 0;

        #endregion

        #region Collections

        public ObservableCollection<TileDefinition> AvailableTiles { get; } = new ObservableCollection<TileDefinition>();
        public ObservableCollection<TileDefinition> FilteredTiles { get; } = new ObservableCollection<TileDefinition>();
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string>();
        public ObservableCollection<Tile> MapTiles { get; } = new ObservableCollection<Tile>();

        #endregion

        #region Commands

        [RelayCommand]
        private async Task LoadPaletteAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading tiles...";

            try
            {
                await _paletteService.LoadTilesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading tiles: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshPaletteAsync()
        {
            await _paletteService.RefreshAsync();
            StatusMessage = $"Refreshed - {AvailableTiles.Count} tile available";
        }

        [RelayCommand]
        private void OpenTilesFolder()
        {
            _paletteService.OpenTilesFolder();
        }

        [RelayCommand]
        private void NewMap()
        {
            CurrentMap = new TileMap()
            {
                Name = "New Map",
                WidthInSquares = 30,
                HeightInSquares = 20
            };
            UpdateMapTilesCollection();
            StatusMessage = "Created new map";
        }

        [RelayCommand]
        private async Task SaveMapAsync()
        {
            try
            {
                var dialog = new SaveFileDialog()
                {
                    Filter = "File Map (*.json)|*.json",
                    FileName = CurrentMap.Name,
                    InitialDirectory = _persistenceService.MapsFolder
                };

                if (dialog.ShowDialog() == true)
                {
                    await _persistenceService.SaveMapAsync(CurrentMap, dialog.FileName);
                    StatusMessage = $"Saved: {CurrentMap.Name}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving map: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadMapAsync()
        {
            try
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Tile Map (*.json)|*.json",
                    InitialDirectory = _persistenceService.MapsFolder
                };

                if (dialog.ShowDialog() == true)
                {
                    CurrentMap = await _persistenceService.LoadMapAsync(dialog.FileName);
                    UpdateMapTilesCollection();
                    StatusMessage = $"Loaded: {CurrentMap.Name}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading map: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SetTool(EditorTool tool)
        {
            CurrentTool = tool;
            StatusMessage = $"Tool: {tool}";
        }

        [RelayCommand]
        private void RotateBrush()
        {
            BrushRotation = (BrushRotation + 90) % 360;
        }

        [RelayCommand]
        private void ClearMap()
        {
            if (MessageBox.Show("Clear all tiles from the map?", "Confirm Clear",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                CurrentMap.Tiles.Clear();
                UpdateMapTilesCollection();
                StatusMessage = "Map cleared";
            }
        }

        #endregion

        #region Painting Methods

        public void PaintTile(int gridX, int gridY)
        {
            if (SelectedTileDefinition == null || CurrentTool != EditorTool.Paint)
                return;

            if (gridX < 0 || gridY < 0 || gridX >= CurrentMap.WidthInSquares || gridY >= CurrentMap.HeightInSquares)
                return;

            var existingTile = CurrentMap.Tiles.FirstOrDefault(t =>
                t.GridX == gridX && t.GridY == gridY && t.Layer == CurrentLayer);

            if (existingTile != null)
                CurrentMap.Tiles.Remove(existingTile);

            var newTile = new Tile()
            {
                TileDefinitionId = SelectedTileDefinition.Id,
                GridX = gridX,
                GridY = gridY,
                Layer = CurrentLayer,
                Rotation = BrushRotation
            };

            CurrentMap.Tiles.Add(newTile);
            UpdateMapTilesCollection();
        }

        public void EraseTile(int gridX, int gridY)
        {
            if (CurrentTool != EditorTool.Erase)
                return;

            var tile = CurrentMap.Tiles.FirstOrDefault(t =>
                t.GridX == gridX && t.GridY == gridY && t.Layer == CurrentLayer);

            if (tile != null)
            {
                CurrentMap.Tiles.Remove(tile);
                UpdateMapTilesCollection();
            }
        }

        public void PickTile(int gridX, int gridY)
        {
            var tile = CurrentMap.Tiles
                .Where(t => t.GridX == gridX && t.GridY == gridY)
                .OrderByDescending(t => t.Layer)
                .FirstOrDefault();

            if (tile != null)
            {
                var definition = _paletteService.GetTileDefinition(tile.TileDefinitionId);
                if (definition != null)
                {
                    SelectedTileDefinition = definition;
                    BrushRotation = tile.Rotation;
                    CurrentLayer = tile.Layer;
                    CurrentTool = EditorTool.Paint;
                    StatusMessage = $"Picked: {definition.Name}";
                }
            }
        }

        public BitmapImage GetTileImage(Tile tile)
        {
            return _imageCache.GetImage(tile.TileDefinitionId);
        }

        public TileDefinition GetTileDefinition(string id)
        {
            return _paletteService.GetTileDefinition(id);
        }

        #endregion

        #region Private Methods

        private void OnTilesReloaded()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailableTiles.Clear();
                Categories.Clear();
                Categories.Add("All");

                foreach (var tile in _paletteService.TileDefinitions)
                {
                    AvailableTiles.Add(tile);
                }

                foreach (var category in _paletteService.Categories)
                {
                    Categories.Add(category);
                }

                FilterTiles();
                StatusMessage = $"Loaded {AvailableTiles.Count} tiles in {Categories.Count - 1} categories";
            });
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            FilterTiles();
        }

        private void FilterTiles()
        {
            FilteredTiles.Clear();

            var tiles = SelectedCategory == "All"
                ? AvailableTiles
                : AvailableTiles.Where(t => t.Category == SelectedCategory);

            foreach (var tile in tiles)
            {
                FilteredTiles.Add(tile);
            }
        }

        private void UpdateMapTilesCollection()
        {
            MapTiles.Clear();
            foreach (var tile in CurrentMap.Tiles.OrderBy(t => t.Layer).ThenBy(t => t.GridY).ThenBy(t => t.GridX))
            {
                MapTiles.Add(tile);
            }
        }

        #endregion
    }

    public enum EditorTool
    {
        Paint,
        Erase, 
        Pick,   // Eyedropper
        Fill,   // Bucket fill (future)
        Select  // Selection tool (future)
    }
}
