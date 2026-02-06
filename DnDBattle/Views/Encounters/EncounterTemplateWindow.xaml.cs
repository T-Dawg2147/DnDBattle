using DnDBattle.Models;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
    /// <summary>
    /// Interaction logic for EncounterTemplateWindow.xaml
    /// </summary>
    public partial class EncounterTemplateWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly ObservableCollection<EncounterTemplate> _templates;
        private readonly ObservableCollection<EncounterSlot> _currentSlots;
        private EncounterTemplate _selectedTemplate;

        private const string TemplatesFilePath = "EncounterTemplates.json";

        public EncounterTemplateWindow(MainViewModel vm)
        {
            InitializeComponent();

            _vm = vm;
            _templates = new ObservableCollection<EncounterTemplate>();
            _currentSlots = new ObservableCollection<EncounterSlot>();

            LstTemplates.ItemsSource = _templates;
            LstSlots.ItemsSource = _currentSlots;

            CmbCreatureSelect.ItemsSource = _vm.CreatureBank.OrderBy(c => c.Name).ToList();

            LoadTemplates();

            if (_templates.Count == 0)
            {
                AddDefaultTemplates();
            }
        }

        #region Template Management

        private void LoadTemplates()
        {
            try
            {
                if (File.Exists(TemplatesFilePath))
                {
                    var json = File.ReadAllText(TemplatesFilePath);
                    var templates = JsonSerializer.Deserialize<List<EncounterTemplate>>(json);

                    _templates.Clear();
                    foreach (var t in templates)
                    {
                        _templates.Add(t);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load templates: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveAllTemplates()
        {
            try
            {
                var json = JsonSerializer.Serialize(_templates.ToList(), new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                File.WriteAllText(TemplatesFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save templates: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Look into possibly adding an ID to the templates? May make searching for them quicked/easier.
        private void AddDefaultTemplates()
        {
            _templates.Add(new EncounterTemplate()
            {
                Name = "Goblin Ambush",
                Description = "A classic low-level ambush encounter",
                Difficulty = "Easy",
                Slots = new List<EncounterSlot>()
                {
                    new EncounterSlot { CreatureName = "Goblin", Count = 4 },
                    new EncounterSlot { CreatureName = "Goblin Chief", Count = 1 }
                }
            });

            _templates.Add(new EncounterTemplate()
            {
                Name = "Undead Patrol",
                Description = "Wandering undead in a dungeon",
                Difficulty = "Medium",
                Slots = new List<EncounterSlot>()
                {
                    new EncounterSlot { CreatureName = "Skeleton", Count = 4 },
                    new EncounterSlot { CreatureName = "Zombie", Count = 2 }
                }
            });

            _templates.Add(new EncounterTemplate()
            {
                Name = "Bandit Camp",
                Description = "A group of bandits with their captain",
                Difficulty = "Medium",
                Slots = new List<EncounterSlot>()
                {
                    new EncounterSlot { CreatureName = "Bandit", Count = 5 },
                    new EncounterSlot { CreatureName = "Bandit Captain", Count = 1 }
                }
            });

            SaveAllTemplates();
        }

        private void LstTemplates_SelectionChanged(object sender, RoutedEventArgs e)
        {
            _selectedTemplate = LstTemplates.SelectedItem as EncounterTemplate;

            if (_selectedTemplate != null)
            {
                LoadTemplatesIntoEditor(_selectedTemplate);
            }
        }

        private void LoadTemplatesIntoEditor(EncounterTemplate template)
        {
            TxtTemplateName.Text = template.Name;
            TxtTemplateName.Text = template.Description;

            foreach (ComboBoxItem item in CmbDifficulty.Items)
            {
                if (item.Content.ToString() == template.Difficulty)
                {
                    CmbDifficulty.SelectedItem = item;
                    break;
                }
            }

            _currentSlots.Clear();
            foreach (var slot in template.Slots)
            {
                _currentSlots.Add(new EncounterSlot()
                {
                    CreatureName = slot.CreatureName,
                    Count = slot.Count,
                    Notes = slot.Notes
                });
            }
        }

        private void BtnNewTemplate_Click(object sender, RoutedEventArgs e)
        {
            _selectedTemplate = null;
            LstTemplates.SelectedItem = null;

            TxtTemplateName.Text = "New Encounter";
            TxtTemplateDescription.Text = "";
            CmbDifficulty.SelectedIndex = 0;
            _currentSlots.Clear();

            TxtTemplateName.Focus();
            TxtTemplateName.SelectAll();
        }

        private void BtnDeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTemplate == null)
            {
                MessageBox.Show("Please select a template to delete.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{_selectedTemplate.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _templates.Remove(_selectedTemplate);
                SaveAllTemplates();
                
                _selectedTemplate = null;
                _currentSlots.Clear();
                TxtTemplateName.Text = "";
                TxtTemplateDescription.Text = "";
            }
        }

        #endregion

        #region Creature Slot Management

        private void BtnAddCreature_Click(object sender, RoutedEventArgs e)
        {
            var selectedCreature = CmbCreatureSelect.SelectedItem as Token;
            string creatureName = selectedCreature?.Name ?? CmbCreatureSelect.Text;

            if (string.IsNullOrWhiteSpace(creatureName))
            {
                MessageBox.Show($"Please select or enter a create name.", "No Creature",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(TxtCreatureCount.Text, out int count) || count < 1)
            {
                count = 1;
            }

            var existingSlot = _currentSlots.FirstOrDefault(s =>
                string.Equals(s.CreatureName, creatureName, StringComparison.OrdinalIgnoreCase));

            if (existingSlot != null)
            {
                existingSlot.Count += count;

                var index = _currentSlots.IndexOf(existingSlot);
                _currentSlots.RemoveAt(index);
                _currentSlots.Insert(index, existingSlot);
            }
            else
            {
                _currentSlots.Add(new EncounterSlot()
                {
                    CreatureName = creatureName,
                    Count = count
                });
            }

            TxtCreatureCount.Text = "1";
            CmbCreatureSelect.SelectedIndex = -1;
            CmbCreatureSelect.Text = "";
        }

        private void BtnRemoveSlot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is EncounterSlot slot)
                _currentSlots.Remove(slot);
        }

        #endregion

        #region Save & Spawn

        private void BtnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtTemplateName.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a template name.", "No Template Selected",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var difficulty = (CmbDifficulty.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (_selectedTemplate != null)
            {
                _selectedTemplate.Name = name;
                _selectedTemplate.Description = TxtTemplateDescription.Text?.Trim() ?? "";
                _selectedTemplate.Difficulty = difficulty;
                _selectedTemplate.Slots = _currentSlots.Select(s => new EncounterSlot()
                {
                    CreatureName = s.CreatureName,
                    Count = s.Count,
                    Notes = s.Notes
                }).ToList();

                var index = _templates.IndexOf(_selectedTemplate);
                _templates.RemoveAt(index);
                _templates.Insert(index, _selectedTemplate);
            }
            else
            {
                var newTemplate = new EncounterTemplate()
                {
                    Name = name,
                    Description = TxtTemplateDescription.Text?.Trim() ?? "",
                    Difficulty = difficulty,
                    Slots = _currentSlots.Select(s => new EncounterSlot()
                    {
                        CreatureName = s.CreatureName,
                        Count = s.Count,
                        Notes = s.Notes
                    }).ToList()
                };

                _templates.Add(newTemplate);
                _selectedTemplate = newTemplate;
                LstTemplates.SelectedItem = newTemplate;
            }

            SaveAllTemplates();

            MessageBox.Show($"Template '{name}' saved successfully!", "Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSpawnEncounter_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlots.Count == 0)
            {
                MessageBox.Show("No creatures to spawn. Add creatures to the template first.", "No Creatures",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int baseX = 0, baseY = 0;
            var mw = Application.Current?.MainWindow as Window;

            if (mw != null)
            {
                var battleGrid = mw.FindName("BattleGrid") as Controls.BattleGridControl;
                if (battleGrid != null)
                {
                    var centerScreen = new Point(battleGrid.ActualWidth / 2.0, battleGrid.ActualHeight / 2.0);
                    var world = battleGrid.ScreenToWorldPublic(centerScreen);
                    baseX = (int)Math.Floor(world.X / battleGrid.GridCellSize);
                    baseY = (int)Math.Floor(world.Y / battleGrid.GridCellSize);
                }
            }

            int totalSpawned = 0;
            int offsetX = 0;
            int offsetY = 0;
            int creaturesPerRow = 5;

            foreach (var slot in _currentSlots)
            {
                var prototype = _vm.CreatureBank.FirstOrDefault(c =>
                    string.Equals(c.Name, slot.CreatureName, StringComparison.OrdinalIgnoreCase));

                if (prototype == null)
                {
                    MessageBox.Show($"Creature '{slot.CreatureName}' not fount in creature bank.   Skipping.",
                        "Creature Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    continue;
                }

                for (int i = 0; i < slot.Count; i++)
                {
                    var token = CloneToken(prototype);

                    token.GridX = baseX + offsetX;
                    token.GridY = baseY + offsetY;

                    _vm.Tokens.Add(token);
                    totalSpawned++;

                    offsetX++;
                    if (offsetX >= creaturesPerRow)
                    {
                        offsetX = 0;
                        offsetY++;
                    }
                }
            }

            if (totalSpawned > 0)
            {
                MessageBox.Show($"Spawned {totalSpawned} creature(s) onto the battle map! ",
                    "Encounter Spawned", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private Token CloneToken(Token prototype)
        {
            return new Token()
            {
                Id = Guid.NewGuid(),
                Name = prototype.Name,
                Size = prototype.Size,
                Type = prototype.Type,
                Alignment = prototype.Alignment,
                ChallengeRating = prototype.ChallengeRating,
                ArmorClass = prototype.ArmorClass,
                MaxHP = prototype.MaxHP,
                HP = prototype.MaxHP,
                HitDice = prototype.HitDice,
                InitiativeModifier = prototype.InitiativeModifier,
                Speed = prototype.Speed,
                Str = prototype.Str,
                Dex = prototype.Dex,
                Con = prototype.Con,
                Int = prototype.Int,
                Wis = prototype.Wis,
                Cha = prototype.Cha,
                Skills = prototype.Skills != null ? new List<string>(prototype.Skills) : new List<string>(),
                Senses = prototype.Senses,
                Languages = prototype.Languages,
                Immunities = prototype.Immunities,
                Resistances = prototype.Resistances,
                Vulnerabilities = prototype.Vulnerabilities,
                Traits = prototype.Traits,
                Actions = prototype.Actions != null ? new List<Models.Combat.Action>(prototype.Actions) : new List<Models.Combat.Action>(),
                BonusActions = prototype.BonusActions != null ? new List<Models.Combat.Action>(prototype.BonusActions) : new List<Models.Combat.Action>(),
                Reactions = prototype.Reactions != null ? new List<Models.Combat.Action>(prototype.Reactions) : new List<Models.Combat.Action>(),
                LegendaryActions = prototype.LegendaryActions != null ? new List<Models.Combat.Action>(prototype.LegendaryActions) : new List<Models.Combat.Action>(),
                Notes = prototype.Notes,
                IconPath = prototype.IconPath,
                SizeInSquares = prototype.SizeInSquares,
                Tags = prototype.Tags != null ? new List<string>(prototype.Tags) : new List<string>()
            };
        }

        #endregion
    }
}
