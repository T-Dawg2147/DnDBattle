using DnDBattle.Models;
using DnDBattle.ViewModels;
using DnDBattle.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.DirectoryServices;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for CreatureBrowserWindow.xaml
    /// </summary>
    public partial class CreatureBrowserWindow : UserControl
    {
        private MainViewModel _vm;
        private List<Token> _allCreatures = new List<Token>();
        private List<Token> _filteredCreatures = new List<Token>();
        private Token _selectedCreature;
        private string _currentGrouping = "Type";
        private bool _isInitialized = false;

        public CreatureBrowserWindow()
        {
            InitializeComponent();
            Loaded += CreatureBrowserWindow_Loaded;
        }

        private async void CreatureBrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as MainViewModel;
            if (_vm == null) return;

            _isInitialized = true;

            await LoadCreaturesFromDatabase();
        }

        #region Data Loading

        /*private async Task LoadCreaturesFromDatabase()
        {
            if (!_isInitialized) return;
            try
            {
                using (var dbService = new CreatureDatabaseService())
                {
                    _allCreatures = await dbService.SearchCreaturesAsync(sortBy: "Name", limit: 10000);
                }
                _filteredCreatures = new List<Token>(_allCreatures);

                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateTreeView();
                    UpdateCreatureCount();
                });
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading creatures: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/

        // DEBUG METHOD
        private async System.Threading.Tasks.Task LoadCreaturesFromDatabase()
        {
            if (!_isInitialized) return;

            try
            {
                using (var dbService = new CreatureDatabaseService())
                {
                    _allCreatures = await System.Threading.Tasks.Task.Run(async () =>
                    {
                        return await dbService.SearchCreaturesAsync(
                            sortBy: "Name",
                            limit: 10000);
                    });
                }

                // DEBUG: Log some info about the creatures
                System.Diagnostics.Debug.WriteLine($"Loaded {_allCreatures.Count} creatures");

                // Check what types we have
                var types = _allCreatures
                    .GroupBy(c => c.Type ?? "NULL")
                    .Select(g => $"{g.Key}: {g.Count()}")
                    .ToList();

                System.Diagnostics.Debug.WriteLine("Types found:");
                foreach (var t in types)
                {
                    System.Diagnostics.Debug.WriteLine($"  {t}");
                }

                // Check first few creatures
                foreach (var c in _allCreatures.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"Creature: {c.Name}, Type: '{c.Type}', CR: '{c.ChallengeRating}'");
                }

                _filteredCreatures = new List<Token>(_allCreatures);

                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateTreeView();
                    UpdateCreatureCount();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading creatures: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            await LoadCreaturesFromDatabase();
            MessageBox.Show($"Reloaded {_allCreatures.Count} creatures from database.", "Reload Complete", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Treeview Population

        private void UpdateTreeView()
        {
            if (CreatureTree == null) return;

            CreatureTree.Items.Clear();

            if (_filteredCreatures == null || _filteredCreatures.Count == 0)
                return;

            if (_currentGrouping == "None")
            {
                foreach (var creature in _filteredCreatures.OrderBy(c => c.Name))
                {
                    CreatureTree.Items.Add(CreateCreatureTreeItem(creature));
                }
            }
            else
            {
                var groups = GroupCreatures(_filteredCreatures, _currentGrouping);

                var sortedGroups = groups
                    .OrderBy(g => g.Key == "All" ? 0 : 1)
                    .ThenBy(g => g.Key)
                    .ToList();

                foreach (var group in sortedGroups)
                {
                    var groupItem = new TreeViewItem()
                    {
                        Header = CreateGroupHeader(group.Key, group.Value.Count),
                        IsExpanded = false,
                        Tag = "group"
                    };

                    foreach (var creature in group.Value.OrderBy(c => c.Name))
                    {
                        var creatureItem = CreateCreatureTreeItem(creature);
                        groupItem.Items.Add(creatureItem);
                    }

                    CreatureTree.Items.Add(groupItem);
                }
            }
        }

        private Dictionary<string, List<Token>> GroupCreatures(List<Token> creatures, string groupBy)
        {
            var groups = new Dictionary<string, List<Token>>();

            if (creatures == null) return groups;

            foreach (var creature in creatures)
            {
                string key = GetGroupKey(creature, groupBy);

                if (string.IsNullOrWhiteSpace(key))
                    key = "Unknown";

                key = char.ToUpper(key[0]) + key.Substring(1);

                if (!groups.ContainsKey(key))
                    groups[key] = new List<Token>();

                groups[key].Add(creature);
            }

            return groups;
        }

        private string GetGroupKey(Token creature, string groupBy)
        {
            if (creature == null) return "Unknown";

            switch (groupBy)
            {
                case "Type":
                    return string.IsNullOrWhiteSpace(creature.Type) 
                        ? "Unknown" 
                        : creature.Type.Trim();

                case "ChallangeRating":
                    return string.IsNullOrWhiteSpace(creature.ChallengeRating) 
                        ? "Unknown" 
                        : $"CR {creature.ChallengeRating.Trim()}";
                case "Size":
                    return string.IsNullOrWhiteSpace(creature.Size)
                        ? "Unknown"
                        : creature.Size.Trim();
                case "Category":
                    if (creature.Extras != null && creature.Extras.TryGetValue("Category", out var cat))
                        return cat?.ToString().Trim() ?? "Unknown";
                    return "Unknown";

                default:
                    return "All";
            }
        }

        private StackPanel CreateGroupHeader(string groupName, int count)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            panel.Children.Add(new TextBlock()
            {
                Text = groupName,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.LightSkyBlue,
                VerticalAlignment = VerticalAlignment.Center
            });

            var badge = new Border()
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(62, 62, 66)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(8, 0, 0, 0),
                Child = new TextBlock()
                {
                    Text = count.ToString(),
                    Foreground = Brushes.LightGray,
                    FontSize = 11
                }
            };

            panel.Children.Add(badge);
            return panel;
        }

        private TreeViewItem CreateCreatureTreeItem(Token creature)
        {
            if (creature == null)
                return new TreeViewItem { Header = "Unknown" };

            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconBorder = new Border()
            {
                Width = 22,
                Height = 22,
                Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 8, 0)
            };

            if (!string.IsNullOrEmpty(creature.IconPath))
            {
                iconBorder.Child = new Image()
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(creature.IconPath, UriKind.RelativeOrAbsolute)),
                    Stretch = Stretch.Uniform
                };
            }

            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            var nameText = new TextBlock()
            {
                Text = creature.Name,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);

            var crText = new TextBlock()
            {
                Text = $"CR {creature.ChallengeRating}",
                Foreground = Brushes.Gray,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(crText, 2);
            grid.Children.Add(crText);

            return new TreeViewItem()
            {
                Header = grid,
                Tag = creature
            };
        }

        #endregion

        #region Filtering

        private void TxtSearch_TextChanged(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";
            TxtCRMin.Text = "";
            TxtCRMax.Text = "";
            TxtHPMin.Text = "";
            TxtHPMax.Text = "";
            CmbSizeFilter.SelectedIndex = 0;

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (!_isInitialized || _allCreatures == null) return;

            var filtered = _allCreatures.AsEnumerable();

            var search = TxtSearch?.Text?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(c =>
                    c.Name?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (double.TryParse(TxtCRMin?.Text, out double crMin))
            {
                filtered = filtered.Where(c => ParseCR(c.ChallengeRating) >= crMin);
            }

            if (double.TryParse(TxtCRMax?.Text, out double crMax))
            {
                filtered = filtered.Where(c => ParseCR(c.ChallengeRating) <= crMax);
            }

            if (int.TryParse(TxtHPMin?.Text, out int hpMin))
            {
                filtered = filtered.Where(c => c.MaxHP >= hpMin);
            }
            if (int.TryParse(TxtHPMax?.Text, out int hpMax))
            {
                filtered = filtered.Where(c => c.MaxHP <= hpMax);
            }

            var selectedSize = (CmbSizeFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (!string.IsNullOrEmpty(selectedSize) && selectedSize != "All")
            {
                filtered = filtered.Where(c =>
                    string.Equals(c.Size, selectedSize, StringComparison.OrdinalIgnoreCase));
            }

            _filteredCreatures = filtered.ToList();
            UpdateTreeView();
            UpdateCreatureCount();
        }

        private double ParseCR(string cr)
        {
            if (string.IsNullOrWhiteSpace(cr)) return 0;

            cr = cr.Trim();
            if (cr.Contains("/"))
            {
                var parts = cr.Split('/');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out double a) &&
                    double.TryParse(parts[1], out double b) &&
                    b != 0)
                {
                    return a / b;
                }
            }

            if (double.TryParse(cr, out double d))
                return d;

            return 0;
        }

        private void UpdateCreatureCount()
        {
            if (TxtCreatureCount != null)
            {
                TxtCreatureCount.Text = $"Showing {_filteredCreatures.Count} of {_allCreatures.Count} creatures";
            }            
        }

        #endregion

        #region Grouping

        private void CmbGroupBy_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            var selected = CmbGroupBy.SelectedItem as ComboBoxItem;
            _currentGrouping = selected?.Tag?.ToString() ?? "Type";
            UpdateTreeView();
        }

        #endregion

        #region Selection & Details

        private void CreatureTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is Token creature)
            {
                _selectedCreature = creature;
                ShowCreatureDetails(creature);
            }
            else
            {
                _selectedCreature = null;
                HideCreatureDetails();
            }
        }

        private void ShowCreatureDetails(Token creature)
        {
            if (creature == null) return;

            TxtNoSelection.Visibility = Visibility.Collapsed;
            CreatureDetails.Visibility = Visibility.Visible;

            TxtCreatureName.Text = creature.Name;
            TxtCreatureSubtitle.Text = $"{creature.Size} {creature.Type}" +
                (string.IsNullOrEmpty(creature.Alignment) ? "" : $", {creature.Alignment}");

            TxtAC.Text = creature.ArmorClass.ToString();
            TxtHP.Text = $"{creature.MaxHP}";
            TxtCR.Text = creature.ChallengeRating ?? "-";
            TxtSpeed.Text = creature.Speed ?? "30ft. ";

            TxtStr.Text = FormatAbilityScore(creature.Str);
            TxtDex.Text = FormatAbilityScore(creature.Dex);
            TxtCon.Text = FormatAbilityScore(creature.Con);
            TxtInt.Text = FormatAbilityScore(creature.Int);
            TxtWis.Text = FormatAbilityScore(creature.Wis);
            TxtCha.Text = FormatAbilityScore(creature.Cha);

            if (!string.IsNullOrWhiteSpace(creature.Traits))
            {
                TraitsSection.Visibility = Visibility.Visible;
                TxtTraits.Text = creature.Traits;
            }
            else
            {
                TraitsSection.Visibility = Visibility.Collapsed;
            }

            if (creature.Actions != null && creature.Actions.Count > 0)
            {
                ActionsSection.Visibility = Visibility.Visible;
                ActionsList.Items.Clear();

                foreach (var action in creature.Actions)
                {
                    var actionPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

                    actionPanel.Children.Add(new TextBlock()
                    {
                        Text = action.Name,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    });
                    if (!string.IsNullOrEmpty(action.Description))
                    {
                        actionPanel.Children.Add(new TextBlock()
                        {
                            Text = action.Description,
                            Foreground = Brushes.LightGray,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 3, 0, 0)
                        });
                    }
                    
                    ActionsList.Items.Add(actionPanel);
                }                
            }
            else
            {
                ActionsSection.Visibility = Visibility.Collapsed;
            }
        }

        private string FormatAbilityScore(int score)
        {
            int mod = (score - 10) / 2;
            string modStr = mod >= 0 ? $"+{mod}" : mod.ToString();
            return $"{score} ({modStr})";
        }

        private void HideCreatureDetails()
        {
            if (TxtNoSelection != null)
                TxtNoSelection.Visibility = Visibility.Visible;
            if (CreatureDetails != null)
                CreatureDetails.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Add to Map

        private void CreatureTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_selectedCreature != null)
            {
                AddCreatureToMap(_selectedCreature);
            }
        }

        private void BtnAddToMap_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature != null)
            {
                AddCreatureToMap(_selectedCreature);
            }
        }

        private void AddCreatureToMap(Token prototype)
        {
            if (_vm == null || prototype == null) return;

            string uniqueName = GenerateUniqueName(prototype.Name);

            var placed = CloneToken(prototype);
            placed.Name = uniqueName;

            var mw = Application.Current?.MainWindow as Window;
            if (mw != null)
            {
                var battleGrid = mw.FindName("BattleGrid") as Controls.BattleGridControl;
                if (battleGrid != null)
                {
                    var centerScreen = new Point(battleGrid.ActualWidth / 2.0, battleGrid.ActualHeight / 2.0);
                    var world = battleGrid.ScreenToWorldPublic(centerScreen);
                    int gx = (int)Math.Floor(world.X / battleGrid.GridCellSize);
                    int gy = (int)Math.Floor(world.Y / battleGrid.GridCellSize);
                    placed.GridX = gx;
                    placed.GridY = gy;
                }
            }

            _vm.Tokens.Add(placed);
            UndoManager.Record(new TokenAddAction(_vm, placed));

            var w = Window.GetWindow(this);
            w?.Close();
        }

        private string GenerateUniqueName(string baseName)
        {
            if (_vm == null || string.IsNullOrEmpty(baseName)) return baseName ?? "Creature";

            var existingNames = _vm.Tokens
                .Where(t => t.Name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Name)
                .ToList();

            if (existingNames.Count == 0)
                return baseName;

            int maxNumber = 0;

            foreach (var name in existingNames)
            {
                if (name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    maxNumber = Math.Max(maxNumber, 1);
                    continue;
                }

                if (name.Length > baseName.Length && name[baseName.Length] == ' ')
                {
                    var suffix = name.Substring(baseName.Length + 1);
                    if (int.TryParse(suffix, out int num))
                        maxNumber = Math.Max(maxNumber, num);
                }
            }

            return $"{baseName} {maxNumber + 1}";
        }

        private Token CloneToken(Token prototype)
        {
            return new Token
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
                Actions = prototype.Actions != null ? new List<Models.Action>(prototype.Actions) : new List<Models.Action>(),
                BonusActions = prototype.BonusActions != null ? new List<Models.Action>(prototype.BonusActions) : new List<Models.Action>(),
                Reactions = prototype.Reactions != null ? new List<Models.Action>(prototype.Reactions) : new List<Models.Action>(),
                LegendaryActions = prototype.LegendaryActions != null ? new List<Models.Action>(prototype.LegendaryActions) : new List<Models.Action>(),
                Notes = prototype.Notes,
                IconPath = prototype.IconPath,
                SizeInSquares = prototype.SizeInSquares,
                Tags = prototype.Tags != null ? new List<string>(prototype.Tags) : new List<string>()
            };
        }

        #endregion

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var w = Window.GetWindow(this);
            w?.Close();
        }
    }
}