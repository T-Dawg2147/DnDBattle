using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DnDBattle.Views
{
    /// <summary>
    /// View model wrapper for spell display in the list
    /// </summary>
    public class SpellListItem
    {
        public SpellTemplate Spell { get; set; }
        public string Name => Spell.Name;
        public string LevelDisplay => Spell.LevelDisplay;
        public string Description => Spell.Description;
        public string ShapeText => $"{Spell.Size}ft {Spell.Shape}";
        public string DamageIcon => Spell.DamageType.GetIcon();
        public bool IsFavorite
        {
            get => Spell.IsFavorite;
            set => Spell.IsFavorite = value;
        }
        public string FavoriteStar => IsFavorite ? "★" : "☆";
        public Visibility ConcentrationVisibility => Spell.RequiresConcentration ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Spell Templates Library window for browsing and placing D&D spells
    /// </summary>
    public partial class SpellLibraryWindow : Window
    {
        private readonly List<SpellTemplate> _allSpells;
        private List<SpellListItem> _displayItems;

        /// <summary>
        /// Fired when the user selects a spell to place
        /// </summary>
        public event Action<AreaEffect> SpellSelected;

        public SpellLibraryWindow()
        {
            InitializeComponent();
            _allSpells = SpellLibrary.GetDefaultSpells();
            RefreshList();
            TxtSearch.Focus();
        }

        private void RefreshList()
        {
            var filtered = _allSpells.AsEnumerable();

            // Search filter
            string search = TxtSearch.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(s =>
                    s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.School.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Level filter
            int levelFilter = CmbLevelFilter.SelectedIndex - 1; // -1 = All, 0 = Cantrips, etc.
            if (levelFilter >= 0)
            {
                filtered = filtered.Where(s => s.Level == levelFilter);
            }

            // Favorites filter
            if (ChkFavoritesOnly.IsChecked == true)
            {
                filtered = filtered.Where(s => s.IsFavorite);
            }

            _displayItems = filtered
                .OrderBy(s => s.Level)
                .ThenBy(s => s.Name)
                .Select(s => new SpellListItem { Spell = s })
                .ToList();

            LstSpells.ItemsSource = _displayItems;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => RefreshList();
        private void CmbLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshList();
        private void ChkFavoritesOnly_Changed(object sender, RoutedEventArgs e) => RefreshList();
        private void ClearSearch_Click(object sender, RoutedEventArgs e) { TxtSearch.Text = ""; }

        private void LstSpells_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = LstSpells.SelectedItem as SpellListItem;
            BtnPlace.IsEnabled = item != null;

            if (item != null)
            {
                var s = item.Spell;
                string info = $"{s.Name} ({s.LevelDisplay} {s.School})\n" +
                              $"{s.Shape} · {s.Size}ft";
                if (s.Duration > 0)
                    info += $" · {s.Duration} rounds{(s.RequiresConcentration ? " (C)" : "")}";
                if (!string.IsNullOrEmpty(s.DamageExpression))
                    info += $" · {s.DamageExpression} {s.DamageType.GetDisplayName()}";
                info += $"\n{s.Description}";
                TxtSelectedInfo.Text = info;
            }
            else
            {
                TxtSelectedInfo.Text = "Select a spell to see details";
            }
        }

        private void LstSpells_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaceSelectedSpell();
        }

        private void PlaceSpell_Click(object sender, RoutedEventArgs e)
        {
            PlaceSelectedSpell();
        }

        private void PlaceSelectedSpell()
        {
            var item = LstSpells.SelectedItem as SpellListItem;
            if (item == null) return;

            var effect = item.Spell.ToAreaEffect();
            SpellSelected?.Invoke(effect);
            DialogResult = true;
            Close();
        }

        private void ToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SpellListItem item)
            {
                item.IsFavorite = !item.IsFavorite;
                RefreshList();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
