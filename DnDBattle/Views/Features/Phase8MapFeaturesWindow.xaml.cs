using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DnDBattle.Controls;
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
using Microsoft.Win32;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services.TileService;

namespace DnDBattle.Views.Features
{
    /// <summary>
    /// Phase 8: Advanced Map Features window.
    /// Provides UI for multi-map management, background layers, hex grids,
    /// gridless mode, custom grid sizes, and map notes.
    /// </summary>
    public partial class Phase8MapFeaturesWindow : Window
    {
        private readonly BattleGridControl _battleGrid;
        private readonly MapLibraryService _mapLibrary = new();
        private readonly ObservableCollection<string> _actionLog = new();

        public Phase8MapFeaturesWindow(BattleGridControl battleGrid)
        {
            InitializeComponent();
            _battleGrid = battleGrid;

            LstActionLog.ItemsSource = _actionLog;
            LstRecentMaps.ItemsSource = _mapLibrary.RecentMaps;

            LoadCurrentValues();
        }

        /// <summary>
        /// Load current option values into all toggle controls.
        /// </summary>
        private void LoadCurrentValues()
        {
            // 8.1 Multi-Map Management
            ChkEnableMultiMap.IsChecked = Options.EnableMultiMapManagement;
            SliderMaxRecent.Value = Options.MapLibraryMaxRecent;
            TxtMaxRecent.Text = Options.MapLibraryMaxRecent.ToString();

            // 8.2 Background Layers
            ChkEnableBackgroundLayers.IsChecked = Options.EnableBackgroundLayers;

            // 8.3 Hex Grid
            ChkEnableHexGrid.IsChecked = Options.EnableHexGrid;

            // 8.4 Gridless Mode
            ChkEnableGridless.IsChecked = Options.EnableGridlessMode;

            // 8.5 Custom Grid Sizes
            ChkEnableCustomGridSizes.IsChecked = Options.EnableCustomGridSizes;
            SliderFeetPerSquare.Value = Options.DefaultFeetPerSquare;
            TxtFeetPerSquare.Text = $"{Options.DefaultFeetPerSquare} ft";

            // 8.6 Map Notes
            ChkEnableMapNotes.IsChecked = Options.EnableMapNotes;
            ChkShowDMNotes.IsChecked = Options.ShowDMOnlyNotes;
            SliderNoteFontSize.Value = Options.MapNoteDefaultFontSize;
            TxtNoteFontSize.Text = Options.MapNoteDefaultFontSize.ToString("F0");

            RefreshNotesListFromGrid();
            RefreshBackgroundLayersFromGrid();
        }

        private void Log(string message)
        {
            _actionLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        #region 8.1 Multi-Map Management

        private void ChkEnableMultiMap_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableMultiMapManagement = ChkEnableMultiMap.IsChecked == true;
            Log($"Multi-Map Management: {(Options.EnableMultiMapManagement ? "ON" : "OFF")}");
        }

