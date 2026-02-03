using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DnDBattle.Views.TileMap
{
    public partial class TileMapEditorWindow : Window
    {
        private Models.Tiles.TileMap _currentMap;
        private string _currentFilePath;
        private readonly TileMapService _mapService;
        private TilePropertiesPanel _propertiesPanel;

        public TileMapEditorWindow()
        {
            InitializeComponent();
            _mapService = new TileMapService();

            // Wire up properties panel
            SetupPropertiesPanel();

            // Create default map
            CreateNewMap(50, 50);
        }

        private void SetupPropertiesPanel()
        {
            // Create properties panel and add to right sidebar
            _propertiesPanel = new TilePropertiesPanel();

            // Wire up events
            EditorControl.TileRightClicked += OnTileRightClicked;
            _propertiesPanel.TilePropertiesChanged += OnTilePropertiesChanged;
        }

        private void OnTileRightClicked(Tile tile)
        {
            // Show properties panel in a popup or side panel
            var window = new Window
            {
                Title = "Tile Properties",
                Content = new TilePropertiesPanel(),
                Width = 350,
                Height = 600,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var panel = window.Content as TilePropertiesPanel;
            panel.SetTile(tile);
            panel.TilePropertiesChanged += (t) =>
            {
                EditorControl.TileMap = _currentMap; // Force refresh
                window.Close();
            };

            window.ShowDialog();
        }

        private void OnTilePropertiesChanged(Tile tile)
        {
            // Refresh the map view
            EditorControl.TileMap = _currentMap;
        }

        private void PalettePanel_TileSelected(TileDefinition tileDef)
        {
            EditorControl.SelectedTileDefinition = tileDef;
        }

        #region File Menu

        private void NewMap_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show dialog to input width/height
            CreateNewMap(50, 50);
        }

        private async void OpenMap_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Tile Map"
            };

            if (dialog.ShowDialog() == true)
            {
                var map = await _mapService.LoadMapAsync(dialog.FileName);
                if (map != null)
                {
                    _currentMap = map;
                    _currentFilePath = dialog.FileName;
                    EditorControl.TileMap = _currentMap;
                    Title = $"Tile Map Builder - {map.Name}";
                }
                else
                {
                    MessageBox.Show("Failed to load map.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void SaveMap_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveMapAs_Click(sender, e);
                return;
            }

            bool success = await _mapService.SaveMapAsync(_currentMap, _currentFilePath);
            if (success)
            {
                MessageBox.Show("Map saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save map.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveMapAs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Tile Map As",
                FileName = _currentMap.Name + ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                _currentFilePath = dialog.FileName;
                bool success = await _mapService.SaveMapAsync(_currentMap, _currentFilePath);

                if (success)
                {
                    MessageBox.Show("Map saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save map.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                Title = "Export Map as Image",
                FileName = _currentMap.Name + ".png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var canvas = EditorControl.MapCanvas;

                    double scale = 2.0;
                    int width = (int)(canvas.ActualWidth * scale);
                    int height = (int)(canvas.ActualHeight * scale);

                    var renderBitmap = new RenderTargetBitmap(
                        width,
                        height,
                        96 * scale,
                        96 * scale,
                        PixelFormats.Pbgra32);

                    var visual = new DrawingVisual();
                    using (var context = visual.RenderOpen())
                    {
                        var brush = new VisualBrush(canvas);
                        context.PushTransform(new ScaleTransform(scale, scale));
                        context.DrawRectangle(brush, null, new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight));
                    }

                    renderBitmap.Render(visual);

                    BitmapEncoder encoder;
                    if (dialog.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        encoder = new JpegBitmapEncoder { QualityLevel = 95 };
                    }
                    else
                    {
                        encoder = new PngBitmapEncoder();
                    }

                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (var stream = File.Create(dialog.FileName))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show(
                        $"Map exported successfully!\n\nResolution: {width}x{height}\nFile: {dialog.FileName}",
                        "Export Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to export image:\n\n{ex.Message}",
                        "Export Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Edit Menu

        private void ClearAllTiles_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all tiles?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _currentMap.PlacedTiles.Clear();
                EditorControl.TileMap = _currentMap; // Force refresh
            }
        }

        private void ResizeMap_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MapResizeDialog(_currentMap);
            if (dialog.ShowDialog() == true)
            {
                int oldWidth = _currentMap.Width;
                int oldHeight = _currentMap.Height;

                // Remove tiles outside new bounds
                var tilesToRemove = _currentMap.PlacedTiles
                    .Where(t => t.GridX >= dialog.NewWidth || t.GridY >= dialog.NewHeight)
                    .ToList();

                foreach (var tile in tilesToRemove)
                {
                    _currentMap.PlacedTiles.Remove(tile);
                }

                // Update map size
                _currentMap.Width = dialog.NewWidth;
                _currentMap.Height = dialog.NewHeight;
                _currentMap.ModifiedDate = DateTime.Now;

                // Refresh editor
                EditorControl.TileMap = null;
                EditorControl.TileMap = _currentMap;

                MessageBox.Show(
                    $"Map resized from {oldWidth}×{oldHeight} to {dialog.NewWidth}×{dialog.NewHeight}\n\n" +
                    $"Tiles removed: {tilesToRemove.Count}",
                    "Map Resized",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        #endregion

        #region View Menu

        private void ToggleGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMap != null)
            {
                _currentMap.ShowGrid = MenuShowGrid.IsChecked;
                EditorControl.TileMap = _currentMap; // Force refresh
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            // Access zoom transform from editor control
            var zoom = EditorControl.ZoomTransform;
            zoom.ScaleX = Math.Min(4.0, zoom.ScaleX * 1.2);
            zoom.ScaleY = Math.Min(4.0, zoom.ScaleY * 1.2);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            var zoom = EditorControl.ZoomTransform;
            zoom.ScaleX = Math.Max(0.25, zoom.ScaleX / 1.2);
            zoom.ScaleY = Math.Max(0.25, zoom.ScaleY / 1.2);
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            EditorControl.ZoomTransform.ScaleX = 1.0;
            EditorControl.ZoomTransform.ScaleY = 1.0;
            EditorControl.PanTransform.X = 0;
            EditorControl.PanTransform.Y = 0;
        }

        #endregion

        #region Helper Methods

        private void CreateNewMap(int width, int height)
        {
            _currentMap = new Models.Tiles.TileMap
            {
                Name = "New Map",
                Width = width,
                Height = height,
                CellSize = 48
            };
            _currentFilePath = null;
            EditorControl.TileMap = _currentMap;
            Title = "Tile Map Builder - New Map";
        }

        #endregion
    }
}