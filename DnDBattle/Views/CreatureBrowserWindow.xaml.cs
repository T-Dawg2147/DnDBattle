using DnDBattle.Models;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedCategory = CmbCategory.SelectedItem as string ?? "All";
                vm.RefreshCreatureBankView();
            }
        }

        private void BtnBatchAddTag_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0) return;

            string tagName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter tag name: ", 
                "Add Tag", 
                "favourite");

            if (string.IsNullOrWhiteSpace(tagName)) return;

            int taggedCount = 0;
            foreach (var token in selectedItems)
            {
                if (!token.Tags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
                {
                    token.Tags.Add(tagName);
                    taggedCount++;
                }                    
            }

            MessageBox.Show(
                $"Tagged {taggedCount} creatures with '{tagName}',", 
                "Batch Tag",
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private void BtnBatchAddToMap_Click(object sender, RoutedEventArgs e)
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
            }

            RefreshTagList();
            ApplyFilters();

            MessageBox.Show(
                $"Added {dialog.SelectedTags.Count} tag(s) to {selectedItems.Count} creature(s).", 
                "Tags Applied",
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private void BtnBatchDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0 || vm == null) return;

            var result = MessageBox.Show(
                $"Delete {selectedItems.Count} creatures?", 
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            foreach (var token in selectedItems)
                vm.CreatureBank.Remove(token);

            MessageBox.Show(
                $"Deleted {selectedItems.Count} creatures.",
                "Batch Delete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            UndoManager.Record(new BatchRemoveAction(vm, selectedItems));
        }

        private void CmbTagFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ChkOnlyTagged_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ContextMenu_AddTag_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = LvResults.SelectedItems.Cast<Token>().ToList();
            if (selectedItems.Count == 0) return;

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
                        {
                            token.Tags.Add(tag);
                        }
                    }
                }
            }

            RefreshTagList();
            ApplyFilters();
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
                MessageBox.Show("Selected creatures have no tags to remove.", "No Tags",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string tagList = string.Join(", ", existingTags);
            string tagToRemove = Microsoft.VisualBasic.Interaction.InputBox(
                $"Available tags: {tagList}\n\nEnter tag to remove:", "Remove Tag", "");

            if (string.IsNullOrWhiteSpace(tagToRemove)) return;

            foreach (var token in selectedItems)
            {
                var tagMatch = token.Tags?.FirstOrDefault(t =>
                string.Equals(t, tagToRemove, StringComparison.OrdinalIgnoreCase));
                if (tagMatch != null)
                {
                    token.Tags.Remove(tagMatch);
                }
            }

            RefreshTagList();
            ApplyFilters();
        }

        private void ContextMenu_QuickTag_Favourite(object sender, RoutedEventArgs e)
        {
            ApplyQuickTag("favourite");
        }

        private void ContextMenu_QuickTag_Boss(object sender, RoutedEventArgs e)
        {
            ApplyQuickTag("boss");
        }

        private void ContextMenu_QuickTag_Monster(object sender, RoutedEventArgs e)
        {
            ApplyQuickTag("monster");
        }

        private void RefreshCategories()
        {
            if (!(DataContext is MainViewModel vm)) return;

            var uniqueCategories = new HashSet<string> { "All" };
            foreach (var t in vm.CreatureBank)
            {
                if (t.Extras.TryGetValue("Category", out var cat))
                    uniqueCategories.Add(cat.ToString());
            }
            CmbCategory.ItemsSource = uniqueCategories.ToList();
            CmbCategory.SelectedIndex = 0;
        }

        private void RefreshTypeList()
        {
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in vm.CreatureBank)
            {
                var type = string.IsNullOrWhiteSpace(t.Type) ? "Uncategoized" : t.Type;
                types.Add(type);
            }
            var list = new List<string> { "All" };
            list.AddRange(types.OrderBy(x => x));
            CmbType.ItemsSource = list;
            CmbType.SelectedIndex = 0;
        }

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

            // name search
            var search = TxtSearch?.Text?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                if (!(t.Name?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                    return false;
            }

            // type filter
            var selType = CmbType?.SelectedItem as string;
            if (!string.IsNullOrEmpty(selType) && selType != "All")
            {
                var tokenType = string.IsNullOrWhiteSpace(t.Type) ? "Uncategorized" : t.Type;
                if (!string.Equals(tokenType, selType, StringComparison.OrdinalIgnoreCase)) return false;
            }

            // numeric filters
            double crMin = double.NaN;
            double crMax = double.NaN;
            bool hasCrMin = TryParseDouble(TxtCRMin?.Text, out double tmpCrMin);
            bool hasCrMax = TryParseDouble(TxtCRMax?.Text, out double tmpCrMax);
            if (hasCrMin) crMin = tmpCrMin;
            if (hasCrMax) crMax = tmpCrMax;

            if (!double.IsNaN(crMin) || !double.IsNaN(crMax))
            {
                double crVal = ParseChallengeRating(t.ChallengeRating);
                if (!double.IsNaN(crMin) && crVal < crMin) return false;
                if (!double.IsNaN(crMax) && crVal > crMax) return false;
            }

            // HP filters (use nullable ints)
            int? hpMin = null;
            int? hpMax = null;
            if (int.TryParse(TxtHPMin?.Text, out int tmpHpMin)) hpMin = tmpHpMin;
            if (int.TryParse(TxtHPMax?.Text, out int tmpHpMax)) hpMax = tmpHpMax;

            if (hpMin.HasValue && t.MaxHP < hpMin.Value) return false;
            if (hpMax.HasValue && t.MaxHP > hpMax.Value) return false;

            return true;
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
                if (parts.Length == 2 && double.TryParse(parts[0], out double a) && double.TryParse(parts[1], out double b) && b != 0) return a / b;
            }
            if (double.TryParse(cr, out double d)) return d;
            return double.NaN;
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
                    case "Type":
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.Type), direction));
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.Name), ListSortDirection.Ascending));
                        break;
                    default:
                        _view.SortDescriptions.Add(new SortDescription(nameof(Token.Name), ListSortDirection.Ascending));
                        break;
                }

                if (sortTag == "Name")
                {
                    _view.SortDescriptions.Add(new SortDescription(nameof(Token.Name), ToggleSortDesc.IsChecked == true ? ListSortDirection.Descending : ListSortDirection.Ascending));
                }
                else if (sortTag == "CR")
                {
                    // custom sort for CR: use a comparer
                    _view.SortDescriptions.Add(new SortDescription(nameof(Token.ChallengeRating), ToggleSortDesc.IsChecked == true ? ListSortDirection.Descending : ListSortDirection.Ascending));
                    // Note: ChallengeRating is string, for more precise sorting we could implement IComparer via SortDescription and a custom ListCollectionView
                }
                else if (sortTag == "MaxHP")
                {
                    _view.SortDescriptions.Add(new SortDescription(nameof(Token.MaxHP), ToggleSortDesc.IsChecked == true ? ListSortDirection.Descending : ListSortDirection.Ascending));
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SearchText = TxtSearch.Text.Trim();
                vm.RefreshCreatureBankView();
            }
        }
        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void FilterNumeric_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void CmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplySort();
        private void ToggleSort_Checked(object sender, RoutedEventArgs e) => ApplySort();

        private void LvResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LvResults.SelectedItem is Token t)
            {
                // default action: spawn to map
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

        private void SpawnTokenToMap(Token prototype)
        {
            if (vm == null || prototype == null) return;

            string uniqueName = GenerateUniqueName(prototype.Name);

            var placed = new Token()
            {
                Id = Guid.NewGuid(),
                Name = uniqueName,
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
                Type = prototype.Type,
                Size = prototype.Size,
                Alignment = prototype.Alignment,
                ChallengeRating = prototype.ChallengeRating,
                Tags = prototype.Tags != null ? new List<string>(prototype.Tags) : new List<string>()
            };

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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // if this UserControl is hosted in a Window, close it
            var w = Window.GetWindow(this);
            w?.Close();
        }

        // Helpers

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
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
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
                SizeInSquares = prototype.SizeInSquares
            };

            // place at center of viewport by calling MainWindow/BattleGrid
            var mw = Application.Current?.MainWindow as Window;
            if (mw != null)
            {
                var battleGrid = mw.FindName("BattleGrid") as Controls.BattleGridControl;
                if (battleGrid != null)
                {
                    var centerScreen = new System.Windows.Point(battleGrid.ActualWidth / 2.0, battleGrid.ActualHeight / 2.0);
                    var world = battleGrid.ScreenToWorldPublic(centerScreen);
                    int gx = (int)Math.Floor(world.X / battleGrid.GridCellSize);
                    int gy = (int)Math.Floor(world.Y / battleGrid.GridCellSize);
                    placed.GridX = gx; placed.GridY = gy;
                }
            }

            vm.Tokens.Add(placed);
            // Record undo action
            Services.UndoManager.Record(new Models.TokenAddAction(vm, placed));

            // Optionally close window if desired: (we leave control to caller)
            var w = Window.GetWindow(this);
            w?.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // if this UserControl is hosted in a Window, close it
            var w = Window.GetWindow(this);
            w?.Close();
        }
    }
}
