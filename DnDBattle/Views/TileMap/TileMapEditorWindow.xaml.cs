using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.TileService;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace DnDBattle.Views.TileMap
{
    public partial class TileMapEditorWindow : Window
    {
        private Models.Tiles.TileMap _currentMap;
        private string _currentFilePath;
        private readonly TileMapService _mapService;

        public TileMapEditorWindow()
        {
            InitializeComponent();
            _mapService = new TileMapService();

            // Create default map
            CreateNewMap(50, 50);
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
            MessageBox.Show("Image export not yet implemented.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Implement PNG/JPG export
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
            MessageBox.Show("Map resizing not yet implemented.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Show dialog to resize map
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
            // TODO: Implement zoom
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement zoom
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Reset pan/zoom
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