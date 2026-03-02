using DnDBattle.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
using DnDBattle.Views.Settings;
using DnDBattle.Views.TileMap;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Views.Spells
{
    public partial class SpellSlotsDisplay : UserControl
    {
        private Token _token;

        public event Action<string> LogAction;

        public SpellSlotsDisplay()
        {
            InitializeComponent();
        }

        public void SetToken(Token token)
        {
            _token = token;
            UpdateDisplay();
        }

        // VISUAL REFRESH
        public void UpdateDisplay()
        {
            SlotsContainer.Children.Clear();

            if (_token?.SpellSlots == null || !_token.HasSpellSlots)
            {
                MainBorder.Visibility = Visibility.Collapsed;
                return;
            }

            MainBorder.Visibility = Visibility.Visible;

            // Create slot displays for each level
            for (int level = 1; level <= 9; level++)
            {
                int max = _token.SpellSlots.GetMaxSlots(level);
                if (max == 0) continue;

                int current = _token.SpellSlots.GetCurrentSlots(level);

                var slotGroup = CreateSlotGroup(level, current, max);
                SlotsContainer.Children.Add(slotGroup);
            }
        }

        private Border CreateSlotGroup(int level, int current, int max)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 6, 6)
            };

            var stack = new StackPanel();

            // Level label
            stack.Children.Add(new TextBlock
            {
                Text = GetLevelText(level),
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Slot circles
            var circlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            for (int i = 0; i < max; i++)
            {
                bool isFilled = i < current;
                var circle = CreateSlotCircle(level, i, isFilled);
                circlePanel.Children.Add(circle);
            }

            stack.Children.Add(circlePanel);
            border.Child = stack;
            return border;
        }

        private Ellipse CreateSlotCircle(int level, int index, bool isFilled)
        {
            var circle = new Ellipse
            {
                Width = 12,
                Height = 12,
                Stroke = new SolidColorBrush(Color.FromRgb(186, 104, 200)),
                StrokeThickness = 2,
                Fill = isFilled
                    ? new SolidColorBrush(Color.FromRgb(186, 104, 200))
                    : Brushes.Transparent,
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand,
                Tag = new Tuple<int, int>(level, index)
            };

            circle.MouseLeftButtonDown += (s, e) =>
            {
                if (s is Ellipse el && el.Tag is Tuple<int, int> data)
                {
                    ToggleSlot(data.Item1, data.Item2);
                }
            };

            circle.ToolTip = isFilled
                ? $"Click to use level {level} slot"
                : $"Click to restore level {level} slot";

            return circle;
        }

        private void ToggleSlot(int level, int index)
        {
            if (_token?.SpellSlots == null) return;

            int current = _token.SpellSlots.GetCurrentSlots(level);

            if (index < current)
            {
                // Use slot
                _token.SpellSlots.UseSlot(level);
                LogAction?.Invoke($"✨ {_token.Name} used a level {level} spell slot ({_token.SpellSlots.GetCurrentSlots(level)}/{_token.SpellSlots.GetMaxSlots(level)} remaining)");
            }
            else
            {
                // Restore slot
                _token.SpellSlots.RestoreSlot(level);
                LogAction?.Invoke($"✨ {_token.Name} restored a level {level} spell slot ({_token.SpellSlots.GetCurrentSlots(level)}/{_token.SpellSlots.GetMaxSlots(level)})");
            }

            UpdateDisplay();
        }

        private string GetLevelText(int level)
        {
            return level switch
            {
                1 => "1st",
                2 => "2nd",
                3 => "3rd",
                _ => $"{level}th"
            };
        }

        private void BtnLongRest_Click(object sender, RoutedEventArgs e)
        {
            if (_token?.SpellSlots == null) return;

            _token.SpellSlots.LongRest();
            LogAction?.Invoke($"🌙 {_token.Name} completed a long rest - all spell slots restored!");
            UpdateDisplay();
        }
    }
}