using DnDBattle.Models;
using DnDBattle.ViewModels;
using DnDBattle.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DnDBattle.Views
{
    public partial class CreatureBrowserWindow : UserControl
    {
        private MainViewModel _vm;
        private List<Token> _allCreatures = new List<Token>();
        private List<Token> _filteredCreatures = new List<Token>();
        private List<CreatureCategory> _categories = new List<CreatureCategory>();
        private Token _selectedCreature;
        private CreatureCategory _selectedCategory;
        private bool _isInitialized = false;

        public CreatureBrowserWindow()
        {
            InitializeComponent();
            Loaded += CreatureBrowserWindow_Loaded;
        }

        private async void CreatureBrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as MainViewModel;
            _isInitialized = true;

            await LoadCategoriesAsync();
            await LoadCreaturesAsync();
        }

        #region Data Loading

        private async System.Threading.Tasks.Task LoadCategoriesAsync()
        {
            try
            {
                using (var db = new CreatureDatabaseService())
                {
                    _categories = await db.GetAllCategoriesAsync();
                }

                BuildCategoryTree();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private void BuildCategoryTree()
        {
            CategoryTree.Items.Clear();

            // Add "All Creatures" node
            var allNode = new TreeViewItem
            {
                Header = CreateCategoryHeader("📋", "All Creatures", _allCreatures.Count),
                Tag = "all",
                IsExpanded = true
            };
            CategoryTree.Items.Add(allNode);

            // Add Favorites
            var favCategory = _categories.FirstOrDefault(c => c.Id == "favorites");
            if (favCategory != null)
            {
                var favNode = new TreeViewItem
                {
                    Header = CreateCategoryHeader(favCategory.Icon, favCategory.Name, favCategory.CreatureCount),
                    Tag = favCategory,
                    IsExpanded = true
                };
                CategoryTree.Items.Add(favNode);
            }

            // Add system categories
            foreach (var cat in _categories.Where(c => c.IsSystem && c.Id != "favorites").OrderBy(c => c.SortOrder))
            {
                var node = new TreeViewItem
                {
                    Header = CreateCategoryHeader(cat.Icon, cat.Name, cat.CreatureCount),
                    Tag = cat,
                    IsExpanded = false
                };

                // Add subcategories
                var subCats = _categories.Where(c => c.ParentId == cat.Id);
                foreach (var subCat in subCats)
                {
                    node.Items.Add(new TreeViewItem
                    {
                        Header = CreateCategoryHeader(subCat.Icon, subCat.Name, subCat.CreatureCount),
                        Tag = subCat
                    });
                }

                CategoryTree.Items.Add(node);
            }

            // Add user categories
            var userCats = _categories.Where(c => !c.IsSystem).ToList();
            if (userCats.Any())
            {
                var userNode = new TreeViewItem
                {
                    Header = CreateCategoryHeader("📁", "My Categories", userCats.Sum(c => c.CreatureCount)),
                    IsExpanded = true,
                    Tag = "user-parent"
                };

                foreach (var cat in userCats)
                {
                    userNode.Items.Add(new TreeViewItem
                    {
                        Header = CreateCategoryHeader(cat.Icon, cat.Name, cat.CreatureCount),
                        Tag = cat
                    });
                }

                CategoryTree.Items.Add(userNode);
            }
        }

        private StackPanel CreateCategoryHeader(string icon, string name, int count)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            panel.Children.Add(new TextBlock
            {
                Text = icon,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            panel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(8, 0, 0, 0),
                Child = new TextBlock
                {
                    Text = count.ToString(),
                    Foreground = Brushes.Gray,
                    FontSize = 11
                }
            });

            return panel;
        }

        private async System.Threading.Tasks.Task LoadCreaturesAsync()
        {
            try
            {
                // First check if ViewModel already has creatures loaded
                if (_vm?.CreatureBank != null && _vm.CreatureBank.Count > 0)
                {
                    _allCreatures = _vm.CreatureBank.ToList();
                }
                else
                {
                    using (var db = new CreatureDatabaseService())
                    {
                        _allCreatures = await db.SearchCreaturesAsync(sortBy: "Name", limit: 10000);
                    }
                }

                // Mark favorites
                using (var db = new CreatureDatabaseService())
                {
                    foreach (var creature in _allCreatures)
                    {
                        creature.IsFavorite = await db.IsCreatureFavoriteAsync(creature.Id.ToString());
                    }
                }

                _filteredCreatures = new List<Token>(_allCreatures);
                PopulateTypeFilter();
                RefreshCreatureList();
                UpdateCreatureCount();
                BuildCategoryTree(); // Rebuild to update counts
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading creatures: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateTypeFilter()
        {
            var types = _allCreatures
                .Where(c => !string.IsNullOrEmpty(c.Type))
                .Select(c => c.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            CmbTypeFilter.Items.Clear();
            CmbTypeFilter.Items.Add(new ComboBoxItem { Content = "All Types", IsSelected = true });
            foreach (var type in types)
            {
                CmbTypeFilter.Items.Add(new ComboBoxItem { Content = type });
            }
        }

        private void RefreshCreatureList()
        {
            CreatureList.ItemsSource = null;
            CreatureList.ItemsSource = _filteredCreatures;
        }

        private void UpdateCreatureCount()
        {
            TxtCreatureCount.Text = $"Showing {_filteredCreatures.Count} of {_allCreatures.Count} creatures";
        }

        private async void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            await LoadCategoriesAsync();
            await LoadCreaturesAsync();
            MessageBox.Show($"Reloaded {_allCreatures.Count} creatures.", "Reload Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Category Events

        private async void CategoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item)
            {
                if (item.Tag is string tagStr)
                {
                    if (tagStr == "all")
                    {
                        _selectedCategory = null;
                        _filteredCreatures = new List<Token>(_allCreatures);
                    }
                    else if (tagStr == "user-parent")
                    {
                        return; // Parent node, ignore
                    }
                }
                else if (item.Tag is CreatureCategory category)
                {
                    _selectedCategory = category;

                    try
                    {
                        using (var db = new CreatureDatabaseService())
                        {
                            _filteredCreatures = await db.GetCreaturesByCategoryAsync(category.Id);

                            // Mark favorites
                            foreach (var creature in _filteredCreatures)
                            {
                                creature.IsFavorite = await db.IsCreatureFavoriteAsync(creature.Id.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading category creatures: {ex.Message}");
                        _filteredCreatures = new List<Token>();
                    }
                }
                else
                {
                    return; // Unknown tag, ignore
                }

                ApplyFilters();
                UpdateCategoryButtons();
            }
        }

        private void UpdateCategoryButtons()
        {
            bool canModify = _selectedCategory != null && !_selectedCategory.IsSystem;
            BtnRenameCategory.IsEnabled = canModify;
            BtnDeleteCategory.IsEnabled = canModify;
        }

        private async void NewCategory_Click(object sender, RoutedEventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter category name:", "New Category", "My Category");

            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                using (var db = new CreatureDatabaseService())
                {
                    await db.AddCategoryAsync(name);
                }

                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating category: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RenameCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null || _selectedCategory.IsSystem) return;

            string newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new name:", "Rename Category", _selectedCategory.Name);

            if (string.IsNullOrWhiteSpace(newName)) return;

            try
            {
                using (var db = new CreatureDatabaseService())
                {
                    await db.RenameCategoryAsync(_selectedCategory.Id, newName);
                }

                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming category: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == null || _selectedCategory.IsSystem) return;

            var result = MessageBox.Show(
                $"Delete category '{_selectedCategory.Name}'?\n\nCreatures in this category will NOT be deleted.",
                "Delete Category",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var db = new CreatureDatabaseService())
                {
                    await db.DeleteCategoryAsync(_selectedCategory.Id);
                }

                _selectedCategory = null;
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting category:  {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Filtering

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
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
            if (CmbTypeFilter.Items.Count > 0)
                CmbTypeFilter.SelectedIndex = 0;

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (!_isInitialized) return;

            var filtered = (_selectedCategory != null ? _filteredCreatures : _allCreatures).AsEnumerable();

            // Name search
            var search = TxtSearch?.Text?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(c =>
                    c.Name?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.Type?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // CR filter
            if (double.TryParse(TxtCRMin?.Text, out double crMin))
            {
                filtered = filtered.Where(c => ParseCR(c.ChallengeRating) >= crMin);
            }
            if (double.TryParse(TxtCRMax?.Text, out double crMax))
            {
                filtered = filtered.Where(c => ParseCR(c.ChallengeRating) <= crMax);
            }

            // Type filter
            var selectedType = (CmbTypeFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrEmpty(selectedType) && selectedType != "All Types")
            {
                filtered = filtered.Where(c =>
                    string.Equals(c.Type, selectedType, StringComparison.OrdinalIgnoreCase));
            }

            var resultList = filtered.OrderBy(c => c.Name).ToList();

            CreatureList.ItemsSource = null;
            CreatureList.ItemsSource = resultList;

            TxtCreatureCount.Text = $"Showing {resultList.Count} of {_allCreatures.Count} creatures";
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

        #endregion

        #region Creature Selection & Details

        private void CreatureList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCreature = CreatureList.SelectedItem as Token;

            if (_selectedCreature != null)
            {
                ShowCreatureDetails(_selectedCreature);
            }
            else
            {
                HideCreatureDetails();
            }
        }

        private void ShowCreatureDetails(Token creature)
        {
            TxtNoSelection.Visibility = Visibility.Collapsed;
            CreatureDetails.Visibility = Visibility.Visible;

            TxtCreatureName.Text = creature.Name ?? "Unknown";
            TxtCreatureSubtitle.Text = $"{creature.Size ?? ""} {creature.Type ?? ""}".Trim();
            if (!string.IsNullOrEmpty(creature.Alignment))
                TxtCreatureSubtitle.Text += $", {creature.Alignment}";

            TxtAC.Text = creature.ArmorClass.ToString();
            TxtHP.Text = creature.MaxHP.ToString();
            TxtCR.Text = creature.ChallengeRating ?? "—";
            TxtSpeed.Text = creature.Speed ?? "30 ft. ";

            // Update favorite button
            BtnFavorite.Foreground = creature.IsFavorite
                ? Brushes.Gold
                : Brushes.Gray;

            // Ability scores
            AbilityScoresGrid.Children.Clear();
            var abilities = new[]
            {
                ("STR", creature.Str),
                ("DEX", creature.Dex),
                ("CON", creature.Con),
                ("INT", creature.Int),
                ("WIS", creature. Wis),
                ("CHA", creature.Cha)
            };

            foreach (var (name, score) in abilities)
            {
                var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(2) };

                panel.Children.Add(new TextBlock
                {
                    Text = name,
                    Foreground = Brushes.Gray,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                panel.Children.Add(new TextBlock
                {
                    Text = score.ToString(),
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                int mod = (score - 10) / 2;
                string modStr = mod >= 0 ? $"+{mod}" : mod.ToString();
                panel.Children.Add(new TextBlock
                {
                    Text = modStr,
                    Foreground = Brushes.Gray,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                AbilityScoresGrid.Children.Add(panel);
            }

            // Traits
            if (!string.IsNullOrWhiteSpace(creature.Traits))
            {
                TraitsSection.Visibility = Visibility.Visible;
                TxtTraits.Text = creature.Traits;
            }
            else
            {
                TraitsSection.Visibility = Visibility.Collapsed;
            }

            // Actions
            if (creature.Actions != null && creature.Actions.Count > 0)
            {
                ActionsSection.Visibility = Visibility.Visible;
                ActionsList.Items.Clear();

                foreach (var action in creature.Actions)
                {
                    var actionPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

                    actionPanel.Children.Add(new TextBlock
                    {
                        Text = action.Name ?? "Action",
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    });

                    if (!string.IsNullOrEmpty(action.Description))
                    {
                        actionPanel.Children.Add(new TextBlock
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

        private void HideCreatureDetails()
        {
            TxtNoSelection.Visibility = Visibility.Visible;
            CreatureDetails.Visibility = Visibility.Collapsed;
        }

        private void CreatureList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_selectedCreature != null)
            {
                AddCreatureToMap(_selectedCreature);
            }
        }

        #endregion

        #region Favorites

        private async void ToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Token creature)
            {
                await ToggleFavoriteAsync(creature);
            }
        }

        private async void BtnFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature != null)
            {
                await ToggleFavoriteAsync(_selectedCreature);

                // Update button color
                BtnFavorite.Foreground = _selectedCreature.IsFavorite
                    ? Brushes.Gold
                    : Brushes.Gray;
            }
        }

        private async System.Threading.Tasks.Task ToggleFavoriteAsync(Token creature)
        {
            try
            {
                using (var db = new CreatureDatabaseService())
                {
                    await db.ToggleFavoriteAsync(creature.Id.ToString());
                    creature.IsFavorite = !creature.IsFavorite;
                }

                // Refresh the list to update star colors
                RefreshCreatureList();

                // Refresh categories to update counts
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling favorite: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Creature Actions

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

            // Position at center of battle grid
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

            // Close the browser window
            var w = Window.GetWindow(this);
            w?.Close();
        }

        private string GenerateUniqueName(string baseName)
        {
            if (_vm == null || string.IsNullOrEmpty(baseName)) return baseName ?? "Creature";

            var existingNames = _vm.Tokens
                .Where(t => t.Name != null && t.Name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
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
            if (prototype == null) return new Token { Name = "Unknown" };

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

        #region Create / Edit / Delete Creature

        private async void CreateCreature_Click(object sender, RoutedEventArgs e)
        {
            var editor = new CreatureEditorWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (editor.ShowDialog() == true && editor.ResultCreature != null)
            {
                await LoadCreaturesAsync();

                // Select the new creature
                var newCreature = _allCreatures.FirstOrDefault(c => c.Id == editor.ResultCreature.Id);
                if (newCreature != null)
                {
                    CreatureList.SelectedItem = newCreature;
                }
            }
        }

        private async void EditCreature_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature == null) return;

            var editor = new CreatureEditorWindow(_selectedCreature)
            {
                Owner = Window.GetWindow(this)
            };

            if (editor.ShowDialog() == true)
            {
                await LoadCreaturesAsync();

                // Re-select the edited creature
                var editedCreature = _allCreatures.FirstOrDefault(c => c.Id == _selectedCreature.Id);
                if (editedCreature != null)
                {
                    CreatureList.SelectedItem = editedCreature;
                    ShowCreatureDetails(editedCreature);
                }
            }
        }

        private async void DuplicateCreature_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature == null) return;

            var duplicate = CloneToken(_selectedCreature);
            duplicate.Name = $"{_selectedCreature.Name} (Copy)";

            try
            {
                using (var db = new CreatureDatabaseService())
                {
                    await db.AddCustomCreatureAsync(duplicate, "custom");
                }

                await LoadCreaturesAsync();

                // Select the duplicate
                var newCreature = _allCreatures.FirstOrDefault(c => c.Name == duplicate.Name);
                if (newCreature != null)
                {
                    CreatureList.SelectedItem = newCreature;
                }

                MessageBox.Show($"Created duplicate: {duplicate.Name}", "Duplicate Created",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error duplicating creature: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteCreature_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{_selectedCreature.Name}'?\n\nThis cannot be undone.",
                "Delete Creature",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var db = new CreatureDatabaseService())
                {
                    await db.DeleteCreatureAsync(_selectedCreature.Id.ToString());
                }

                _selectedCreature = null;
                HideCreatureDetails();
                await LoadCreaturesAsync();

                MessageBox.Show("Creature deleted.", "Deleted",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting creature: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Import Single JSON

        private async void ImportSingleJson_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Import Creature from JSON",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Multiselect = false
            };

            if (openDialog.ShowDialog() != true) return;

            try
            {
                var json = await File.ReadAllTextAsync(openDialog.FileName);

                // Try to parse as single creature or array
                Token creature = null;

                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        // Take first creature from array
                        creature = ParseCreatureFromJson(root[0]);
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        creature = ParseCreatureFromJson(root);
                    }
                }

                if (creature == null)
                {
                    MessageBox.Show("Could not parse creature from JSON file.", "Import Failed",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Add to database
                using (var db = new CreatureDatabaseService())
                {
                    await db.AddCustomCreatureAsync(creature, "custom");
                }

                await LoadCreaturesAsync();

                // Select the imported creature
                var imported = _allCreatures.FirstOrDefault(c => c.Id == creature.Id);
                if (imported != null)
                {
                    CreatureList.SelectedItem = imported;
                }

                MessageBox.Show($"Successfully imported:  {creature.Name}", "Import Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing creature: {ex.Message}", "Import Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Token ParseCreatureFromJson(JsonElement element)
        {
            var creature = new Token
            {
                Id = Guid.NewGuid()
            };

            if (element.TryGetProperty("Name", out var nameProp) || element.TryGetProperty("name", out nameProp))
                creature.Name = nameProp.GetString();

            if (element.TryGetProperty("Type", out var typeProp) || element.TryGetProperty("type", out typeProp))
                creature.Type = typeProp.GetString();

            if (element.TryGetProperty("Size", out var sizeProp) || element.TryGetProperty("size", out sizeProp))
                creature.Size = sizeProp.GetString();

            if (element.TryGetProperty("Alignment", out var alignProp) || element.TryGetProperty("alignment", out alignProp))
                creature.Alignment = alignProp.GetString();

            if (element.TryGetProperty("ArmorClass", out var acProp) || element.TryGetProperty("ac", out acProp))
            {
                if (acProp.ValueKind == JsonValueKind.Number)
                    creature.ArmorClass = acProp.GetInt32();
                else if (int.TryParse(acProp.GetString(), out int ac))
                    creature.ArmorClass = ac;
            }

            if (element.TryGetProperty("MaxHP", out var hpProp) || element.TryGetProperty("hp", out hpProp))
            {
                if (hpProp.ValueKind == JsonValueKind.Number)
                {
                    creature.MaxHP = hpProp.GetInt32();
                    creature.HP = creature.MaxHP;
                }
                else if (int.TryParse(hpProp.GetString(), out int hp))
                {
                    creature.MaxHP = hp;
                    creature.HP = hp;
                }
            }

            if (element.TryGetProperty("ChallengeRating", out var crProp) || element.TryGetProperty("cr", out crProp))
                creature.ChallengeRating = crProp.GetString();

            if (element.TryGetProperty("Speed", out var speedProp) || element.TryGetProperty("speed", out speedProp))
                creature.Speed = speedProp.GetString();

            // Ability scores
            if (element.TryGetProperty("Str", out var strProp)) creature.Str = strProp.GetInt32();
            if (element.TryGetProperty("Dex", out var dexProp)) creature.Dex = dexProp.GetInt32();
            if (element.TryGetProperty("Con", out var conProp)) creature.Con = conProp.GetInt32();
            if (element.TryGetProperty("Int", out var intProp)) creature.Int = intProp.GetInt32();
            if (element.TryGetProperty("Wis", out var wisProp)) creature.Wis = wisProp.GetInt32();
            if (element.TryGetProperty("Cha", out var chaProp)) creature.Cha = chaProp.GetInt32();

            if (element.TryGetProperty("Traits", out var traitsProp))
                creature.Traits = traitsProp.GetString();

            return creature;
        }

        #endregion
    }
}