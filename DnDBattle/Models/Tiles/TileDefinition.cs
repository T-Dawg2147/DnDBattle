using System;
using System.ComponentModel;
using System.Windows.Media;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Defines a tile template with all its properties and behaviors.
    /// Tiles are placed on the map as instances referencing a TileDefinition.
    /// </summary>
    public class TileDefinition : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion

        #region Properties - Identity

        /// <summary>
        /// Unique identifier for this tile definition
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        private string _name;
        /// <summary>
        /// Internal name of the tile
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _displayName;
        /// <summary>
        /// Display name shown in the UI
        /// </summary>
        public string DisplayName
        {
            get => _displayName ?? _name;
            set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); }
        }

        private string _description;
        /// <summary>
        /// Description of the tile
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        private string _imagePath;
        /// <summary>
        /// Path to the tile image
        /// </summary>
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }

        /// <summary>
        /// Tags for categorization and filtering
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        private TileLayer _layer = TileLayer.Terrain;
        /// <summary>
        /// Layer determines rendering order and behavior
        /// </summary>
        public TileLayer Layer
        {
            get => _layer;
            set { _layer = value; OnPropertyChanged(nameof(Layer)); }
        }

        private string _category = "General";
        /// <summary>
        /// Category for grouping tiles in the palette
        /// </summary>
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        #endregion

        #region Properties - Collision & Vision

        /// <summary>
        /// Indicates if this is a wall tile (legacy property, use BlocksMovement/BlocksSight instead)
        /// </summary>
        public bool IsWall { get; set; }

        private bool _blocksMovement = false;
        /// <summary>
        /// Whether this tile blocks creature movement
        /// </summary>
        public bool BlocksMovement
        {
            get => _blocksMovement;
            set { _blocksMovement = value; OnPropertyChanged(nameof(BlocksMovement)); }
        }

        private bool _blocksVision = false;
        /// <summary>
        /// Whether this tile blocks vision (alias for BlocksSight)
        /// </summary>
        public bool BlocksVision
        {
            get => _blocksVision;
            set 
            { 
                _blocksVision = value; 
                OnPropertyChanged(nameof(BlocksVision));
                OnPropertyChanged(nameof(BlocksSight));
            }
        }

        /// <summary>
        /// Whether this tile blocks line of sight (for fog of war calculations)
        /// </summary>
        public bool BlocksSight
        {
            get => _blocksVision;
            set 
            { 
                _blocksVision = value; 
                OnPropertyChanged(nameof(BlocksSight));
                OnPropertyChanged(nameof(BlocksVision));
            }
        }

        /// <summary>
        /// Whether this tile blocks light propagation (for lighting calculations)
        /// </summary>
        public bool BlocksLight { get; set; } = false;

        #endregion

        #region Properties - Door

        private bool _isDoor = false;
        /// <summary>
        /// Indicates if this tile represents a door that can be opened/closed
        /// </summary>
        public bool IsDoor
        {
            get => _isDoor;
            set { _isDoor = value; OnPropertyChanged(nameof(IsDoor)); }
        }

        private string _openImagePath;
        /// <summary>
        /// Path to the image used when the door is open
        /// </summary>
        public string OpenImagePath
        {
            get => _openImagePath;
            set { _openImagePath = value; OnPropertyChanged(nameof(OpenImagePath)); }
        }

        /// <summary>
        /// Whether the door is locked by default
        /// </summary>
        public bool IsLockedByDefault { get; set; } = false;

        /// <summary>
        /// DC required to pick the lock (0 = no lock or unpickable)
        /// </summary>
        public int LockDC { get; set; } = 0;

        /// <summary>
        /// DC required to break down the door (0 = cannot be broken)
        /// </summary>
        public int BreakDC { get; set; } = 0;

        #endregion

        #region Properties - Interactive

        private bool _isInteractive = false;
        /// <summary>
        /// Indicates if this tile can be interacted with (chests, levers, switches)
        /// </summary>
        public bool IsInteractive
        {
            get => _isInteractive;
            set { _isInteractive = value; OnPropertyChanged(nameof(IsInteractive)); }
        }

        private string _interactionType;
        /// <summary>
        /// Type of interaction: "OpenClose", "Loot", "Lever", "Button", "Trap", "Custom"
        /// </summary>
        public string InteractionType
        {
            get => _interactionType;
            set { _interactionType = value; OnPropertyChanged(nameof(InteractionType)); }
        }

        private string _interactionData;
        /// <summary>
        /// JSON payload or description of what happens when interacted with
        /// </summary>
        public string InteractionData
        {
            get => _interactionData;
            set { _interactionData = value; OnPropertyChanged(nameof(InteractionData)); }
        }

        #endregion

        #region Properties - Furniture & Cover

        private bool _providesCover = false;
        /// <summary>
        /// Whether this tile provides cover in combat
        /// </summary>
        public bool ProvidesCover
        {
            get => _providesCover;
            set { _providesCover = value; OnPropertyChanged(nameof(ProvidesCover)); }
        }

        /// <summary>
        /// Type of cover provided: None, Half, ThreeQuarters, Full
        /// </summary>
        public CoverType CoverType { get; set; } = CoverType.None;

        #endregion

        #region Properties - Visual

        /// <summary>
        /// Optional tint color to apply to the tile
        /// </summary>
        public Color? TintColor { get; set; } = null;

        /// <summary>
        /// Z-index for rendering order within the same layer
        /// </summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>
        /// Whether this tile is enabled and can be placed
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Opacity of the tile (0.0 to 1.0)
        /// </summary>
        public double Opacity { get; set; } = 1.0;

        #endregion

        #region Properties - Terrain Effects

        /// <summary>
        /// Indicates if this tile is difficult terrain (costs double movement)
        /// </summary>
        public bool IsDifficultTerrain { get; set; } = false;

        /// <summary>
        /// Movement cost multiplier (1.0 = normal, 2.0 = difficult terrain)
        /// </summary>
        public double MovementCostMultiplier { get; set; } = 1.0;

        /// <summary>
        /// Indicates if this tile deals damage when entered (lava, acid, etc.)
        /// </summary>
        public bool DealsDamageOnEnter { get; set; } = false;

        /// <summary>
        /// Damage dice expression when entering (e.g., "2d6")
        /// </summary>
        public string EnterDamageDice { get; set; }

        /// <summary>
        /// Damage type for entering damage (Fire, Acid, etc.)
        /// </summary>
        public string EnterDamageType { get; set; }

        #endregion

        #region Constructor

        public TileDefinition()
        {
            Id = Guid.NewGuid().ToString();
            Tags = Array.Empty<string>();
            IsWall = false;
            BlocksMovement = false;
            BlocksSight = false;
            BlocksLight = false;
            IsDoor = false;
            IsInteractive = false;
            ProvidesCover = false;
            CoverType = CoverType.None;
            IsDifficultTerrain = false;
            MovementCostMultiplier = 1.0;
            Opacity = 1.0;
        }

        #endregion

        #region Methods

        public override string ToString() => DisplayName ?? Name ?? ImagePath ?? "Unnamed Tile";

        /// <summary>
        /// Creates a deep copy of this tile definition
        /// </summary>
        public TileDefinition Clone()
        {
            return new TileDefinition
            {
                Id = Guid.NewGuid().ToString(), // New ID for clone
                ImagePath = this.ImagePath,
                Name = this.Name,
                DisplayName = this.DisplayName,
                Description = this.Description,
                Tags = (string[])this.Tags?.Clone() ?? Array.Empty<string>(),
                Category = this.Category,
                Layer = this.Layer,
                IsWall = this.IsWall,
                BlocksMovement = this.BlocksMovement,
                BlocksSight = this.BlocksSight,
                BlocksLight = this.BlocksLight,
                IsDoor = this.IsDoor,
                OpenImagePath = this.OpenImagePath,
                IsLockedByDefault = this.IsLockedByDefault,
                LockDC = this.LockDC,
                BreakDC = this.BreakDC,
                IsInteractive = this.IsInteractive,
                InteractionType = this.InteractionType,
                InteractionData = this.InteractionData,
                ProvidesCover = this.ProvidesCover,
                CoverType = this.CoverType,
                TintColor = this.TintColor,
                ZIndex = this.ZIndex,
                IsEnabled = this.IsEnabled,
                Opacity = this.Opacity,
                IsDifficultTerrain = this.IsDifficultTerrain,
                MovementCostMultiplier = this.MovementCostMultiplier,
                DealsDamageOnEnter = this.DealsDamageOnEnter,
                EnterDamageDice = this.EnterDamageDice,
                EnterDamageType = this.EnterDamageType
            };
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// Types of cover provided by furniture and obstacles
    /// </summary>
    public enum CoverType
    {
        /// <summary>No cover provided</summary>
        None = 0,

        /// <summary>Half cover (+2 AC and Dexterity saving throws)</summary>
        Half = 1,

        /// <summary>Three-quarters cover (+5 AC and Dexterity saving throws)</summary>
        ThreeQuarters = 2,

        /// <summary>Full cover (cannot be targeted directly)</summary>
        Full = 3
    }

    #endregion
}