        private void SliderMaxRecent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtMaxRecent == null) return;
            Options.MapLibraryMaxRecent = (int)SliderMaxRecent.Value;
            TxtMaxRecent.Text = Options.MapLibraryMaxRecent.ToString();
        }

        private void AddMapToLibrary_Click(object sender, RoutedEventArgs e)
        {
            var map = _battleGrid?.TileMap;
            if (map == null)
            {
                MessageBox.Show("No map is currently loaded.", "Add Map", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mapRef = new TileMapReference
            {
                Id = map.Id.ToString(),
                Name = map.Name,
                LastUsed = DateTime.Now
            };

            _mapLibrary.AddMap(mapRef);
            _mapLibrary.SetCurrentMap(map);
            RefreshRecentMaps();
            Log($"Added map '{map.Name}' to library.");
        }

        private void SearchMaps_Click(object sender, RoutedEventArgs e)
        {
            var query = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter search query:", "Search Maps", "");

            if (string.IsNullOrWhiteSpace(query)) return;

            var results = _mapLibrary.SearchMaps(query);
            if (results.Count == 0)
            {
                MessageBox.Show("No maps found.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var names = string.Join("\n", results.Select(r => r.Name));
                MessageBox.Show($"Found {results.Count} map(s):\n{names}", "Search Results",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            Log($"Searched maps for '{query}': {results.Count} result(s).");
        }

        private void LstRecentMaps_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void LoadSelectedMap_Click(object sender, RoutedEventArgs e)
        {
            if (LstRecentMaps.SelectedItem is not TileMapReference selected)
            {
                MessageBox.Show("Select a map from the recent list.", "Load Map", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _mapLibrary.LoadMap(selected.Id);
            if (_mapLibrary.CurrentMap != null && _battleGrid != null)
            {
                _battleGrid.TileMap = _mapLibrary.CurrentMap;
            }
            RefreshRecentMaps();
            Log($"Loaded map '{selected.Name}'.");
        }

        private void RefreshRecentMaps()
        {
            LstRecentMaps.ItemsSource = null;
            LstRecentMaps.ItemsSource = _mapLibrary.RecentMaps;
        }

        #endregion

        #region 8.2 Background Image Layers

        private void ChkEnableBackgroundLayers_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableBackgroundLayers = ChkEnableBackgroundLayers.IsChecked == true;
            Log($"Background Layers: {(Options.EnableBackgroundLayers ? "ON" : "OFF")}");
        }

        private void AddBackgroundLayer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
                Title = "Select Background Image"
            };

            if (dialog.ShowDialog() == true)
            {
                var layer = new BackgroundLayer
                {
                    ImagePath = dialog.FileName,
                    Opacity = SliderLayerOpacity.Value,
                    IsVisible = true
                };

                var map = _battleGrid?.TileMap;
                if (map != null)
                {
                    map.BackgroundLayers.Add(layer);
                    RefreshBackgroundLayersFromGrid();
                    Log($"Added background layer: {System.IO.Path.GetFileName(dialog.FileName)}");
                }
            }
        }

        private void SliderLayerOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtLayerOpacity == null) return;
            TxtLayerOpacity.Text = SliderLayerOpacity.Value.ToString("F2");

            if (LstBackgroundLayers?.SelectedItem is BackgroundLayer selected)
            {
                selected.Opacity = SliderLayerOpacity.Value;
            }
        }

        private void LstBackgroundLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstBackgroundLayers.SelectedItem is BackgroundLayer selected)
            {
                SliderLayerOpacity.Value = selected.Opacity;
            }
        }

        private void RemoveBackgroundLayer_Click(object sender, RoutedEventArgs e)
        {
            if (LstBackgroundLayers.SelectedItem is not BackgroundLayer selected) return;

            var map = _battleGrid?.TileMap;
            if (map != null)
            {
                map.BackgroundLayers.Remove(selected);
                RefreshBackgroundLayersFromGrid();
                Log("Removed background layer.");
            }
        }

        private void RefreshBackgroundLayersFromGrid()
        {
            var map = _battleGrid?.TileMap;
            LstBackgroundLayers.ItemsSource = null;
            if (map != null)
            {
                LstBackgroundLayers.ItemsSource = map.BackgroundLayers;
            }
        }

        #endregion

        #region 8.3 Hexagonal Grids

        private void ChkEnableHexGrid_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableHexGrid = ChkEnableHexGrid.IsChecked == true;
            Log($"Hex Grid Support: {(Options.EnableHexGrid ? "ON" : "OFF")}");
        }

        private void CmbGridType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbGridType.SelectedIndex < 0) return;

            var map = _battleGrid?.TileMap;
            if (map == null) return;

            map.GridType = CmbGridType.SelectedIndex switch
            {
                1 => GridType.HexFlatTop,
                2 => GridType.HexPointyTop,
                _ => GridType.Square
            };
            Log($"Grid type set to {map.GridType}.");
        }

        #endregion

        #region 8.4 Gridless Mode

        private void ChkEnableGridless_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableGridlessMode = ChkEnableGridless.IsChecked == true;

            var map = _battleGrid?.TileMap;
            if (map != null)
            {
                map.GridlessMode = Options.EnableGridlessMode;
            }
            Log($"Gridless Mode: {(Options.EnableGridlessMode ? "ON" : "OFF")}");
        }

        #endregion

        #region 8.5 Custom Grid Sizes

        private void ChkEnableCustomGridSizes_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableCustomGridSizes = ChkEnableCustomGridSizes.IsChecked == true;
            Log($"Custom Grid Sizes: {(Options.EnableCustomGridSizes ? "ON" : "OFF")}");
        }

        private void SliderFeetPerSquare_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtFeetPerSquare == null) return;
            int feet = (int)SliderFeetPerSquare.Value;
            Options.DefaultFeetPerSquare = feet;
            TxtFeetPerSquare.Text = $"{feet} ft";

            var map = _battleGrid?.TileMap;
            if (map != null)
            {
                map.FeetPerSquare = feet;
            }
        }

        #endregion

        #region 8.6 Map Notes & Labels

        private void ChkEnableMapNotes_Changed(object sender, RoutedEventArgs e)
        {
            Options.EnableMapNotes = ChkEnableMapNotes.IsChecked == true;
            Log($"Map Notes: {(Options.EnableMapNotes ? "ON" : "OFF")}");
        }

        private void ChkShowDMNotes_Changed(object sender, RoutedEventArgs e)
        {
            Options.ShowDMOnlyNotes = ChkShowDMNotes.IsChecked == true;
            Log($"Show DM Notes: {(Options.ShowDMOnlyNotes ? "ON" : "OFF")}");
        }

        private void SliderNoteFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtNoteFontSize == null) return;
            Options.MapNoteDefaultFontSize = SliderNoteFontSize.Value;
            TxtNoteFontSize.Text = SliderNoteFontSize.Value.ToString("F0");
        }

        private void AddNote_Click(object sender, RoutedEventArgs e)
        {
            var text = TxtNoteText.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Enter note text first.", "Add Note", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var map = _battleGrid?.TileMap;
            if (map == null) return;

            var category = CmbNoteCategory.SelectedIndex switch
            {
                1 => NoteCategory.Trap,
                2 => NoteCategory.Treasure,
                3 => NoteCategory.NPC,
                4 => NoteCategory.Quest,
                5 => NoteCategory.Lore,
                6 => NoteCategory.Secret,
                _ => NoteCategory.General
            };

            var note = new MapNote
            {
                Text = text,
                GridX = map.Width / 2,
                GridY = map.Height / 2,
                Category = category,
                FontSize = Options.MapNoteDefaultFontSize,
                IsPlayerVisible = true
            };

            map.AddNote(note);
            TxtNoteText.Text = "";
            RefreshNotesListFromGrid();
            Log($"Added note: '{text}' at center ({note.GridX},{note.GridY}).");
        }

        private void RemoveNote_Click(object sender, RoutedEventArgs e)
        {
            if (LstMapNotes.SelectedItem is not MapNote selected) return;

            var map = _battleGrid?.TileMap;
            if (map != null)
            {
                map.RemoveNote(selected.Id);
                RefreshNotesListFromGrid();
                Log($"Removed note: '{selected.Text}'.");
            }
        }

        private void RefreshNotesListFromGrid()
        {
            var map = _battleGrid?.TileMap;
            LstMapNotes.ItemsSource = null;
            if (map != null)
            {
                LstMapNotes.ItemsSource = map.Notes;
            }
        }

        #endregion
    }
}
