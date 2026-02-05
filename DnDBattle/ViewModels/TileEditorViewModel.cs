using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DnDBattle.ViewModels
{
    /// <summary>
    /// ViewModel for the Tile Map Editor - manages tile editing state and operations
    /// </summary>
    public partial class TileEditorViewModel : ObservableObject
    {
        #region Fields

        private readonly TileMapService _mapService;
        private readonly TileLibraryService _libraryService;
        private string _currentFilePath;

        #endregion

        #region Properties - Map

        [ObservableProperty]
        private TileMap _currentMap;

        [ObservableProperty]
        private string _mapName = "New Map";

        [ObservableProperty]
        private int _mapWidth = 50;

        [ObservableProperty]
        private int _mapHeight = 50;

        [ObservableProperty]
        private double _cellSize = 48.0;

        [ObservableProperty]
        private bool _showGrid = true;

        [ObservableProperty]
        private bool _hasUnsavedChanges;

        #endregion

        #region Properties - Selection

        [ObservableProperty]
        private TileDefinition _selectedTileDefinition;

        [ObservableProperty]
        private PlacedTile _selectedTile;

        [ObservableProperty]
        private TileLayer _activeLayer = TileLayer.Floor;

        #endregion

        #region Properties - Palette

        [ObservableProperty]
        private ObservableCollection<TileDefinition> _tilePalette;

        [ObservableProperty]
        private ObservableCollection<TileDefinition> _filteredTilePalette;

        [ObservableProperty]
        private string _searchFilter = "";

        [ObservableProperty]
        private string _categoryFilter = "All";

        #endregion

        #region Properties - Layer Visibility

        [ObservableProperty]
        private bool _showFloorLayer = true;

        [ObservableProperty]
        private bool _showTerrainLayer = true;

        [ObservableProperty]
        private bool _showWallLayer = true;

        [ObservableProperty]
        private bool _showDoorLayer = true;

        [ObservableProperty]
        private bool _showFurnitureLayer = true;

        [ObservableProperty]
        private bool _showPropsLayer = true;

        [ObservableProperty]
        private bool _showEffectsLayer = true;

        [ObservableProperty]
        private bool _showRoofLayer = true;

        #endregion

        #region Properties - Edit Mode

        [ObservableProperty]
        private EditorTool _currentTool = EditorTool.Brush;

        [ObservableProperty]
        private int _currentRotation = 0;

        [ObservableProperty]
        private bool _currentFlipHorizontal = false;

        [ObservableProperty]
        private bool _currentFlipVertical = false;

        [ObservableProperty]
        private bool _showDMView = true;

        #endregion

        #region Properties - Status

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private string _cursorPosition = "";

        [ObservableProperty]
        private double _zoomLevel = 1.0;

        #endregion

        #region Events

        /// <summary>
        /// Raised when the map needs to be re-rendered
        /// </summary>
        public event Action MapChanged;

        /// <summary>
        /// Raised when a tile is placed
        /// </summary>
        public event Action<PlacedTile> TilePlaced;

        /// <summary>
        /// Raised when a tile is removed
        /// </summary>
        public event Action<PlacedTile> TileRemoved;

        #endregion

        #region Constructor

        public TileEditorViewModel()
        {
            _mapService = new TileMapService();
            _libraryService = TileLibraryService.Instance;

            TilePalette = new ObservableCollection<TileDefinition>();
            FilteredTilePalette = new ObservableCollection<TileDefinition>();

            // Initialize with a new map
            CreateNewMap(50, 50);
            LoadTilePalette();
        }

        #endregion

        #region Commands - File Operations

        [RelayCommand]
        private void NewMap()
        {
            CreateNewMap(MapWidth, MapHeight);
            HasUnsavedChanges = false;
            StatusText = "New map created";
        }

        [RelayCommand]
        private async Task OpenMapAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Tile Map"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var map = await _mapService.LoadMapAsync(dialog.FileName);
                    if (map != null)
                    {
                        CurrentMap = map;
                        _currentFilePath = dialog.FileName;
                        MapName = map.Name;
                        MapWidth = map.Width;
                        MapHeight = map.Height;
                        CellSize = map.CellSize;
                        ShowGrid = map.ShowGrid;
                        HasUnsavedChanges = false;
                        StatusText = $"Loaded: {map.Name}";
                        MapChanged?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TileEditorVM] Load error: {ex.Message}");
                    MessageBox.Show($"Failed to load map: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task SaveMapAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                await SaveMapAsAsync();
                return;
            }

            try
            {
                CurrentMap.Name = MapName;
                CurrentMap.ModifiedDate = DateTime.Now;
                await _mapService.SaveMapAsync(CurrentMap, _currentFilePath);
                HasUnsavedChanges = false;
                StatusText = "Map saved successfully";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileEditorVM] Save error: {ex.Message}");
                MessageBox.Show($"Failed to save map: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task SaveMapAsAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Tile Map As",
                FileName = $"{MapName}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                _currentFilePath = dialog.FileName;
                await SaveMapAsync();
            }
        }

        #endregion

        #region Commands - Tile Operations

        [RelayCommand]
        private void PlaceTile(Point gridPosition)
        {
            if (CurrentMap == null || SelectedTileDefinition == null) return;

            int gridX = (int)gridPosition.X;
            int gridY = (int)gridPosition.Y;

            // Validate bounds
            if (gridX < 0 || gridX >= CurrentMap.Width || gridY < 0 || gridY >= CurrentMap.Height)
                return;

            // Create new tile instance
            var newTile = new PlacedTile
            {
                Id = Guid.NewGuid(),
                TileDefinitionId = Guid.Parse(SelectedTileDefinition.Id),
                GridX = gridX,
                GridY = gridY,
                Rotation = CurrentRotation,
                FlipHorizontal = CurrentFlipHorizontal,
                FlipVertical = CurrentFlipVertical
            };

            // Remove existing tiles at this position on the same layer
            var existingTiles = CurrentMap.PlacedTiles
                .Where(t => t.GridX == gridX && t.GridY == gridY)
                .ToList();

            foreach (var existing in existingTiles)
            {
                var existingDef = _libraryService.GetTileById(existing.TileDefinitionId);
                if (existingDef?.Layer == SelectedTileDefinition.Layer)
                {
                    CurrentMap.PlacedTiles.Remove(existing);
                }
            }

            // Add the new tile
            CurrentMap.AddTile(newTile);
            HasUnsavedChanges = true;
            TilePlaced?.Invoke(newTile);

            Debug.WriteLine($"[TileEditorVM] Placed tile at ({gridX}, {gridY})");
        }

        [RelayCommand]
        private void EraseTile(Point gridPosition)
        {
            if (CurrentMap == null) return;

            int gridX = (int)gridPosition.X;
            int gridY = (int)gridPosition.Y;

            // Get all tiles at this position
            var tilesToRemove = CurrentMap.PlacedTiles
                .Where(t => t.GridX == gridX && t.GridY == gridY)
                .ToList();

            bool removedAny = false;
            foreach (var tile in tilesToRemove)
            {
                var tileDef = _libraryService.GetTileById(tile.TileDefinitionId);
                
                // Only erase tiles on the currently active layer
                // This ensures users don't accidentally erase tiles from other layers
                if (ShouldEraseTileOnLayer(tileDef?.Layer))
                {
                    CurrentMap.PlacedTiles.Remove(tile);
                    TileRemoved?.Invoke(tile);
                    removedAny = true;
                }
            }

            if (removedAny)
            {
                HasUnsavedChanges = true;
            }

            Debug.WriteLine($"[TileEditorVM] Erased tiles at ({gridX}, {gridY})");
        }

        /// <summary>
        /// Determines if a tile on the given layer should be erased based on the active layer.
        /// Tiles are only erased if they match the currently active layer.
        /// </summary>
        /// <param name="tileLayer">The layer of the tile to check</param>
        /// <returns>True if the tile should be erased</returns>
        private bool ShouldEraseTileOnLayer(TileLayer? tileLayer)
        {
            // If we couldn't determine the tile's layer, don't erase it
            if (!tileLayer.HasValue) return false;

            // Only erase tiles that match the active layer
            return tileLayer.Value == ActiveLayer;
        }

        [RelayCommand]
        private void ClearAllTiles()
        {
            if (CurrentMap == null) return;

            var result = MessageBox.Show(
                "Are you sure you want to clear all tiles?", 
                "Confirm Clear", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                CurrentMap.PlacedTiles.Clear();
                HasUnsavedChanges = true;
                MapChanged?.Invoke();
                StatusText = "All tiles cleared";
            }
        }

        #endregion

        #region Commands - Tools

        [RelayCommand]
        private void SetTool(EditorTool tool)
        {
            CurrentTool = tool;
            StatusText = $"Tool: {tool}";
        }

        [RelayCommand]
        private void RotateLeft()
        {
            CurrentRotation = (CurrentRotation - 90 + 360) % 360;
        }

        [RelayCommand]
        private void RotateRight()
        {
            CurrentRotation = (CurrentRotation + 90) % 360;
        }

        [RelayCommand]
        private void ToggleFlipHorizontal()
        {
            CurrentFlipHorizontal = !CurrentFlipHorizontal;
        }

        [RelayCommand]
        private void ToggleFlipVertical()
        {
            CurrentFlipVertical = !CurrentFlipVertical;
        }

        [RelayCommand]
        private void ResetTransform()
        {
            CurrentRotation = 0;
            CurrentFlipHorizontal = false;
            CurrentFlipVertical = false;
        }

        #endregion

        #region Commands - Layer Management

        [RelayCommand]
        private void SetActiveLayer(TileLayer layer)
        {
            ActiveLayer = layer;
            ApplyPaletteFilter();
            StatusText = $"Active Layer: {layer.GetDisplayName()}";
        }

        [RelayCommand]
        private void ToggleLayerVisibility(TileLayer layer)
        {
            switch (layer)
            {
                case TileLayer.Floor:
                    ShowFloorLayer = !ShowFloorLayer;
                    break;
                case TileLayer.Terrain:
                    ShowTerrainLayer = !ShowTerrainLayer;
                    break;
                case TileLayer.Wall:
                    ShowWallLayer = !ShowWallLayer;
                    break;
                case TileLayer.Door:
                    ShowDoorLayer = !ShowDoorLayer;
                    break;
                case TileLayer.Furniture:
                    ShowFurnitureLayer = !ShowFurnitureLayer;
                    break;
                case TileLayer.Props:
                    ShowPropsLayer = !ShowPropsLayer;
                    break;
                case TileLayer.Effects:
                    ShowEffectsLayer = !ShowEffectsLayer;
                    break;
                case TileLayer.Roof:
                    ShowRoofLayer = !ShowRoofLayer;
                    break;
            }
            MapChanged?.Invoke();
        }

        #endregion

        #region Methods - Map Creation

        /// <summary>
        /// Creates a new empty map with specified dimensions
        /// </summary>
        public void CreateNewMap(int width, int height)
        {
            CurrentMap = new TileMap
            {
                Id = Guid.NewGuid(),
                Name = "New Map",
                Width = width,
                Height = height,
                CellSize = CellSize,
                ShowGrid = ShowGrid,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            MapName = "New Map";
            MapWidth = width;
            MapHeight = height;
            _currentFilePath = null;
            HasUnsavedChanges = false;

            MapChanged?.Invoke();
            Debug.WriteLine($"[TileEditorVM] Created new map: {width}×{height}");
        }

        /// <summary>
        /// Resizes the current map
        /// </summary>
        public void ResizeMap(int newWidth, int newHeight)
        {
            if (CurrentMap == null) return;

            // Remove tiles outside new bounds
            var tilesToRemove = CurrentMap.PlacedTiles
                .Where(t => t.GridX >= newWidth || t.GridY >= newHeight)
                .ToList();

            foreach (var tile in tilesToRemove)
            {
                CurrentMap.PlacedTiles.Remove(tile);
            }

            CurrentMap.Width = newWidth;
            CurrentMap.Height = newHeight;
            CurrentMap.ModifiedDate = DateTime.Now;

            MapWidth = newWidth;
            MapHeight = newHeight;
            HasUnsavedChanges = true;

            MapChanged?.Invoke();
            Debug.WriteLine($"[TileEditorVM] Resized map to: {newWidth}×{newHeight}");
        }

        #endregion

        #region Methods - Tile Palette

        /// <summary>
        /// Loads the tile palette from the library service
        /// </summary>
        public void LoadTilePalette()
        {
            try
            {
                _libraryService.LoadTileLibrary();

                TilePalette.Clear();
                foreach (var tile in _libraryService.AvailableTiles)
                {
                    TilePalette.Add(tile);
                }

                ApplyPaletteFilter();
                StatusText = $"Loaded {TilePalette.Count} tiles";
                Debug.WriteLine($"[TileEditorVM] Loaded {TilePalette.Count} tiles into palette");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileEditorVM] Error loading palette: {ex.Message}");
                StatusText = "Error loading tile palette";
            }
        }

        /// <summary>
        /// Refreshes the tile palette from disk
        /// </summary>
        public void RefreshTilePalette()
        {
            _libraryService.RefreshLibrary();
            LoadTilePalette();
        }

        /// <summary>
        /// Applies search and category filters to the palette
        /// </summary>
        private void ApplyPaletteFilter()
        {
            FilteredTilePalette.Clear();

            var filtered = TilePalette.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                filtered = filtered.Where(t =>
                    (t.DisplayName ?? t.Id).Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    (t.Category ?? "").Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(CategoryFilter) && CategoryFilter != "All")
            {
                filtered = filtered.Where(t => 
                    (t.Category ?? "General").Equals(CategoryFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var tile in filtered.OrderBy(t => t.Category).ThenBy(t => t.DisplayName))
            {
                FilteredTilePalette.Add(tile);
            }
        }

        /// <summary>
        /// Called when search filter changes
        /// </summary>
        partial void OnSearchFilterChanged(string value)
        {
            ApplyPaletteFilter();
        }

        /// <summary>
        /// Called when category filter changes
        /// </summary>
        partial void OnCategoryFilterChanged(string value)
        {
            ApplyPaletteFilter();
        }

        #endregion

        #region Methods - Layer Visibility

        /// <summary>
        /// Gets the set of currently visible layers
        /// </summary>
        public HashSet<TileLayer> GetVisibleLayers()
        {
            var visible = new HashSet<TileLayer>();

            if (ShowFloorLayer) visible.Add(TileLayer.Floor);
            if (ShowTerrainLayer) visible.Add(TileLayer.Terrain);
            if (ShowWallLayer) visible.Add(TileLayer.Wall);
            if (ShowDoorLayer) visible.Add(TileLayer.Door);
            if (ShowFurnitureLayer) visible.Add(TileLayer.Furniture);
            if (ShowPropsLayer) visible.Add(TileLayer.Props);
            if (ShowEffectsLayer) visible.Add(TileLayer.Effects);
            if (ShowRoofLayer) visible.Add(TileLayer.Roof);

            return visible;
        }

        /// <summary>
        /// Checks if a specific layer is visible
        /// </summary>
        public bool IsLayerVisible(TileLayer layer)
        {
            return layer switch
            {
                TileLayer.Floor => ShowFloorLayer,
                TileLayer.Terrain => ShowTerrainLayer,
                TileLayer.Wall => ShowWallLayer,
                TileLayer.Door => ShowDoorLayer,
                TileLayer.Furniture => ShowFurnitureLayer,
                TileLayer.Props => ShowPropsLayer,
                TileLayer.Effects => ShowEffectsLayer,
                TileLayer.Roof => ShowRoofLayer,
                _ => true
            };
        }

        #endregion

        #region Methods - Utility

        /// <summary>
        /// Gets tiles at a specific grid position
        /// </summary>
        public IEnumerable<PlacedTile> GetTilesAt(int gridX, int gridY)
        {
            return CurrentMap?.GetTilesAt(gridX, gridY) ?? Enumerable.Empty<PlacedTile>();
        }

        /// <summary>
        /// Gets the tile definition for a placed tile
        /// </summary>
        public TileDefinition GetTileDefinition(PlacedTile tile)
        {
            if (tile == null) return null;
            return _libraryService.GetTileById(tile.TileDefinitionId);
        }

        /// <summary>
        /// Updates the cursor position display
        /// </summary>
        public void UpdateCursorPosition(int gridX, int gridY)
        {
            CursorPosition = $"({gridX}, {gridY})";
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// Available editor tools
    /// </summary>
    public enum EditorTool
    {
        Brush,
        Eraser,
        Fill,
        Picker,
        Select,
        Properties
    }

    #endregion
}
