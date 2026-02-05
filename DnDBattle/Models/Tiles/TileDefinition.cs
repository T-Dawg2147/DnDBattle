using System;
using System.ComponentModel;
using System.Windows.Media;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Defines a reusable tile type (template)
    /// FUTURE-PROOF: Works for ALL layers!
    /// </summary>
    public class TileDefinition : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Id { get; set; } = Guid.NewGuid().ToString();

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }

        // NEW: Layer determines rendering order and behavior ✨
        private TileLayer _layer = TileLayer.Terrain;
        public TileLayer Layer
        {
            get => _layer;
            set { _layer = value; OnPropertyChanged(nameof(Layer)); }
        }

        private TileCategory _category = TileCategory.Floor;
        public TileCategory Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        #region Collision & Vision Properties

        private bool _blocksMovement = false;
        public bool BlocksMovement
        {
            get => _blocksMovement;
            set { _blocksMovement = value; OnPropertyChanged(nameof(BlocksMovement)); }
        }

        private bool _blocksVision = false;
        public bool BlocksVision
        {
            get => _blocksVision;
            set { _blocksVision = value; OnPropertyChanged(nameof(BlocksVision)); }
        }

        #endregion

        #region Door Properties (for Walls layer)

        private bool _isDoor = false;
        public bool IsDoor
        {
            get => _isDoor;
            set { _isDoor = value; OnPropertyChanged(nameof(IsDoor)); }
        }

        private string _openImagePath;
        /// <summary>
        /// Alternative image when door is open (optional)
        /// </summary>
        public string OpenImagePath
        {
            get => _openImagePath;
            set { _openImagePath = value; OnPropertyChanged(nameof(OpenImagePath)); }
        }

        #endregion

        #region Interactive Properties (for Interactables layer)

        private bool _isInteractive = false;
        /// <summary>
        /// Can be clicked/activated during gameplay
        /// </summary>
        public bool IsInteractive
        {
            get => _isInteractive;
            set { _isInteractive = value; OnPropertyChanged(nameof(IsInteractive)); }
        }

        private string _interactionType;
        /// <summary>
        /// Type of interaction: "OpenClose", "Loot", "Lever", "Button", etc.
        /// </summary>
        public string InteractionType
        {
            get => _interactionType;
            set { _interactionType = value; OnPropertyChanged(nameof(InteractionType)); }
        }

        private string _interactionData;
        /// <summary>
        /// JSON or string data for interaction (e.g., loot table ID, target door ID)
        /// </summary>
        public string InteractionData
        {
            get => _interactionData;
            set { _interactionData = value; OnPropertyChanged(nameof(InteractionData)); }
        }

        #endregion

        #region Furniture Properties

        private bool _provideCover = false;
        /// <summary>
        /// Provides half/three-quarters cover in combat
        /// </summary>
        public bool ProvidesCover
        {
            get => _provideCover;
            set { _provideCover = value; OnPropertyChanged(nameof(ProvidesCover)); }
        }

        private CoverType _coverType = CoverType.Half;
        public CoverType CoverType
        {
            get => _coverType;
            set { _coverType = value; OnPropertyChanged(nameof(CoverType)); }
        }

        #endregion

        #region General Properties

        private string _tags;
        /// <summary>
        /// Comma-separated tags: "wall", "door", "furniture", "treasure", "hazard"
        /// </summary>
        public string Tags
        {
            get => _tags;
            set { _tags = value; OnPropertyChanged(nameof(Tags)); }
        }

        private string _description;
        /// <summary>
        /// Description shown when examining the tile
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        #endregion

        #region Cached Images

        // Cached image (loaded from disk)
        private ImageSource _image;
        public ImageSource Image
        {
            get => _image;
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        private ImageSource _openImage;
        public ImageSource OpenImage
        {
            get => _openImage;
            set { _openImage = value; OnPropertyChanged(nameof(OpenImage)); }
        }

        #endregion
    }

    /// <summary>
    /// Cover types for furniture
    /// </summary>
    public enum CoverType
    {
        None,
        Half,        // +2 AC, +2 Dex saves
        ThreeQuarters // +5 AC, +5 Dex saves
    }
}