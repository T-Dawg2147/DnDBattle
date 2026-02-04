using DnDBattle.Models.Tiles;
using System;
using System.Threading.Tasks;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    /// <summary>
    /// Manages tile map loading and state
    /// </summary>
    public class BattleGridTileMapManager
    {
        #region Fields

        private TileMap _loadedTileMap;

        #endregion

        #region Events

        public event Action<TileMap> TileMapLoaded;
        public event Action<string, string> LogMessage;

        #endregion

        #region Properties

        public TileMap LoadedTileMap => _loadedTileMap;

        #endregion

        #region Constructor

        public BattleGridTileMapManager()
        {
            System.Diagnostics.Debug.WriteLine("[TileMapManager] Initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a tile map asynchronously
        /// </summary>
        public async Task LoadTileMapAsync(TileMap tileMap, double cellSize)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TileMapManager] Loading tile map: {tileMap?.Name ?? "null"}");

                if (tileMap == null)
                {
                    throw new ArgumentNullException(nameof(tileMap));
                }

                // Validate tile map
                if (tileMap.Width <= 0 || tileMap.Height <= 0)
                {
                    throw new InvalidOperationException($"Invalid tile map dimensions: {tileMap.Width}×{tileMap.Height}");
                }

                // Store reference
                _loadedTileMap = tileMap;

                // Pre-load tile images in background (optional optimization)
                await PreloadTileImagesAsync(tileMap);

                System.Diagnostics.Debug.WriteLine($"[TileMapManager] Tile map loaded successfully");
                System.Diagnostics.Debug.WriteLine($"[TileMapManager] - Name: {tileMap.Name}");
                System.Diagnostics.Debug.WriteLine($"[TileMapManager] - Size: {tileMap.Width}×{tileMap.Height}");
                System.Diagnostics.Debug.WriteLine($"[TileMapManager] - Tiles: {tileMap.PlacedTiles?.Count ?? 0}");

                // Fire event
                TileMapLoaded?.Invoke(tileMap);
                LogMessage?.Invoke("TileMap", $"Loaded: {tileMap.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TileMapManager] Error loading tile map: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clears the currently loaded tile map
        /// </summary>
        public void ClearTileMap()
        {
            System.Diagnostics.Debug.WriteLine("[TileMapManager] Clearing tile map");

            _loadedTileMap = null;

            TileMapLoaded?.Invoke(null);
            LogMessage?.Invoke("TileMap", "Cleared");
        }

        /// <summary>
        /// Gets the tile at a specific grid position
        /// </summary>
        public Tile GetTileAt(int gridX, int gridY)
        {
            if (_loadedTileMap == null || _loadedTileMap.PlacedTiles == null)
                return null;

            // Find the topmost tile at this position
            Tile topTile = null;
            int highestZIndex = int.MinValue;

            foreach (var tile in _loadedTileMap.PlacedTiles)
            {
                if (tile.GridX == gridX && tile.GridY == gridY)
                {
                    int zIndex = tile.ZIndex ?? 0;
                    if (zIndex > highestZIndex)
                    {
                        highestZIndex = zIndex;
                        topTile = tile;
                    }
                }
            }

            return topTile;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Pre-loads all tile images in background to avoid stuttering during render
        /// </summary>
        private async Task PreloadTileImagesAsync(TileMap tileMap)
        {
            if (tileMap.PlacedTiles == null || tileMap.PlacedTiles.Count == 0)
                return;

            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[TileMapManager] Pre-loading tile images...");

                    var uniqueTileIds = new System.Collections.Generic.HashSet<string>();

                    foreach (var tile in tileMap.PlacedTiles)
                    {
                        uniqueTileIds.Add(tile.TileDefinitionId);
                    }

                    System.Diagnostics.Debug.WriteLine($"[TileMapManager] Found {uniqueTileIds.Count} unique tiles to load");

                    int loadedCount = 0;
                    foreach (var tileId in uniqueTileIds)
                    {
                        try
                        {
                            var tileDef = Services.TileService.TileLibraryService.Instance.GetTileById(tileId);
                            if (tileDef != null)
                            {
                                // Load image into cache
                                var image = Services.TileService.TileImageCacheService.Instance.GetOrLoadImage(tileDef.ImagePath);
                                if (image != null)
                                {
                                    loadedCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[TileMapManager] Failed to preload tile {tileId}: {ex.Message}");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[TileMapManager] Pre-loaded {loadedCount}/{uniqueTileIds.Count} tile images");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TileMapManager] Error during preload: {ex.Message}");
                }
            });
        }

        #endregion
    }
}