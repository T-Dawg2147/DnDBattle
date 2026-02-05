using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Represents a complete tile-based map
    /// FUTURE-PROOF: Works with ANY number of layers!
    /// </summary>
    public class TileMap : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; } = Guid.NewGuid();

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private int _width = 50;
        public int Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(nameof(Width)); }
        }

        private int _height = 50;
        public int Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(nameof(Height)); }
        }

        private double _cellSize = 48.0;
        public double CellSize
        {
            get => _cellSize;
            set { _cellSize = value; OnPropertyChanged(nameof(CellSize)); }
        }

        private bool _showGrid = true;
        /// <summary>
        /// Whether to display the grid overlay
        /// </summary>
        public bool ShowGrid
        {
            get => _showGrid;
            set { _showGrid = value; OnPropertyChanged(nameof(ShowGrid)); }
        }

        private Color _backgroundColor = Colors.DarkSlateGray;
        /// <summary>
        /// Background color of the map
        /// </summary>
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set { _backgroundColor = value; OnPropertyChanged(nameof(BackgroundColor)); }
        }

        // All tile definitions available in this map
        public List<TileDefinition> TileDefinitions { get; set; } = new List<TileDefinition>();

        // All placed tile instances (ALL layers mixed together)
        public List<PlacedTile> PlacedTiles { get; set; } = new List<PlacedTile>();

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        #region Helper Methods - Tile Management

        /// <summary>
        /// Adds a tile to the map
        /// </summary>
        public void AddTile(PlacedTile tile)
        {
            if (tile != null && !PlacedTiles.Contains(tile))
            {
                PlacedTiles.Add(tile);
                ModifiedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Adds a tile to the map (overload for Tile type compatibility)
        /// </summary>
        public void AddTile(Tile tile)
        {
            if (tile != null)
            {
                var placedTile = ConvertToPlacedTile(tile);
                AddTile(placedTile);
            }
        }

        /// <summary>
        /// Removes a tile from the map
        /// </summary>
        public void RemoveTile(PlacedTile tile)
        {
            if (tile != null)
            {
                PlacedTiles.Remove(tile);
                ModifiedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Removes a tile from the map (overload for Tile type compatibility)
        /// </summary>
        public void RemoveTile(Tile tile)
        {
            if (tile != null)
            {
                var toRemove = PlacedTiles.FirstOrDefault(t => t.Id == tile.InstanceId);
                if (toRemove != null)
                {
                    RemoveTile(toRemove);
                }
            }
        }

        /// <summary>
        /// Converts a Tile to a PlacedTile
        /// </summary>
        private PlacedTile ConvertToPlacedTile(Tile tile)
        {
            return new PlacedTile
            {
                Id = tile.InstanceId,
                TileDefinitionId = Guid.TryParse(tile.TileDefinitionId, out var guid) ? guid : Guid.Empty,
                GridX = tile.GridX,
                GridY = tile.GridY,
                Rotation = tile.Rotation,
                FlipHorizontal = tile.FlipHorizontal,
                FlipVertical = tile.FlipVertical,
                ZIndex = tile.ZIndex,
                Notes = tile.Notes,
                Metadata = tile.Metadata
            };
        }

        #endregion

        #region Helper Methods - General

        /// <summary>
        /// Get all tiles at a specific grid position
        /// </summary>
        public IEnumerable<PlacedTile> GetTilesAt(int gridX, int gridY)
        {
            return PlacedTiles.Where(t => t.GridX == gridX && t.GridY == gridY);
        }

        /// <summary>
        /// Get all tiles at a position on a specific layer
        /// </summary>
        public IEnumerable<PlacedTile> GetTilesAt(int gridX, int gridY, TileLayer layer)
        {
            return PlacedTiles.Where(t => t.GridX == gridX && t.GridY == gridY && t.Layer == layer);
        }

        /// <summary>
        /// Get all tiles on a specific layer
        /// </summary>
        public IEnumerable<PlacedTile> GetTilesOnLayer(TileLayer layer)
        {
            return PlacedTiles.Where(t => t.Layer == layer);
        }

        #endregion

        #region Helper Methods - Collision & Vision

        /// <summary>
        /// Check if movement is blocked at a position (ANY layer)
        /// </summary>
        public bool IsMovementBlocked(int gridX, int gridY)
        {
            return GetTilesAt(gridX, gridY).Any(t => t.BlocksMovement);
        }

        /// <summary>
        /// Check if vision is blocked at a position (ANY layer)
        /// </summary>
        public bool IsVisionBlocked(int gridX, int gridY)
        {
            return GetTilesAt(gridX, gridY).Any(t => t.BlocksVision);
        }

        #endregion

        #region Helper Methods - Specific Tile Types

        /// <summary>
        /// Find a door tile at a position
        /// </summary>
        public PlacedTile GetDoorAt(int gridX, int gridY)
        {
            return GetTilesAt(gridX, gridY)
                .FirstOrDefault(t => t.TileDefinition?.IsDoor == true);
        }

        /// <summary>
        /// Find any interactive tile at a position
        /// </summary>
        public PlacedTile GetInteractiveTileAt(int gridX, int gridY)
        {
            return GetTilesAt(gridX, gridY)
                .FirstOrDefault(t => t.IsInteractive);
        }

        /// <summary>
        /// Get all furniture at a position (for cover calculation)
        /// </summary>
        public IEnumerable<PlacedTile> GetFurnitureAt(int gridX, int gridY)
        {
            return GetTilesAt(gridX, gridY, TileLayer.Furniture);
        }

        /// <summary>
        /// Check if a position provides cover
        /// </summary>
        public CoverType GetCoverAt(int gridX, int gridY)
        {
            var furniture = GetFurnitureAt(gridX, gridY)
                .Where(t => t.TileDefinition?.ProvidesCover == true)
                .Select(t => t.TileDefinition.CoverType)
                .OrderByDescending(c => c)
                .FirstOrDefault();

            return furniture;
        }

        #endregion

        #region Helper Methods - Rendering

        /// <summary>
        /// Get tiles sorted by render order (bottom to top)
        /// </summary>
        public IEnumerable<PlacedTile> GetTilesByRenderOrder()
        {
            return PlacedTiles.OrderBy(t => t.Layer);
        }

        /// <summary>
        /// Get tiles in a specific area sorted by render order
        /// </summary>
        public IEnumerable<PlacedTile> GetTilesInArea(int minX, int minY, int maxX, int maxY)
        {
            return PlacedTiles
                .Where(t => t.GridX >= minX && t.GridX <= maxX && t.GridY >= minY && t.GridY <= maxY)
                .OrderBy(t => t.Layer);
        }

        #endregion
    }
}