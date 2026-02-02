using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileMap;
using DnDBattle.Services.TileService;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            int totalTiles = TileLibraryService.Instance.AvailableTiles.Count;
            StatusText.Text = $"{totalTiles} tiles available";
        }

        private void RefreshLibrary_Click(object sender, RoutedEventArgs e)
        {
            LoadTiles();
            StatusText.Text = "Library refreshed";
        }

        private async void ImportTiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Tile Images",
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                var importService = new TileImportService();

                // Ask for category
                var categoryDialog = new TileCategoryDialog(importService.GetAvailableCategories());
                if (categoryDialog.ShowDialog() == true)
                {
                    string category = categoryDialog.SelectedCategory;

                    StatusText.Text = "Importing...";

                    var imported = await importService.ImportMultipleTilesAsync(dialog.FileNames, category);

                    LoadTiles();
                    StatusText.Text = $"Imported {imported.Count} tiles into '{category}'";
                }
            }
        }

        private void TileButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is TileDefinition tileDef)
            {
                _selectedTile = tileDef;
                StatusText.Text = $"Selected: {tileDef.DisplayName}";
                TileSelected?.Invoke(tileDef);
            }
        }
    }
}