using System;
using System.ComponentModel;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Represents a tile instance placed on the map
    /// FUTURE-PROOF: Handles ALL tile types across all layers!
    /// </summary>
    public class PlacedTile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; } = Guid.NewGuid();

        #region Position & Layer

        private int _gridX;
        public int GridX
        {
            get => _gridX;
            set { _gridX = value; OnPropertyChanged(nameof(GridX)); }
        }

        private int _gridY;
        public int GridY
        {
            get => _gridY;
            set { _gridY = value; OnPropertyChanged(nameof(GridY)); }
        }

        /// <summary>
        /// Layer is determined by TileDefinition, but can be overridden
        /// </summary>
        public TileLayer Layer => TileDefinition?.Layer ?? TileLayer.Terrain;

        #endregion

        #region Tile Reference

        private Guid _tileDefinitionId;
        /// <summary>
        /// Reference to the TileDefinition template
        /// </summary>
        public Guid TileDefinitionId
        {
            get => _tileDefinitionId;
            set { _tileDefinitionId = value; OnPropertyChanged(nameof(TileDefinitionId)); }
        }

        // Runtime reference (not serialized)
        private TileDefinition _tileDefinition;
        public TileDefinition TileDefinition
        {
            get => _tileDefinition;
            set
            {
                _tileDefinition = value;
                OnPropertyChanged(nameof(TileDefinition));
                OnPropertyChanged(nameof(Layer));
            }
        }

        #endregion

        #region State Management (for interactive tiles)

        // For doors
        private bool _isOpen = false;
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen != value)
                {
                    _isOpen = value;
                    OnPropertyChanged(nameof(IsOpen));
                    OnPropertyChanged(nameof(BlocksMovement));
                    OnPropertyChanged(nameof(BlocksVision));
                }
            }
        }

        // For treasure/containers
        private bool _isLooted = false;
        public bool IsLooted
        {
            get => _isLooted;
            set
            {
                _isLooted = value;
                OnPropertyChanged(nameof(IsLooted));
            }
        }

        // For levers/switches
        private bool _isActivated = false;
        public bool IsActivated
        {
            get => _isActivated;
            set
            {
                _isActivated = value;
                OnPropertyChanged(nameof(IsActivated));
            }
        }

        // Generic state for custom interactions
        private string _customState;
        public string CustomState
        {
            get => _customState;
            set
            {
                _customState = value;
                OnPropertyChanged(nameof(CustomState));
            }
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Does this tile block movement? (respects door state)
        /// </summary>
        public bool BlocksMovement
        {
            get
            {
                if (TileDefinition == null) return false;
                if (TileDefinition.IsDoor)
                    return !IsOpen; // Open doors don't block
                return TileDefinition.BlocksMovement;
            }
        }

        /// <summary>
        /// Does this tile block vision? (respects door state)
        /// </summary>
        public bool BlocksVision
        {
            get
            {
                if (TileDefinition == null) return false;
                if (TileDefinition.IsDoor)
                    return !IsOpen; // Open doors don't block vision
                return TileDefinition.BlocksVision;
            }
        }

        /// <summary>
        /// Can this tile be interacted with?
        /// </summary>
        public bool IsInteractive =>
            TileDefinition?.IsInteractive == true ||
            TileDefinition?.IsDoor == true;

        #endregion
    }
}