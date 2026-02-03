using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DnDBattle.Views.TileMap
{
    /// <summary>
    /// Interaction logic for MinimapControl.xaml
    /// </summary>
    public partial class MinimapControl : UserControl
    {
        private Models.Tiles.TileMap _tileMap;
        private double _scale = 1.0;

        public MinimapControl()
        {
            InitializeComponent();
        }

        public void SetTileMap(Models.Tiles.TileMap tileMap)
        {
            _tileMap = tileMap;
            RenderMinimap();
        }

        public void UpdateViewport(double viewportX, double viewportY, double viewportWidth, double viewportHeight)
        {
            if (_tileMap == null) return;

            // Scale viewport to minimap coordinates
            double scaleX = MinimapCanvas.ActualWidth / _tileMap.Width;
            double scaleY = MinimapCanvas.ActualHeight / _tileMap.Height;

            Canvas.SetLeft(ViewportRect, viewportX * scaleX);
            Canvas.SetTop(ViewportRect, viewportY * scaleY);
            ViewportRect.Width = viewportWidth * scaleX;
            ViewportRect.Height = viewportHeight * scaleY;
        }

        private void RenderMinimap()
        {
            if (_tileMap == null) return;

            MinimapCanvas.Children.Clear();

            double width = MinimapCanvas.ActualWidth;
            double height = MinimapCanvas.ActualHeight;

            if (width == 0 || height == 0)
            {
                // Wait for layout
                Loaded += (s, e) => RenderMinimap();
                return;
            }

            // Calculate scale
            double scaleX = width / _tileMap.Width;
            double scaleY = height / _tileMap.Height;
            _scale = Math.Min(scaleX, scaleY);

            // Draw simplified tiles
            foreach (var tile in _tileMap.PlacedTiles)
            {
                var tileDef = TileLibraryService.Instance.GetTileById(tile.TileDefinitionId);
                if (tileDef == null) continue;

                // Simple colored rectangle per tile
                var rect = new Rectangle
                {
                    Width = _scale,
                    Height = _scale,
                    Fill = GetLayerBrush(tileDef.Layer)
                };

                Canvas.SetLeft(rect, tile.GridX * _scale);
                Canvas.SetTop(rect, tile.GridY * _scale);

                MinimapCanvas.Children.Add(rect);
            }
        }

        private Brush GetLayerBrush(TileLayer layer)
        {
            return layer switch
            {
                TileLayer.Floor => new SolidColorBrush(Color.FromRgb(101, 67, 33)),
                TileLayer.Terrain => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                TileLayer.Wall => new SolidColorBrush(Color.FromRgb(96, 96, 96)),
                TileLayer.Door => new SolidColorBrush(Color.FromRgb(121, 85, 72)),
                TileLayer.Furniture => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                TileLayer.Props => new SolidColorBrush(Color.FromRgb(255, 235, 59)),
                TileLayer.Effects => new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                TileLayer.Roof => new SolidColorBrush(Color.FromRgb(63, 81, 181)),
                _ => Brushes.Gray
            };
        }
    }
}
