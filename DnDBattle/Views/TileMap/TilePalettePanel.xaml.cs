using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Services.TileService;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Views.TileMap
{
    public partial class TilePalettePanel : UserControl
    {
        public event Action<TileDefinition> TileSelected;

        private TileDefinition _selectedTile;
        private string _searchFilter = "";

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
            try
            {
                // Load tile library
                TileLibraryService.Instance.LoadTileLibrary();

                // Apply filter and display
                ApplyFilter();

                int totalTiles = TileLibraryService.Instance.AvailableTiles.Count;
                StatusText.Text = $"{totalTiles} tiles available";

                // Debug output
                System.Diagnostics.Debug.WriteLine($"[TilePalette] Loaded {totalTiles} tiles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TilePalette] Error loading tiles: {ex.Message}");
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        // VISUAL REFRESH - TILE_MAP_EDITOR
        private void ApplyFilter()
        {
            var allTiles = TileLibraryService.Instance.AvailableTiles;

            if (allTiles == null || allTiles.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[TilePalette] No tiles available!");
                TileList.ItemsSource = null;
                return;
            }

            // Apply search filter
            var filtered = string.IsNullOrWhiteSpace(_searchFilter)
                ? allTiles
                : allTiles.Where(t =>
                    (t.DisplayName ?? t.Id).Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    (t.Category ?? "").Contains(_searchFilter, StringComparison.OrdinalIgnoreCase));

            // Group by category
            var grouped = filtered
                .GroupBy(t => t.Category ?? "General")
                .OrderBy(g => g.Key)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[TilePalette] Grouped into {grouped.Count} categories");

            // Set ItemsSource
            TileList.ItemsSource = grouped;

            // Update status
            int filteredCount = filtered.Count();
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                StatusText.Text = $"{filteredCount} tiles match '{_searchFilter}'";
            }
            else
            {
                StatusText.Text = $"{filteredCount} tiles available";
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchFilter = TxtSearch.Text;
            ApplyFilter();
        }

        private void RefreshLibrary_Click(object sender, RoutedEventArgs e)
        {
            LoadTiles();
            StatusText.Text = "Library refreshed";
        }

        private async void ImportTiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                    Multiselect = true,
                    Title = "Import Tile Images"
                };

                if (dialog.ShowDialog() == true)
                {
                    var importService = new TileImportService();
                    // Show category selection dialog
                    var categoryDialog = new TileCategoryDialog(importService.GetAvailableCategories());
                    if (categoryDialog.ShowDialog() == true)
                    {
                        string category = categoryDialog.SelectedCategory;

                        // Import tiles
                        
                        int imported = importService.ImportMultipleTilesAsync(dialog.FileNames, category).Result.Count;

                        MessageBox.Show(
                            $"Successfully imported {imported} tiles to category '{category}'",
                            "Import Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Reload library
                        LoadTiles();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error importing tiles:\n\n{ex.Message}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void TileItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileDefinition tile)
            {
                _selectedTile = tile;
                TileSelected?.Invoke(tile);

                StatusText.Text = $"Selected: {tile.DisplayName ?? tile.Id}";
                System.Diagnostics.Debug.WriteLine($"[TilePalette] Selected tile: {tile.DisplayName}");
            }
        }
    }
}