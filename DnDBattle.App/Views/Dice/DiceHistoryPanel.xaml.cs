using DnDBattle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;

namespace DnDBattle.Views.Dice
{
    public partial class DiceHistoryPanel : UserControl
    {
        private DiceHistoryService _historyService;
        private string _filterType = "All";
        private string _filterRoller = "";

        public DiceHistoryPanel()
        {
            InitializeComponent();
        }

        public void SetHistoryService(DiceHistoryService service)
        {
            _historyService = service;
            _historyService.RollRecorded += OnRollRecorded;

            HistoryList.ItemsSource = _historyService.History;
            UpdateStatistics();
        }

        private void OnRollRecorded(DiceRollRecord record)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateRollerFilter(record.RollerName);
                ApplyFilters();
                UpdateStatistics();
            });
        }

        private void UpdateRollerFilter(string rollerName)
        {
            // Add roller to filter if not exists
            bool exists = CmbRoller.Items.Cast<ComboBoxItem>()
                .Any(item => item.Tag as string == rollerName);

            if (!exists && !string.IsNullOrEmpty(rollerName))
            {
                CmbRoller.Items.Add(new ComboBoxItem { Content = rollerName, Tag = rollerName });
            }
        }

        private void ApplyFilters()
        {
            if (_historyService == null) return;

            IEnumerable<DiceRollRecord> filtered = _historyService.History;

            // Filter by type
            if (_filterType != "All" && Enum.TryParse<DiceRollType>(_filterType, out var rollType))
            {
                filtered = filtered.Where(r => r.RollType == rollType);
            }

            // Filter by roller
            if (!string.IsNullOrEmpty(_filterRoller))
            {
                filtered = filtered.Where(r => r.RollerName.Equals(_filterRoller, StringComparison.OrdinalIgnoreCase));
            }

            HistoryList.ItemsSource = filtered.ToList();
        }

        private void UpdateStatistics()
        {
            if (_historyService == null) return;

            var stats = _historyService.GetStatistics(_filterRoller);

            TxtTotalRolls.Text = stats.TotalRolls.ToString();
            TxtNat20s.Text = stats.Natural20s.ToString();
            TxtNat1s.Text = stats.Natural1s.ToString();
            TxtAvgD20.Text = stats.AverageD20.ToString("F1");
        }

        private void CmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFilter.SelectedItem is ComboBoxItem item)
            {
                _filterType = item.Tag as string ?? "All";
                ApplyFilters();
            }
        }

        private void CmbRoller_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRoller.SelectedItem is ComboBoxItem item)
            {
                _filterRoller = item.Tag as string ?? "";
                ApplyFilters();
                UpdateStatistics();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Clear all dice roll history?",
                "Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _historyService?.Clear();
                UpdateStatistics();
            }
        }
    }
}