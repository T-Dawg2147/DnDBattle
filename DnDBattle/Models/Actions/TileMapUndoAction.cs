using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Actions
{
    public class TilePlaceAction : IUndoabaleAction
    {
        private readonly TileMap _map;
        private readonly Tile _tile;

        public string Description => $"Place Tile at ({_tile.GridX}, {_tile.GridY})";

        public TilePlaceAction(TileMap map, Tile tile)
        {
            _map = map;
            _tile = tile;
        }

        public void Do()
        {
            if (!_map.PlacedTiles.Contains(_tile))
                _map.PlacedTiles.Add(_tile);
        }

        public void Undo()
        {
            _map.PlacedTiles.Remove(_tile);
        }
    }

    public class TileRemoveAction : IUndoabaleAction
    {
        private readonly TileMap _map;
        private readonly Tile _tile;

        public string Description => $"Remove Tile at ({_tile.GridX}, {_tile.GridY})";

        public TileRemoveAction(TileMap map, Tile tile)
        {
            _map = map;
            _tile = tile;
        }

        public void Do()
        {
            _map.PlacedTiles.Remove(_tile);
        }

        public void Undo()
        {
            if (!_map.PlacedTiles.Contains(_tile))
                _map.PlacedTiles.Add(_tile);
        }        
    }

    public class TileBatchAction : IUndoabaleAction
    {
        private readonly TileMap _map;
        private readonly List<Tile> _tilesAdded;
        private readonly List<Tile> _tilesRemoved;

        public string Description { get; }
        
        public TileBatchAction(TileMap map, List<Tile> tilesAdded, List<Tile> tilesRemoved, string description = "Batch Edit")
        {
            _map = map;
            _tilesAdded = tilesAdded ?? new List<Tile>();
            _tilesRemoved = tilesRemoved ?? new List<Tile>();
            Description = description;
        }

        public void Do()
        {
            foreach (var tile in _tilesRemoved)
            {
                _map.PlacedTiles.Remove(tile);
            }

            foreach (var tile in _tilesAdded)
            {
                if (!_map.PlacedTiles.Contains(tile))
                {
                    _map.PlacedTiles.Add(tile);
                }
            }
        }

        public void Undo()
        {
            foreach (var tile in _tilesAdded)
            {
                _map.PlacedTiles.Remove(tile);
            }

            foreach (var tile in _tilesRemoved)
            {
                if (!_map.PlacedTiles.Contains(tile))
                {
                    _map.PlacedTiles.Add(tile);
                }
            }
        }
    }

    /// <summary>
    /// Undo action for tile metadata changes
    /// </summary>
    public class TileMetadataAction : IUndoabaleAction
    {
        private readonly Tile _tile;
        private readonly TileMetadata _metadata;
        private readonly bool _isAdd;

        public string Description => _isAdd ? $"Add {_metadata.Type}" : $"Remove {_metadata.Type}";

        public TileMetadataAction(Tile tile, TileMetadata metadata, bool isAdd)
        {
            _tile = tile;
            _metadata = metadata;
            _isAdd = isAdd;
        }

        public void Do()
        {
            if (_isAdd)
            {
                if (!_tile.Metadata.Contains(_metadata))
                {
                    _tile.Metadata.Add(_metadata);
                }
            }
            else
            {
                _tile.Metadata.Remove(_metadata);
            }
        }

        public void Undo()
        {
            if (_isAdd)
            {
                _tile.Metadata.Remove(_metadata);
            }
            else
            {
                if (!_tile.Metadata.Contains(_metadata))
                {
                    _tile.Metadata.Add(_metadata);
                }
            }
        }
    }

    /// <summary>
    /// Undo action for map property changes (size, name, etc.)
    /// </summary>
    public class MapPropertyChangeAction : IUndoabaleAction
    {
        private readonly TileMap _map;
        private readonly string _propertyName;
        private readonly object _oldValue;
        private readonly object _newValue;

        public string Description => $"Change {_propertyName}";

        public MapPropertyChangeAction(TileMap map, string propertyName, object oldValue, object newValue)
        {
            _map = map;
            _propertyName = propertyName;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Do()
        {
            SetPropertyValue(_newValue);
        }

        public void Undo()
        {
            SetPropertyValue(_oldValue);
        }

        private void SetPropertyValue(object value)
        {
            var prop = _map.GetType().GetProperty(_propertyName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(_map, value);
            }
        }
    }
}
