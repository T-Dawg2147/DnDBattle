using DnDBattle.Controls;
using DnDBattle.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for TokenDetailsWindow.xaml
    /// </summary>
    public partial class TokenDetailsWindow : Window
    {
        private BattleGridControl BattleGrid = new();
        private Token _token;

        public TokenDetailsWindow(Token token)
        {
            InitializeComponent();
            _token = token;
            DataContext = token;

            Loaded += TokenDetailsWindow_Loaded;
        }

        private void TokenDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateActions();
        }

        private void TxtHP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BattleGrid?.Focus();
                e.Handled = true;
            }
        }

        #region Actions Display

        private void PopulateActions()
        {
            if (_token == null || ActionsContainer == null) return;

            ActionsContainer.Children.Clear();

            // Group actions by type
            var actionGroups = new Dictionary<string, (List<Models.Action> actions, string displayName, Color color)>
            {
                { "Actions", (_token.Actions, "Actions", Color.FromRgb(244, 67, 54)) },
                { "BonusActions", (_token.BonusActions, "Bonus Actions", Color.FromRgb(255, 152, 0)) },
                { "Reactions", (_token.Reactions, "Reactions", Color.FromRgb(156, 39, 176)) },
                { "LegendaryActions", (_token.LegendaryActions, "Legendary Actions", Color.FromRgb(255, 215, 0)) }
            };

            bool hasAnyActions = false;

            foreach (var group in actionGroups)
            {
                var actions = group.Value.actions;
                if (actions == null || actions.Count == 0) continue;

                hasAnyActions = true;

                // Section header
                ActionsContainer.Children.Add(new TextBlock
                {
                    Text = group.Value.displayName,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Margin = new Thickness(0, ActionsContainer.Children.Count > 0 ? 15 : 0, 0, 8),
                    Foreground = new SolidColorBrush(group.Value.color)
                });

                // Action items
                foreach (var action in actions)
                {
                    ActionsContainer.Children.Add(CreateActionElement(action));
                }
            }

            // Show message if no actions
            if (!hasAnyActions)
            {
                ActionsContainer.Children.Add(new TextBlock
                {
                    Text = "No actions available",
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    FontStyle = FontStyles.Italic,
                    FontSize = 11
                });
            }
        }

        private UIElement CreateActionElement(Models.Action action)
        {
            // Main container with hover effect
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand
            };

            // Hover effects
            border.MouseEnter += (s, e) => border.Background = new SolidColorBrush(Color.FromRgb(60, 60, 64));
            border.MouseLeave += (s, e) => border.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

            var content = new StackPanel();

            // First row: Name + badges
            var nameRow = new WrapPanel { Orientation = Orientation.Horizontal };

            // Action name
            nameRow.Children.Add(new TextBlock
            {
                Text = action.Name ?? "Unknown Action",
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });

            // Attack bonus badge
            if (action.AttackBonus != null && action.AttackBonus != 0)
            {
                nameRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = $"+{action.AttackBonus}",
                        Foreground = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold
                    }
                });
            }

            // Damage badge
            if (!string.IsNullOrEmpty(action.DamageExpression))
            {
                nameRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = action.DamageExpression,
                        Foreground = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold
                    }
                });
            }

            // Range badge
            if (!string.IsNullOrEmpty(action.Range))
            {
                nameRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = action.Range,
                        Foreground = Brushes.Black,
                        FontSize = 10
                    }
                });
            }

            content.Children.Add(nameRow);

            // Description (truncated)
            if (!string.IsNullOrEmpty(action.Description))
            {
                string truncatedDesc = action.Description.Length > 120
                    ? action.Description.Substring(0, 117) + "..."
                    : action.Description;

                content.Children.Add(new TextBlock
                {
                    Text = truncatedDesc,
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            border.Child = content;

            // Create detailed tooltip
            border.ToolTip = CreateActionTooltip(action);

            return border;
        }

        private ToolTip CreateActionTooltip(Models.Action action)
        {
            var tooltip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                MaxWidth = 400
            };

            var stack = new StackPanel();

            // Action name
            stack.Children.Add(new TextBlock
            {
                Text = action.Name ?? "Unknown Action",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Stats row
            var statsPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };

            if (action.AttackBonus != null && action.AttackBonus != 0)
            {
                statsPanel.Children.Add(new TextBlock
                {
                    Text = $"Attack: +{action.AttackBonus}",
                    Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    Margin = new Thickness(0, 0, 15, 0)
                });
            }

            if (!string.IsNullOrEmpty(action.DamageExpression))
            {
                statsPanel.Children.Add(new TextBlock
                {
                    Text = $"Damage: {action.DamageExpression}",
                    Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    Margin = new Thickness(0, 0, 15, 0)
                });
            }

            if (!string.IsNullOrEmpty(action.Range))
            {
                statsPanel.Children.Add(new TextBlock
                {
                    Text = $"Range: {action.Range}",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246))
                });
            }

            if (statsPanel.Children.Count > 0)
            {
                stack.Children.Add(statsPanel);
            }

            // Full description
            if (!string.IsNullOrEmpty(action.Description))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = action.Description,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12
                });
            }

            tooltip.Content = stack;
            return tooltip;
        }

        #endregion
    }
}
