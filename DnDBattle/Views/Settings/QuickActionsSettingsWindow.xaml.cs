using DnDBattle.Models;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Views.Settings;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Views.Settings
{
    /// <summary>
    /// Interaction logic for QuickActionsSettingsWindow.xaml
    /// </summary>
    public partial class QuickActionsSettingsWindow : Window
    {
        private List<QuickAction> _actions;
        private List<QuickAction> _originalActions;

        public List<QuickAction> ResultActions { get; private set; }

        public QuickActionsSettingsWindow(List<QuickAction> currentActions)
        {
            InitializeComponent();

            _originalActions = currentActions;
            _actions = currentActions.Select(a => new QuickAction
            {
                Id = a.Id,
                Name = a.Name,
                Icon = a.Icon,
                Description = a.Description,
                ActionType = a.ActionType,
                IsEnabled = a.IsEnabled,
                SortOrder = a.SortOrder,
                ConditionToToggle = a.ConditionToToggle,
                DiceExpression = a.DiceExpression,
                CustomCommand = a.CustomCommand
            }).OrderBy(a => a.SortOrder).ToList();

            RefreshList();
        }

        private void RefreshList(string filter = null)
        {
            var filtered = _actions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                filter = filter.ToLower();
                filtered = filtered.Where(a =>
                    a.Name.ToLower().Contains(filter) == true);
            }
            ActionsList.ItemsSource = filtered.OrderBy(a => a.SortOrder).ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshList(TxtSearch.Text);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var action = _actions.FirstOrDefault(a => a.Id == id);
                if (action == null) return;

                var index = _actions.IndexOf(action);
                if (index > 0)
                {
                    var prevAction = _actions[index - 1];
                    var tempSort = action.SortOrder;
                    prevAction.SortOrder = tempSort;

                    _actions = _actions.OrderBy(a => a.SortOrder).ToList();
                    RefreshList(TxtSearch.Text);
                }
            }
        }

        private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string id)
            {
                var action = _actions.FirstOrDefault(a => a.Id == id);
                if (action == null) return;

                var index = _actions.IndexOf(action);
                if (index < _actions.Count - 1)
                {
                    var nextAction = _actions[index + 1];
                    var tempSort = action.SortOrder;
                    action.SortOrder = nextAction.SortOrder;
                    nextAction.SortOrder = tempSort;

                    _actions = _actions.OrderBy(a => a.SortOrder).ToList();
                    RefreshList(TxtSearch.Text);
                }
            }
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var action in _actions)
                action.IsEnabled = true;

            RefreshList(TxtSearch.Text);
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var action in _actions)
                action.IsEnabled = false;
            RefreshList(TxtSearch.Text);
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Reset all quick actions to default settings?",
                "Reset Default",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _actions = QuickActionPresets.GetDefaultActions();
                RefreshList(TxtSearch.Text);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _actions.Count; i++)
                _actions[i].SortOrder = i;

            ResultActions = _actions;
            DialogResult = true;
            Close();
        }
    }
}
