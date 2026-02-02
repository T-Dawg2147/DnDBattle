using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.TileService;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DnDBattle.Views.TileMap
{
    public partial class TilePalettePanel : UserControl
    {
        public event Action<TileDefinition> TileSelected;

        private TileDefinition _selectedTile;

        public TilePalettePanel()
        {
            InitializeComponent();
            Loaded += TilePalettePanel_Loaded;
        }

        private void TilePalettePanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTiles();
        }

        private void LoadTiles()
        {
            TileLibraryService.Instance.LoadTileLibrary();
            var grouped = TileLibraryService.Instance.GetTilesByCategory();
            TileList.ItemsSource = grouped;
        }

        private void RefreshLibrary_Click(object sender, RoutedEventArgs e)
        {
            LoadTiles();
        }

        private void TileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileDefinition tileDef)
            {
                _selectedTile = tileDef;
                StatusText.Text = $"Selected: {tileDef.DisplayName}";
                TileSelected?.Invoke(tileDef);
            }
        }
    }

    /// <summary>
    /// Converter to load tile images via cache
    /// </summary>
    public class TileImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath)
            {
                return TileImageCacheService.Instance.GetOrLoadImage(imagePath);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}