using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DnDBattle.Views
{
    public partial class CreatureBrowserWindow : UserControl
    {
        #region Fields

        private readonly CreatureDatabaseService _dbService;
        private readonly MainViewModel _vm;

        private List<Token> _allCreatures = new List<Token>();
        private List<Token> _filteredCreatures = new List<Token>();
        private List<CreatureSummary> _favorites = new List<CreatureSummary>();
        private List<CreatureCategory> _categories = new List<CreatureCategory>();

        private Token _selectedCreature;
        private string _currentGrouping = "Type";
        private string _currentTypeFilter = "All";
        private string _currentCategory = "All";

        private bool _isInitialized = false;
        private bool _isLoading = false;

        #endregion

        #region Constructor

        public CreatureBrowserWindow()
        {
            InitializeComponent();

            _dbService = new CreatureDatabaseService();

            // Try to get the MainViewModel from the application
            if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
            {
                _vm = vm;
            }
        }

        #endregion

        #region Initialization

        private async void CreatureBrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Initialize database first
                await _dbService.EnsureInitializedAsync();

                // Load types for filter dropdown
                await LoadTypesAsync();

                // Load categories (this will also trigger loading creatures via selection)
                await LoadCategoriesAsync();

                // Load favorites for the favorites panel
                await LoadFavoritesAsync();

                // If no category was auto-selected, load all creatures
                if (_allCreatures == null || _allCreatures.Count == 0)
                {
                    _currentCategory = "dnd5e-srd"; // or "all"
                    await LoadCreaturesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing creature browser: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTypesAsync()
        {
            try
            {
                var types = await _dbService.GetAllTypesAsync();
                CmbTypeFilter.Items.Clear();
                foreach (var type in types)
                {
                    CmbTypeFilter.Items.Add(type);
                }
                CmbTypeFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading types: {ex.Message}");
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                CategoryTree.Items.Clear();

                // Load categories from the database
                _categories = await _dbService.GetCategoriesAsync();

                foreach (var category in _categories.OrderBy(c => c.SortOrder))
                {
                    // Get creature count for this category
                    int count = await _dbService.GetCreatureCountByCategoryAsync(category.Id);

                    // Create the header with icon, name, and count
                    var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };

                    // Icon and Name
                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = $"{category.Icon} {category.Name}",
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    // Count badge (grayed out)
                    if (count > 0)
                    {
                        headerPanel.Children.Add(new TextBlock
                        {
                            Text = $"  {count}",
                            Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)), // Gray
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                    }

                    var item = new TreeViewItem
                    {
                        Header = headerPanel,
                        Tag = category.Id,
                        IsExpanded = false
                    };

                    // Style system categories differently
                    if (category.IsSystem)
                    {
                        item.FontWeight = FontWeights.SemiBold;
                    }

                    CategoryTree.Items.Add(item);
                }

                // Select the first item by default
                if (CategoryTree.Items.Count > 0 && CategoryTree.Items[0] is TreeViewItem firstItem)
                {
                    firstItem.IsSelected = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadCategoriesAsync: {ex.Message}");
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadCreaturesAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                if (TxtCreatureCount != null)
                    TxtCreatureCount.Text = "Loading...";

                System.Diagnostics.Debug.WriteLine($"Loading creatures for category: {_currentCategory}");

                // Load creatures based on current category
                if (string.IsNullOrEmpty(_currentCategory) ||
                    _currentCategory.ToLower() == "all" ||
                    _currentCategory.ToLower() == "dnd5e-srd")
                {
                    // Load all creatures
                    _allCreatures = await _dbService.GetAllCreaturesAsync(limit: 4000);
                }
                else if (_currentCategory.ToLower() == "favorites")
                {
                    // Load favorites
                    var favSummaries = await _dbService.GetFavoritesAsync();
                    _allCreatures = new List<Token>();
                    foreach (var fav in favSummaries)
                    {
                        var creature = await _dbService.GetCreatureByIdAsync(fav.Id);
                        if (creature != null)
                            
                            _allCreatures.Add(creature);
                    }
                }
                else
                {
                    // Load by specific category
                    _allCreatures = await _dbService.GetCreaturesByCategoryAsync(_currentCategory, limit: 2000);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {_allCreatures?.Count ?? 0} creatures");

                _filteredCreatures = _allCreatures?.ToList() ?? new List<Token>();

                // Apply filters
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading creatures: {ex.Message}");
                MessageBox.Show($"Error loading creatures: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadFavoritesAsync()
        {
            try
            {
                _favorites = await _dbService.GetFavoritesAsync();
                FavoritesList.ItemsSource = _favorites;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading favorites: {ex.Message}");
            }
        }

        #endregion

        #region Filtering

        private void ApplyFilters()
        {
            if (!_isInitialized || _allCreatures == null) return;

            var filtered = _allCreatures.AsEnumerable();

            // Search filter
            var search = TxtSearch?.Text?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(c =>
                    c.Name?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // CR Min filter
            if (double.TryParse(TxtCRMin?.Text, out double crMin))
            {
                filtered = filtered.Where(c => ParseCR(c.ChallengeRating) >= crMin);
            }

            // CR Max filter
            if (double.TryParse(TxtCRMax?.Text, out double crMax))
            {
                filtered = filtered.Where(c => ParseCR(c.ChallengeRating) <= crMax);
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

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            _currentTypeFilter = CmbTypeFilter.SelectedItem?.ToString() ?? "All";
            _ = LoadCreaturesAsync();
        }

        private void CmbGroupBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            var selected = CmbGroupBy.SelectedItem as ComboBoxItem;
            _currentGrouping = selected?.Tag?.ToString() ?? "Type";
            UpdateTreeView();
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            TxtSearch.Text = "";
            TxtCRMin.Text = "";
            TxtCRMax.Text = "";
            CmbTypeFilter.SelectedIndex = 0;
            CmbGroupBy.SelectedIndex = 1; // "Type"

            ApplyFilters();
        }

        #endregion

        #region TreeView Management

        private void UpdateTreeView()
        {
            if (CreatureTree == null) return;

            CreatureTree.Items.Clear();

            if (_filteredCreatures == null || _filteredCreatures.Count == 0)
                return;

            if (string.IsNullOrEmpty(_currentGrouping) || _currentGrouping == "None")
            {
                // Flat list - no grouping
                foreach (var creature in _filteredCreatures.OrderBy(c => c.Name))
                {
                    CreatureTree.Items.Add(CreateCreatureTreeItem(creature));
                }
            }
            else
            {
                // Grouped view
                var groups = GroupCreatures(_filteredCreatures, _currentGrouping);

                var sortedGroups = groups
                    .OrderBy(g => g.Key == "Unknown" ? 1 : 0)
                    .ThenBy(g => g.Key)
                    .ToList();

                foreach (var group in sortedGroups)
                {
                    var groupItem = new TreeViewItem
                    {
                        Header = CreateGroupHeader(group.Key, group.Value.Count),
                        IsExpanded = false, // START COLLAPSED
                        Tag = "Group"
                    };

                    foreach (var creature in group.Value.OrderBy(c => c.Name))
                    {
                        groupItem.Items.Add(CreateCreatureTreeItem(creature));
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

                if (key.Length > 0)
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

            return groupBy switch
            {
                "Type" => string.IsNullOrWhiteSpace(creature.Type) ? "Unknown" : creature.Type.Trim(),
                "ChallengeRating" => string.IsNullOrWhiteSpace(creature.ChallengeRating) ? "Unknown" : $"CR {creature.ChallengeRating.Trim()}",
                "Size" => string.IsNullOrWhiteSpace(creature.Size) ? "Unknown" : creature.Size.Trim(),
                _ => "All"
            };
        }

        private StackPanel CreateGroupHeader(string groupName, int count)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            panel.Children.Add(new TextBlock
            {
                Text = groupName,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(79, 195, 247)),
                VerticalAlignment = VerticalAlignment.Center
            });

            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(62, 62, 66)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(8, 0, 0, 0),
                Child = new TextBlock
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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Name only
            var nameText = new TextBlock
            {
                Text = creature.Name,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 12
            };
            Grid.SetColumn(nameText, 0);
            grid.Children.Add(nameText);

            // CR badge
            var crBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(62, 62, 66)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            crBorder.Child = new TextBlock
            {
                Text = $"CR {creature.ChallengeRating ?? "?"}",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 183, 77)),
                FontSize = 10
            };
            Grid.SetColumn(crBorder, 1);
            grid.Children.Add(crBorder);

            return new TreeViewItem
            {
                Header = grid,
                Tag = creature
            };
        }

        private void UpdateCreatureCount()
        {
            if (TxtCreatureCount != null)
            {
                int filtered = _filteredCreatures?.Count ?? 0;
                int total = _allCreatures?.Count ?? 0;
                TxtCreatureCount.Text = $"Showing {filtered} of {total} creatures";
            }
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

        private async void CategoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is string categoryId)
            {
                System.Diagnostics.Debug.WriteLine($"Category selected: {categoryId}");
                _currentCategory = categoryId;
                await LoadCreaturesAsync();
            }
        }

        private async void ShowCreatureDetails(Token creature)
        {
            try
            {
                if (creature == null) return;

                var actions = await _dbService.GetActionsAsync(creature.Id.ToString(), "Action");

                // Show details panel
                TxtNoSelection.Visibility = Visibility.Collapsed;
                CreatureDetails.Visibility = Visibility.Visible;

                // Set basic info immediately
                TxtCreatureName.Text = creature.Name ?? "Unknown";
                TxtCreatureSubtitle.Text = $"{creature.Size} {creature.Type}, {creature.Alignment}".Trim(' ', ',');

                TxtAC.Text = creature.ArmorClass.ToString();
                TxtHP.Text = creature.MaxHP.ToString();
                TxtCR.Text = creature.ChallengeRating ?? "?";

                TxtStr.Text = creature.Str.ToString();
                TxtDex.Text = creature.Dex.ToString();
                TxtCon.Text = creature.Con.ToString();
                TxtInt.Text = creature.Int.ToString();
                TxtWis.Text = creature.Wis.ToString();
                TxtCha.Text = creature.Cha.ToString();

                TxtSpeed.Text = creature.Speed ?? "30 ft.";

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

                // Update favorite button - check if creature is in favorites
                bool isFavorite = _favorites?.Any(f => f.Id == creature.Id.ToString()) ?? false;
                BtnFavorite.Content = isFavorite ? "★" : "☆";
                BtnFavorite.Foreground = isFavorite
                    ? new SolidColorBrush(Color.FromRgb(255, 215, 0))
                    : Brushes.Gray;

                // Load image ASYNCHRONOUSLY (only for this one creature!)
                await LoadCreatureImageAsync(creature);

                ActionsList.ItemsSource = creature.Actions;
            }
            catch (Exception ex)
            {

            }
        }

        private async Task LoadCreatureImageAsync(Token creature)
        {
            try
            {
                // Show a placeholder immediately
                ImgCreatureDetail.Source = CreatureImageService.GeneratePlaceholderToken(
                    creature.Name, creature.Type, creature.Size, creature.ChallengeRating);

                // Then try to fetch real image in background
                var image = await CreatureImageService.GetCreatureImageAsync(
                    creature.Name,
                    creature.Type,
                    creature.Size,
                    creature.ChallengeRating,
                    creature.IconPath);

                // Only update if this creature is still selected
                if (_selectedCreature?.Id == creature.Id)
                {
                    ImgCreatureDetail.Source = image;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading creature image: {ex.Message}");
            }
        }

        private string FormatAbilityScore(int score)
        {
            int mod = (score - 10) / 2;
            string modStr = mod >= 0 ? $"+{mod}" : mod.ToString();
            return $"{score}\n({modStr})";
        }

        private void HideCreatureDetails()
        {
            TxtNoSelection.Visibility = Visibility.Visible;
            CreatureDetails.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Favorites

        private void Favorite_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is CreatureSummary summary)
            {
                var creature = _allCreatures.FirstOrDefault(c => c.Id.ToString() == summary.Id);
                if (creature != null)
                {
                    _selectedCreature = creature;
                    ShowCreatureDetails(creature);

                    // Find and select in tree
                    SelectCreatureInTree(creature);
                }
            }
        }

        private void SelectCreatureInTree(Token creature)
        {
            foreach (var item in CreatureTree.Items)
            {
                if (item is TreeViewItem tvi)
                {
                    if (tvi.Tag is Token t && t.Id == creature.Id)
                    {
                        tvi.IsSelected = true;
                        return;
                    }

                    // Check children (for grouped view)
                    foreach (var child in tvi.Items)
                    {
                        if (child is TreeViewItem childTvi && childTvi.Tag is Token ct && ct.Id == creature.Id)
                        {
                            childTvi.IsSelected = true;
                            tvi.IsExpanded = true;
                            return;
                        }
                    }
                }
            }
        }

        private async void BtnFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature == null) return;

            await _dbService.ToggleFavoriteAsync(_selectedCreature.Id.ToString());
            await LoadFavoritesAsync();

            // Update button
            bool isFavorite = await _dbService.IsFavoriteAsync(_selectedCreature.Id.ToString());
            BtnFavorite.Content = isFavorite ? "★" : "☆";
            BtnFavorite.Foreground = isFavorite ? Brushes.Gold : Brushes.Gray;
        }

        #endregion

        #region Actions

        private void BtnAddToMap_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature == null || _vm == null) return;

            var newToken = CloneCreatureForMap(_selectedCreature);
            _vm.Tokens.Add(newToken);

            // Add some way to indicate a creature has been added
        }

        private Token CloneCreatureForMap(Token source)
        {
            return new Token
            {
                Id = Guid.NewGuid(),
                Name = source.Name,
                Size = source.Size,
                Type = source.Type,
                Alignment = source.Alignment,
                ChallengeRating = source.ChallengeRating,
                ArmorClass = source.ArmorClass,
                MaxHP = source.MaxHP,
                HP = source.MaxHP,
                HitDice = source.HitDice,
                InitiativeModifier = source.InitiativeModifier,
                Speed = source.Speed,
                Str = source.Str,
                Dex = source.Dex,
                Con = source.Con,
                Int = source.Int,
                Wis = source.Wis,
                Cha = source.Cha,
                SizeInSquares = source.SizeInSquares,
                GridX = 0,
                GridY = 0
            };
        }

        private void EditCreature_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature == null) return;
            MessageBox.Show("Edit creature functionality coming soon!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DuplicateCreature_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature == null) return;
            MessageBox.Show("Duplicate creature functionality coming soon!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteCreature_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCreature == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{_selectedCreature.Name}' from the database?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _dbService.DeleteCreatureAsync(_selectedCreature.Id.ToString());
                await LoadCreaturesAsync();
                await LoadFavoritesAsync();
                HideCreatureDetails();
            }
        }

        private void CreateCreature_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Create creature functionality coming soon!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Import

        private async void ImportJson_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON Files|*.json",
                Title = "Import Creatures from JSON"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    ImportProgressOverlay.Visibility = Visibility.Visible;
                    ImportProgressText.Text = "Importing...";

                    var count = await _dbService.ImportFromJsonFileAsync(dlg.FileName);

                    ImportProgressText.Text = $"Imported {count} creatures!";
                    await Task.Delay(1500);

                    await LoadCreaturesAsync();
                    await LoadCategoriesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ImportProgressOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void ImportFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select a folder containing JSON files",
                Filter = "Folder|*.folder",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dlg.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    await ImportCreaturesWithProgressAsync(folderPath);
                }
            }
        }

        private async Task ImportCreaturesWithProgressAsync(string folderPath)
        {
            ImportProgressOverlay.Visibility = Visibility.Visible;
            ImportProgressBar.Value = 0;
            ImportProgressText.Text = "Scanning folder...";
            ImportProgressDetail.Text = "";

            try
            {
                var jsonFiles = System.IO.Directory.GetFiles(folderPath, "*.json");
                int totalFiles = jsonFiles.Length;
                int processedFiles = 0;
                int totalCreatures = 0;

                foreach (var file in jsonFiles)
                {
                    var fileName = System.IO.Path.GetFileName(file);
                    ImportProgressText.Text = $"Importing {fileName}...";
                    ImportProgressDetail.Text = $"File {processedFiles + 1} of {totalFiles}";

                    try
                    {
                        var count = await _dbService.ImportFromJsonFileAsync(file);
                        totalCreatures += count;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error importing {fileName}: {ex.Message}");
                    }

                    processedFiles++;
                    ImportProgressBar.Value = (double)processedFiles / totalFiles * 100;

                    await Task.Delay(10); // Allow UI to update
                }

                ImportProgressText.Text = "Import complete!";
                ImportProgressDetail.Text = $"Imported {totalCreatures} creatures from {processedFiles} files";

                await Task.Delay(2000);

                await LoadCreaturesAsync();
                await LoadCategoriesAsync();
                await LoadFavoritesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ImportProgressOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            await LoadCreaturesAsync();
            await LoadFavoritesAsync();
        }

        #endregion

        #region Category Management

        private async Task LoadCreaturesByCategoryAsync(string categoryId)
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                if (TxtCreatureCount != null)
                    TxtCreatureCount.Text = "Loading...";

                // Load creatures for this category
                _allCreatures = await _dbService.GetCreaturesByCategoryAsync(categoryId);
                _filteredCreatures = _allCreatures.ToList();

                // Apply any active filters
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading creatures: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void NewCategory_Click(object sender, RoutedEventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the name for the new category:",
                "New Category",
                "My Category");

            if (string.IsNullOrWhiteSpace(name)) return;

            // Generate a URL-friendly ID
            string id = name.ToLower()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("\"", "");

            // Ask for an icon (optional)
            string icon = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter an emoji icon for the category (or leave default):",
                "Category Icon",
                "📁");

            if (string.IsNullOrWhiteSpace(icon)) icon = "📁";

            // Get the next sort order
            int sortOrder = _categories.Count > 0 ? _categories.Max(c => c.SortOrder) + 1 : 10;

            bool success = await _dbService.AddCategoryAsync(id, name, icon, null, sortOrder);

            if (success)
            {
                await LoadCategoriesAsync();
                MessageBox.Show($"Category '{name}' created!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to create category. It may already exist.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void RenameCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryTree.SelectedItem is not TreeViewItem selectedItem || selectedItem.Tag is not string categoryId)
            {
                MessageBox.Show("Please select a category to rename.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Check if it's a system category
            var category = _categories.FirstOrDefault(c => c.Id == categoryId);
            if (category?.IsSystem == true)
            {
                MessageBox.Show("System categories cannot be renamed.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string currentName = category?.Name ?? "";
            string newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the new name for this category:",
                "Rename Category",
                currentName);

            if (string.IsNullOrWhiteSpace(newName) || newName == currentName) return;

            bool success = await _dbService.RenameCategoryAsync(categoryId, newName);

            if (success)
            {
                await LoadCategoriesAsync();
            }
            else
            {
                MessageBox.Show("Failed to rename category.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryTree.SelectedItem is not TreeViewItem selectedItem || selectedItem.Tag is not string categoryId)
            {
                MessageBox.Show("Please select a category to delete.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Check if it's a system category
            var category = _categories.FirstOrDefault(c => c.Id == categoryId);
            if (category?.IsSystem == true)
            {
                MessageBox.Show("System categories cannot be deleted.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the category '{category?.Name}'?\n\n" +
                "Creatures in this category will be moved to 'Custom'.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            bool success = await _dbService.DeleteCategoryAsync(categoryId);

            if (success)
            {
                await LoadCategoriesAsync();
                await LoadCreaturesByCategoryAsync("custom");
            }
            else
            {
                MessageBox.Show("Failed to delete category.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion
    }
}