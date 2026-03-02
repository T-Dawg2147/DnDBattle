using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Tile Map loading and rendering functionality
    /// </summary>
    public partial class BattleGridControl
    {
        #region Tile Map Loading

        /// <summary>
        /// Load a tile map as the battle grid background
        /// </summary>
        // VISUAL REFRESH
        public void LoadTileMap(TileMap tileMap)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] ===== LoadTileMap START =====");

                _loadedTileMap = tileMap;

                if (tileMap != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Loading tile map: {tileMap.Name}");
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Map size: {tileMap.Width}×{tileMap.Height}");
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Tile count: {tileMap.PlacedTiles?.Count ?? 0}");

                    // Step 1: Adjust grid size
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 1: Adjust grid size...");
                    SetGridMaxSize(tileMap.Width, tileMap.Height);
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 1: DONE");

                    // Step 2: Render tile map
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 2: Render tile map...");
                    RenderTileMapToVisual();
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 2: DONE");

                    // Step 3: Update grid visual
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 3: Update grid visual...");
                    UpdateGridVisual();
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 3: DONE");

                    // Step 4: Rebuild token visuals
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 4: Rebuild token visuals...");
                    RebuildTokenVisuals();
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 4: DONE");

                    // Step 5: Log success
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 5: Add to action log...");
                    AddToActionLog("Map", $"✅ Loaded tile map: {tileMap.Name} ({tileMap.Width}×{tileMap.Height}, {tileMap.PlacedTiles?.Count ?? 0} tiles)");
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Step 5: DONE");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[BattleGrid] Clearing tile map");

                    // Clear tile map visual
                    using (var dc = _tileMapVisual.RenderOpen())
                    {
                        // Empty - clears the visual
                    }

                    AddToActionLog("Map", "Tile map cleared");
                }

                // Force redraw
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Calling InvalidateVisual...");
                InvalidateVisual();
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] ===== LoadTileMap END =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] ERROR in LoadTileMap: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Stack trace: {ex.StackTrace}");

                MessageBox.Show(
                    $"Error loading tile map:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Tile Map Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Auto Spawn

        /// <summary>
        /// Automatically spawns tokens from spawn points marked "SpawnOnMapLoad"
        /// </summary>
        private void AutoSpawnMapLoadTokens()
        {
            if (_loadedTileMap == null) return;

            var loadSpawns = new List<SpawnMetadata>();

            // Find all spawn points marked for map load
            foreach (var tile in _loadedTileMap.PlacedTiles)
            {
                var spawns = tile.GetMetadata(TileMetadataType.Spawn)
                    .OfType<SpawnMetadata>()
                    .Where(s => s.SpawnOnMapLoad && !s.HasSpawned)
                    .ToList();

                loadSpawns.AddRange(spawns);
            }

            if (loadSpawns.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[BattleGrid] No spawn-on-load tokens found");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[BattleGrid] Auto-spawning {loadSpawns.Count} spawn points...");

            // Activate each spawn point
            foreach (var spawn in loadSpawns)
            {
                ActivateSpawnPoint(spawn);
            }

            AddToActionLog("Spawn", $"✅ Auto-spawned {loadSpawns.Count} enemy groups from map");
        }

        #endregion

        #region Tile Map Rendering

        /// <summary>
        /// Render the tile map to the background visual layer
        /// </summary>
        // VISUAL REFRESH
        private void RenderTileMapToVisual()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] RenderTileMapToVisual START");

                if (_loadedTileMap == null)
                {
                    System.Diagnostics.Debug.WriteLine("[BattleGrid] No tile map loaded, exiting");
                    return;
                }

                if (_loadedTileMap.PlacedTiles == null)
                {
                    System.Diagnostics.Debug.WriteLine("[BattleGrid] PlacedTiles is null!");
                    _loadedTileMap.PlacedTiles = new System.Collections.ObjectModel.ObservableCollection<Models.Tiles.Tile>();
                    return;
                }

                // Ensure tile library is loaded before rendering
                if (TileLibraryService.Instance.AvailableTiles.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[BattleGrid] Tile library empty, loading...");
                    TileLibraryService.Instance.LoadTileLibrary();
                }

                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Rendering {_loadedTileMap.PlacedTiles.Count} tiles...");

                using (var dc = _tileMapVisual.RenderOpen())
                {
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Drawing context opened");

                    // Draw background color
                    var bgColor = (Color)ColorConverter.ConvertFromString(_loadedTileMap.BackgroundColor ?? "#FF1A1A1A");
                    var bgBrush = new SolidColorBrush(bgColor);
                    bgBrush.Freeze();

                    double mapWidth = _loadedTileMap.Width * GridCellSize;
                    double mapHeight = _loadedTileMap.Height * GridCellSize;

                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Drawing background: {mapWidth}×{mapHeight}");

                    dc.DrawRectangle(bgBrush, null, new Rect(0, 0, mapWidth, mapHeight));

                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Background drawn, now drawing tiles...");

                    // Draw all tiles
                    int drawnCount = 0;
                    foreach (var tile in _loadedTileMap.PlacedTiles.OrderBy(t => t.ZIndex ?? 0))
                    {
                        try
                        {
                            DrawTileToVisual(dc, tile);
                            drawnCount++;

                            // Log every 10 tiles
                            if (drawnCount % 10 == 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Drew {drawnCount} tiles so far...");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[BattleGrid] Error drawing tile at ({tile.GridX},{tile.GridY}): {ex.Message}");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Drew {drawnCount} tiles total");
                }

                System.Diagnostics.Debug.WriteLine($"[BattleGrid] RenderTileMapToVisual END");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] ERROR in RenderTileMapToVisual: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Draw a single tile to the drawing context
        /// </summary>
        // VISUAL REFRESH
        private void DrawTileToVisual(DrawingContext dc, Tile tile)
        {
            try
            {
                var tileDef = TileLibraryService.Instance.GetTileById(tile.TileDefinitionId);
                if (tileDef == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Tile definition not found: {tile.TileDefinitionId}");
                    return;
                }

                var image = TileImageCacheService.Instance.GetOrLoadImage(tileDef.ImagePath);
                if (image == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Image not found: {tileDef.ImagePath}");
                    return;
                }

                double x = tile.GridX * GridCellSize;
                double y = tile.GridY * GridCellSize;
                double size = GridCellSize;

                var rect = new Rect(x, y, size, size);

                // Apply transformations if needed
                if (tile.Rotation != 0 || tile.FlipHorizontal || tile.FlipVertical)
                {
                    dc.PushTransform(new TranslateTransform(x + size / 2, y + size / 2));

                    if (tile.Rotation != 0)
                    {
                        dc.PushTransform(new RotateTransform(tile.Rotation));
                    }

                    if (tile.FlipHorizontal || tile.FlipVertical)
                    {
                        dc.PushTransform(new ScaleTransform(
                            tile.FlipHorizontal ? -1 : 1,
                            tile.FlipVertical ? -1 : 1));
                    }

                    dc.DrawImage(image, new Rect(-size / 2, -size / 2, size, size));

                    if (tile.FlipHorizontal || tile.FlipVertical)
                        dc.Pop();
                    if (tile.Rotation != 0)
                        dc.Pop();
                    dc.Pop();
                }
                else
                {
                    dc.DrawImage(image, rect);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Error in DrawTileToVisual: {ex.Message}");
                // Don't rethrow - just skip this tile
            }
        }

        /// <summary>
        /// Get the tile at a specific grid position
        /// </summary>
        public Tile GetTileAt(int gridX, int gridY)
        {
            if (_loadedTileMap == null) return null;
            return _loadedTileMap.GetTilesAt(gridX, gridY).OrderByDescending(t => t.ZIndex ?? 0).FirstOrDefault();
        }

        #endregion
    }
}
