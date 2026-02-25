using DnDBattle.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DnDBattle.Views;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
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

namespace DnDBattle.Views.Combat
{
    public partial class LegendaryActionsDisplay : UserControl
    {
        private Token _token;

        public event Action<string> LogAction;

        public LegendaryActionsDisplay()
        {
            InitializeComponent();
        }

        public void SetToken(Token token)
        {
            _token = token;
            UpdateDisplay();
        }

        // VISUAL REFRESH - SELECTED_TOKEN_PANEL
        public void UpdateDisplay()
        {
            ActionPointsContainer.Children.Clear();

            if (_token == null || !_token.HasLegendaryActions)
            {
                MainBorder.Visibility = Visibility.Collapsed;
                return;
            }

            MainBorder.Visibility = Visibility.Visible;

            // Create action point indicators
            for (int i = 0; i < _token.LegendaryActionsMax; i++)
            {
                bool isAvailable = i < _token.LegendaryActionsRemaining;
                var point = CreateActionPoint(i, isAvailable);
                ActionPointsContainer.Children.Add(point);
            }

            // Update text
            TxtRemaining.Text = $"{_token.LegendaryActionsRemaining} of {_token.LegendaryActionsMax} remaining";

            // Show reset button if any are used
            BtnReset.Visibility = _token.LegendaryActionsRemaining < _token.LegendaryActionsMax
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private Border CreateActionPoint(int index, bool isAvailable)
        {
            var border = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = isAvailable
                    ? new SolidColorBrush(Color.FromRgb(255, 215, 0))
                    : new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                BorderThickness = new Thickness(2),
                Margin = new Thickness(3, 0, 3, 0),
                Cursor = Cursors.Hand,
                Tag = index
            };

            // Star icon
            border.Child = new TextBlock
            {
                Text = "★",
                Foreground = isAvailable ? Brushes.Black : new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.ToolTip = isAvailable
                ? "Click to use a legendary action"
                : "Click to restore a legendary action";

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (isAvailable)
                {
                    UseLegendaryAction();
                }
                else
                {
                    RestoreLegendaryAction();
                }
            };

            return border;
        }

        private void UseLegendaryAction()
        {
            if (_token == null || _token.LegendaryActionsRemaining <= 0) return;

            _token.UseLegendaryAction(1);
            LogAction?.Invoke($"⭐ {_token.Name} used a legendary action ({_token.LegendaryActionsRemaining}/{_token.LegendaryActionsMax} remaining)");
            UpdateDisplay();
        }

        private void RestoreLegendaryAction()
        {
            if (_token == null || _token.LegendaryActionsRemaining >= _token.LegendaryActionsMax) return;

            _token.LegendaryActionsRemaining++;
            LogAction?.Invoke($"⭐ {_token.Name} restored a legendary action ({_token.LegendaryActionsRemaining}/{_token.LegendaryActionsMax})");
            UpdateDisplay();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;

            _token.ResetLegendaryActions();
            LogAction?.Invoke($"⭐ {_token.Name}'s legendary actions reset to {_token.LegendaryActionsMax}");
            UpdateDisplay();
        }
    }
}