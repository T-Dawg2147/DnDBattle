using DnDBattle.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DnDBattle.Views
{
    /// <summary>
    /// Developer Settings Window for configuring experimental and advanced features.
    /// </summary>
    public partial class DevSettingsWindow : Window
    {
        private readonly DevSettings _settings;

        public DevSettingsWindow()
        {
            InitializeComponent();
            _settings = DevSettings.Instance;
            DataContext = _settings;

            // Select first navigation item
            NavList.SelectedIndex = 0;

            // Initialize combo boxes
            InitializeComboBoxes();
        }

        private void InitializeComboBoxes()
        {
            // Set diagonal movement rule
            foreach (ComboBoxItem item in DiagonalRuleCombo.Items)
            {
                if (item.Tag?.ToString() == _settings.DiagonalMovementRule.ToString())
                {
                    DiagonalRuleCombo.SelectedItem = item;
                    break;
                }
            }

            // Set grid size
            foreach (ComboBoxItem item in GridSizeCombo.Items)
            {
                if (item.Tag?.ToString() == _settings.CustomGridSizeFeet.ToString())
                {
                    GridSizeCombo.SelectedItem = item;
                    break;
                }
            }
        }

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavList.SelectedItem is not ListBoxItem selectedItem)
                return;

            // Hide all panels
            PanelLighting.Visibility = Visibility.Collapsed;
            PanelTokens.Visibility = Visibility.Collapsed;
            PanelEffects.Visibility = Visibility.Collapsed;
            PanelCombat.Visibility = Visibility.Collapsed;
            PanelMap.Visibility = Visibility.Collapsed;
            PanelPerformance.Visibility = Visibility.Collapsed;

            // Show selected panel
            var tag = selectedItem.Tag?.ToString();
            switch (tag)
            {
                case "Lighting":
                    PanelLighting.Visibility = Visibility.Visible;
                    break;
                case "Tokens":
                    PanelTokens.Visibility = Visibility.Visible;
                    break;
                case "Effects":
                    PanelEffects.Visibility = Visibility.Visible;
                    break;
                case "Combat":
                    PanelCombat.Visibility = Visibility.Visible;
                    break;
                case "Map":
                    PanelMap.Visibility = Visibility.Visible;
                    break;
                case "Performance":
                    PanelPerformance.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void DiagonalRuleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DiagonalRuleCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                if (Enum.TryParse<DiagonalMovementRule>(item.Tag.ToString(), out var rule))
                {
                    _settings.DiagonalMovementRule = rule;
                }
            }
        }

        private void GridSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridSizeCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                if (int.TryParse(item.Tag.ToString(), out var size))
                {
                    _settings.CustomGridSizeFeet = size;
                }
            }
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all settings to their defaults?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _settings.ResetToDefaults();
                InitializeComboBoxes();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Reload settings to discard changes
            var fresh = DevSettings.Load();
            foreach (var prop in typeof(DevSettings).GetProperties())
            {
                if (prop.CanWrite && prop.CanRead && prop.Name != "Instance")
                {
                    prop.SetValue(_settings, prop.GetValue(fresh));
                }
            }
            Close();
        }

        private void SaveClose_Click(object sender, RoutedEventArgs e)
        {
            _settings.Save();
            Close();
        }
    }
}
