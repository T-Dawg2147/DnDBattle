using DnDBattle.Models;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Services.TileService;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Views.Encounters
{
    public partial class EncounterBuilderWindow : UserControl
    {
        private EncounterBuilderService _encounterService;
        private CreatureDatabaseService _dbService;
        private ObservableCollection<EncounterCreature> _encounterCreatures;
        private List<Token> _allCreatures;
        private List<Token> _filteredCreatures;

        public event Action<List<Token>> DeployRequested;

        public EncounterBuilderWindow()
        {
            InitializeComponent();

            _encounterService = new EncounterBuilderService();
            _dbService = new CreatureDatabaseService();
            _encounterCreatures = new ObservableCollection<EncounterCreature>();

            EncounterList.ItemsSource = _encounterCreatures;
            _encounterCreatures.CollectionChanged += (s, e) => RecalculateEncounter();

            Loaded += EncounterBuilderWindow_Loaded;
        }

        private async void EncounterBuilderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCreaturesAsync();
            RecalculateEncounter();
        }

        private async System.Threading.Tasks.Task LoadCreaturesAsync()
        {
            try
            {
                _allCreatures = await _dbService.GetAllCreaturesAsync(limit: 5000);
                _filteredCreatures = _allCreatures.ToList();
                CreatureList.ItemsSource = _filteredCreatures.Take(100); // Limit initial display
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading creatures: {ex.Message}");
                _allCreatures = new List<Token>();
                _filteredCreatures = new List<Token>();
            }
        }

        #region Party Settings

        public int PartySize => CmbPartySize.SelectedIndex + 1;
        public int PartyLevel => CmbPartyLevel.SelectedIndex + 1;

        private void PartySettings_Changed(object sender, SelectionChangedEventArgs e)
        {
            RecalculateEncounter();
        }

        #endregion

        #region Creature Search

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = TxtSearch.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(search))
            {
                _filteredCreatures = _allCreatures?.ToList() ?? new List<Token>();
            }
            else
            {
                _filteredCreatures = _allCreatures?
                    .Where(c => c.Name?.ToLower().Contains(search) == true ||
                                c.Type?.ToLower().Contains(search) == true)
                    .ToList() ?? new List<Token>();
            }

            CreatureList.ItemsSource = _filteredCreatures.Take(100);
        }

        #endregion

        #region Add/Remove Creatures

        private void BtnAddCreature_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Token creature)
            {
                AddCreatureToEncounter(creature);
            }
        }

        private void CreatureList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CreatureList.SelectedItem is Token creature)
            {
                AddCreatureToEncounter(creature);
            }
        }

        private void AddCreatureToEncounter(Token creature)
        {
            // Check if already in list
            var existing = _encounterCreatures.FirstOrDefault(c => c.Creature.Id == creature.Id);
            if (existing != null)
            {
                existing.Quantity++;
                // Force refresh
                var index = _encounterCreatures.IndexOf(existing);
                _encounterCreatures.RemoveAt(index);
                _encounterCreatures.Insert(index, existing);
            }
            else
            {
                _encounterCreatures.Add(new EncounterCreature { Creature = creature, Quantity = 1 });
            }

            RecalculateEncounter();
        }

        private void BtnIncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is EncounterCreature ec)
            {
                ec.Quantity++;
                RefreshEncounterList(ec);
            }
        }

        private void BtnDecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is EncounterCreature ec)
            {
                ec.Quantity--;
                if (ec.Quantity <= 0)
                {
                    _encounterCreatures.Remove(ec);
                }
                else
                {
                    RefreshEncounterList(ec);
                }
            }
        }

        private void BtnRemoveCreature_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is EncounterCreature ec)
            {
                _encounterCreatures.Remove(ec);
            }
        }

        private void RefreshEncounterList(EncounterCreature ec)
        {
            var index = _encounterCreatures.IndexOf(ec);
            if (index >= 0)
            {
                _encounterCreatures.RemoveAt(index);
                _encounterCreatures.Insert(index, ec);
            }
            RecalculateEncounter();
        }

        #endregion

        #region Calculate Encounter

        private void RecalculateEncounter()
        {
            if (_encounterService == null || TxtTotalXP == null) return;

            var calc = _encounterService.CalculateEncounter(_encounterCreatures, PartySize, PartyLevel);

            // Update XP displays
            TxtTotalXP.Text = calc.TotalXP.ToString("N0");
            TxtAdjustedXP.Text = calc.AdjustedXP.ToString("N0");
            TxtMultiplier.Text = $"×{calc.Multiplier:0.#}";

            // Update difficulty badge
            TxtDifficultyIcon.Text = calc.Difficulty.GetIcon();
            TxtDifficulty.Text = calc.Difficulty.GetDisplayName();
            DifficultyBadge.Background = new SolidColorBrush(calc.Difficulty.GetColor());

            // Update thresholds
            TxtEasyThreshold.Text = calc.EasyThreshold.ToString("N0");
            TxtMediumThreshold.Text = calc.MediumThreshold.ToString("N0");
            TxtHardThreshold.Text = calc.HardThreshold.ToString("N0");
            TxtDeadlyThreshold.Text = calc.DeadlyThreshold.ToString("N0");
        }

        #endregion

        #region Presets

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            if (_encounterCreatures.Count == 0)
            {
                MessageBox.Show("Add some creatures to the encounter first.", "Empty Encounter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter a name for this encounter preset:",
                "Save Preset",
                "New Encounter");

            if (string.IsNullOrWhiteSpace(name)) return;

            var preset = new EncounterPreset
            {
                Name = name,
                RecommendedPartySize = PartySize,
                RecommendedPartyLevel = PartyLevel,
                Creatures = _encounterCreatures.Select(ec => new EncounterCreatureEntry
                {
                    CreatureId = ec.Creature.Id.ToString(),
                    CreatureName = ec.Creature.Name,
                    Quantity = ec.Quantity
                }).ToList()
            };

            try
            {
                string presetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");
                Directory.CreateDirectory(presetsDir);

                string fileName = $"{SanitizeFileName(name)}_{DateTime.Now:yyyyMMddHHmmss}.json";
                string filePath = Path.Combine(presetsDir, fileName);

                var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);

                MessageBox.Show($"Preset saved: {name}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadPreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string presetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");
                Directory.CreateDirectory(presetsDir);

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    InitialDirectory = presetsDir,
                    Filter = "Encounter Presets (*.json)|*.json",
                    Title = "Load Encounter Preset"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var preset = JsonSerializer.Deserialize<EncounterPreset>(json);

                    if (preset != null)
                    {
                        LoadPreset(preset);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadPreset(EncounterPreset preset)
        {
            _encounterCreatures.Clear();

            CmbPartySize.SelectedIndex = Math.Clamp(preset.RecommendedPartySize - 1, 0, 7);
            CmbPartyLevel.SelectedIndex = Math.Clamp(preset.RecommendedPartyLevel - 1, 0, 19);

            foreach (var entry in preset.Creatures)
            {
                Token creature = null;

                // Try to find by ID first
                if (!string.IsNullOrEmpty(entry.CreatureId))
                {
                    creature = await _dbService.GetCreatureByIdAsync(entry.CreatureId);
                }

                // Fall back to name search
                if (creature == null && !string.IsNullOrEmpty(entry.CreatureName))
                {
                    creature = _allCreatures?.FirstOrDefault(c =>
                        c.Name.Equals(entry.CreatureName, StringComparison.OrdinalIgnoreCase));
                }

                if (creature != null)
                {
                    _encounterCreatures.Add(new EncounterCreature
                    {
                        Creature = creature,
                        Quantity = entry.Quantity
                    });
                }
            }

            RecalculateEncounter();
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }

        #endregion

        #region Clear / Deploy

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (_encounterCreatures.Count == 0) return;

            var result = MessageBox.Show(
                "Clear all creatures from the encounter?",
                "Clear Encounter",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _encounterCreatures.Clear();
                RecalculateEncounter();
            }
        }

        private void BtnDeploy_Click(object sender, RoutedEventArgs e)
        {
            if (_encounterCreatures.Count == 0)
            {
                MessageBox.Show("Add some creatures to the encounter first.", "Empty Encounter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create list of tokens to deploy
            var tokensToDeploy = new List<Token>();

            foreach (var ec in _encounterCreatures)
            {
                for (int i = 0; i < ec.Quantity; i++)
                {
                    tokensToDeploy.Add(ec.Creature);
                }
            }

            DeployRequested?.Invoke(tokensToDeploy);

            MessageBox.Show(
                $"Deployed {tokensToDeploy.Count} creature(s) to the battle map!",
                "Encounter Deployed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion
    }
}