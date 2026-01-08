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

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for CreatureBrowserWindow.xaml
    /// </summary>
    public partial class CreatureBrowserWindow : UserControl
    {
        public MainViewModel vm;
        private ICollectionView _view;

        public CreatureBrowserWindow()
        {
            InitializeComponent();
            Loaded += CreatureBrowserWindow_Loaded;
        }

        private void CreatureBrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            vm = DataContext as MainViewModel;

            if (vm == null) return;

            _view = CollectionViewSource.GetDefaultView(vm.CreatureBank);

            RefreshCategories();
            RefreshTypeList();
            RefreshTagList();

            if (CmbSort.Items.Count > 0 && CmbSort.SelectedIndex < 0)
                CmbSort.SelectedIndex = 0;

            ApplyFilters();
        }

        #region Category, Type, Tag Refresh

        private void RefreshCategories()
        {
            if (!(DataContext is MainViewModel viewModel)) return;

            var uniqueCategories = new HashSet<string> { "All" };
            foreach (var t in viewModel.CreatureBank)
            {
                if (t.Extras.TryGetValue("Category", out var cat))
                    uniqueCategories.Add(cat.ToString());
            }
            CmbCategory.ItemsSource = uniqueCategories.ToList();
            CmbCategory.SelectedIndex = 0;
        }

        private void RefreshTypeList()
        {
            if (vm == null) return;

            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in vm.CreatureBank)
            {
                var type = string.IsNullOrWhiteSpace(t.Type) ? "Uncategorized" : t.Type;
                types.Add(type);
            }
            var list = new List<string> { "All" };
            list.AddRange(types.OrderBy(x => x));
            CmbType.ItemsSource = list;
            CmbType.SelectedIndex = 0;
        }

        private void RefreshTagList()
        {
            if (vm == null) return;

            var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "All" };

            foreach (var token in vm.CreatureBank)
            {
                if (token.Tags != null)
                {
                    foreach (var tag in token.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                            allTags.Add(tag);
                    }
                }
            }

            // If you have a CmbTagFilter combobox, populate it here
            // CmbTagFilter. ItemsSource = allTags. OrderBy(t => t).ToList();
            // CmbTagFilter.SelectedIndex = 0;
        }

        #endregion

        #region Filter & Sort

        private void ApplyFilters()
        {
            if (_view == null) return;
            _view.Filter = new Predicate<object>(FilterToken);
            ApplySort();
            _view.Refresh();
        }

        private bool FilterToken(object obj)
        {
            if (!(obj is Token t)) return false;

            // Name search
            var search = TxtSearch?.Text?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                if (!(t.Name?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                    return false;
            }

            // Type filter
            var selType = CmbType?.SelectedItem as string;
            if (!string.IsNullOrEmpty(selType) && selType != "All")
            {
                var tokenType = string.IsNullOrWhiteSpace(t.Type) ? "Uncategorized" : t.Type;
                if (!string.Equals(tokenType, selType, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // CR filters
            bool hasCrMin = TryParseDouble(TxtCRMin?.Text, out double crMin);
            bool hasCrMax = TryParseDouble(TxtCRMax?.Text, out double crMax);

            if (hasCrMin || hasCrMax)
            {
                double crVal = ParseChallengeRating(t.ChallengeRating);
                if (hasCrMin && !double.IsNaN(crMin) && crVal < crMin) return false;
                if (hasCrMax && !double.IsNaN(crMax) && crVal > crMax) return false;
            }

            // HP filters
            int? hpMin = null;
            int? hpMax = null;
            if (int.TryParse(TxtHPMin?.Text, out int tmpHpMin)) hpMin = tmpHpMin;
            if (int.TryParse(TxtHPMax?.Text, out int tmpHpMax)) hpMax = tmpHpMax;

            if (hpMin.HasValue && t.MaxHP < hpMin.Value) return false;
            if (hpMax.HasValue && t.MaxHP > hpMax.Value) return false;

            return true;
        }

        private void ApplySort()
        {
            if (_view == null) return;

            using (_view.DeferRefresh())
            {
                _view.SortDescriptions.Clear();

                var selected = CmbSort?.SelectedItem as ComboBoxItem;
                var sortTag = selected?.Tag as string ?? "Name";
                bool descending = ToggleSortDesc?.IsChecked == true;

                ListSortDirection direction = descending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;

                switch (sortTag)
                {
                    case "Name":
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.Name), direction));
                        break;
                    case "CR":
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.ChallengeRating), direction));
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.Name), ListSortDirection.Ascending));
                        break;
                    case "MaxHP":
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.MaxHP), direction));
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.Name), ListSortDirection.Ascending));
                        break;
                    default:
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.Name), ListSortDirection.Ascending));
                        break;
                }
            }
        }

        private static bool TryParseDouble(string s, out double v)
        {
            if (double.TryParse(s, out v)) return true;
            v = double.NaN;
            return false;
        }

        private static double ParseChallengeRating(string cr)
        {
            if (string.IsNullOrWhiteSpace(cr)) return double.NaN;
            cr = cr.Trim();
            if (cr.Contains("/"))
            {
                var parts = cr.Split('/');
                if (parts.Length == 2 && double.TryParse(parts[0], out double a) && double.TryParse(parts[1], out double b) && b != 0)
                    return a / b;
            }
            if (double.TryParse(cr, out double d)) return d;
            return double.NaN;
        }

        #endregion

        #region Event Handlers

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SelectedCategory = CmbCategory.SelectedItem as string ?? "All";
                viewModel.RefreshCreatureBankViewAsync();
            }
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void FilterNumeric_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void CmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplySort();
        private void ToggleSort_Checked(object sender, RoutedEventArgs e) => ApplySort();

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SearchText = TxtSearch.Text.Trim();
                viewModel.RefreshCreatureBankViewAsync();
            }
            ApplyFilters();
        }

        private void LvResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LvResults.SelectedItem is Token t)
            {
                SpawnTokenToMap(t);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is Token t)
            {
                SpawnTokenToMap(t);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var w = Window.GetWindow(this);
            w?.Close();
        }

        #endregion

        #region Batch Operations

        private void BtnBatchAddTag_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0 || vm == null) return;

            var allTags = vm.CreatureBank
                .Where(c => c.Tags != null)
                .SelectMany(c => c.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var dialog = new TagInputDialog(allTags)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.SelectedTags.Count > 0)
            {
                int taggedCount = 0;
                foreach (var token in selectedItems)
                {
                    if (token.Tags == null)
                        token.Tags = new List<string>();

                    foreach (var tag in dialog.SelectedTags)
                    {
                        if (!token.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                        {
                            token.Tags.Add(tag);
                            taggedCount++;
                        }
                    }
                }

                RefreshTagList();
                ApplyFilters();

                MessageBox.Show($"Added {dialog.SelectedTags.Count} tag(s) to {selectedItems.Count} creature(s).",
                    "Tags Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnBatchAddToMap_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0 || vm == null) return;

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

            int offsetX = 0;
            var addedTokens = new List<Token>();

            foreach (var prototype in selectedItems)
            {
                var placed = CloneToken(prototype);
                placed.Name = GenerateUniqueName(prototype.Name);
                placed.GridX = baseX + offsetX;
                placed.GridY = baseY;
                offsetX++;

                vm.Tokens.Add(placed);
                addedTokens.Add(placed);
            }

            UndoManager.Record(new BatchTokenAddAction(vm, addedTokens));

            MessageBox.Show($"Added {addedTokens.Count} creatures to the map.",
                "Batch Add", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnBatchDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0 || vm == null) return;

            var result = MessageBox.Show($"Delete {selectedItems.Count} creatures from the bank?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            foreach (var token in selectedItems)
                vm.CreatureBank.Remove(token);

            UndoManager.Record(new BatchRemoveAction(vm, selectedItems));

            MessageBox.Show($"Deleted {selectedItems.Count} creatures.",
                "Batch Delete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Context Menu - Tags

        private void ContextMenu_AddTag_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0 || vm == null) return;

            var allTags = vm.CreatureBank
                .Where(c => c.Tags != null)
                .SelectMany(c => c.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var currentTags = selectedItems
                .Where(t => t.Tags != null)
                .SelectMany(t => t.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var dialog = new TagInputDialog(allTags, currentTags)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var token in selectedItems)
                {
                    if (token.Tags == null)
                        token.Tags = new List<string>();

                    foreach (var tag in dialog.SelectedTags)
                    {
                        if (!token.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                            token.Tags.Add(tag);
                    }
                }

                RefreshTagList();
                ApplyFilters();
            }
        }

        private void ContextMenu_RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0) return;

            var existingTags = selectedItems
                .Where(t => t.Tags != null)
                .SelectMany(t => t.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (existingTags.Count == 0)
            {
                MessageBox.Show("Selected creatures have no tags to remove.",
                    "No Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string tagList = string.Join(", ", existingTags);
            string tagToRemove = ShowSimpleInputDialog($"Available tags: {tagList}\n\nEnter tag to remove:", "Remove Tag");

            if (string.IsNullOrWhiteSpace(tagToRemove)) return;

            foreach (var token in selectedItems)
            {
                var tagMatch = token.Tags?.FirstOrDefault(t =>
                    string.Equals(t, tagToRemove, StringComparison.OrdinalIgnoreCase));
                if (tagMatch != null)
                    token.Tags.Remove(tagMatch);
            }

            RefreshTagList();
            ApplyFilters();
        }

        private void ContextMenu_QuickTag_Favourite(object sender, RoutedEventArgs e) => ApplyQuickTag("favourite");
        private void ContextMenu_QuickTag_Boss(object sender, RoutedEventArgs e) => ApplyQuickTag("boss");
        private void ContextMenu_QuickTag_Monster(object sender, RoutedEventArgs e) => ApplyQuickTag("monster");
        private void ContextMenu_QuickTag_NPC(object sender, RoutedEventArgs e) => ApplyQuickTag("npc");

        private void ApplyQuickTag(string tagName)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0) return;

            foreach (var token in selectedItems)
            {
                if (token.Tags == null)
                    token.Tags = new List<string>();

                if (!token.Tags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
                    token.Tags.Add(tagName);
            }

            RefreshTagList();
            ApplyFilters();
        }

        private void CmbTagFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void ChkOnlyTagged_Changed(object sender, RoutedEventArgs e) => ApplyFilters();

        #endregion

        #region Spawn Token

        private void SpawnTokenToMap(Token prototype)
        {
            if (vm == null || prototype == null) return;

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

            vm.Tokens.Add(placed);
            UndoManager.Record(new TokenAddAction(vm, placed));

            var w = Window.GetWindow(this);
            w?.Close();
        }

        #endregion

        #region Helpers

        private string GenerateUniqueName(string baseName)
        {
            if (vm == null) return baseName;

            var existingNames = vm.Tokens
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

        private string ShowSimpleInputDialog(string prompt, string title)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48))
            };

            var stack = new StackPanel { Margin = new Thickness(15) };
            var label = new TextBlock
            {
                Text = prompt,
                Foreground = System.Windows.Media.Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 15), Padding = new Thickness(5) };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "Cancel", Width = 75 };

            string result = null;
            okButton.Click += (s, args) => { result = textBox.Text; dialog.Close(); };
            cancelButton.Click += (s, args) => { dialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(label);
            stack.Children.Add(textBox);
            stack.Children.Add(buttonPanel);
            dialog.Content = stack;

            dialog.ShowDialog();
            return result;
        }

        #endregion
    }
}