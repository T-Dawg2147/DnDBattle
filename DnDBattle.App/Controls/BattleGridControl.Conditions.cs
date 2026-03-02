using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DnDBattle.Models.Tiles;
using Condition = DnDBattle.Models.Effects.Condition;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Condition badges and visual effects
    /// </summary>
    public partial class BattleGridControl
    {
        #region Condition Badges

        /// <summary>
        /// Creates condition badge icons that appear around the token
        /// </summary>
        // VISUAL REFRESH
        private FrameworkElement CreateConditionBadges(Token token)
        {
            if (token.Conditions == Models.Effects.Condition.None)
                return null;

            var activeConditions = token.Conditions.GetActiveConditions().ToList();
            if (activeConditions.Count == 0)
                return null;

            var badgePanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                MaxWidth = GridCellSize * token.SizeInSquares,
                Margin = new Thickness(2, 2, 0, 0)
            };

            int maxBadges = 6; // Limit visible badges
            int count = 0;

            foreach (var condition in activeConditions)
            {
                if (count >= maxBadges) break;

                var badge = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(ConditionExtensions.GetConditionColor(condition)),
                    Margin = new Thickness(1),
                    ToolTip = CreateConditionTooltip(condition),
                    Child = new TextBlock
                    {
                        Text = ConditionExtensions.GetConditionIcon(condition),
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                badgePanel.Children.Add(badge);
                count++;
            }

            // Show "+X" if there are more conditions
            if (activeConditions.Count > maxBadges)
            {
                var moreBadge = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    Margin = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = $"+{activeConditions.Count - maxBadges}",
                        FontSize = 8,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                badgePanel.Children.Add(moreBadge);
            }

            return badgePanel;
        }

        /// <summary>
        /// Creates a tooltip for a condition badge
        /// </summary>
        private ToolTip CreateConditionTooltip(Models.Effects.Condition condition)
        {
            var tooltip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(ConditionExtensions.GetConditionColor(condition)),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(10)
            };

            var stack = new StackPanel { MaxWidth = 300 };

            stack.Children.Add(new TextBlock
            {
                Text = $"{ConditionExtensions.GetConditionIcon(condition)} {ConditionExtensions.GetConditionName(condition)}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 8)
            });

            stack.Children.Add(new TextBlock
            {
                Text = ConditionExtensions.GetConditionDescription(condition),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200))
            });

            tooltip.Content = stack;
            return tooltip;
        }

        #endregion

        #region Condition Visual Effects

        /// <summary>
        /// Applies visual effects to the token image based on conditions
        /// </summary>
        // VISUAL REFRESH
        private void ApplyConditionVisualEffects(Image img, Token token)
        {
            // Invisible - make semi-transparent
            if (token.HasCondition(Models.Effects.Condition.Invisible))
            {
                img.Opacity = 0.4;
            }

            // Petrified - grayscale effect
            if (token.HasCondition(Models.Effects.Condition.Petrified))
            {
                var grayscaleEffect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    ShadowDepth = 0,
                    BlurRadius = 0
                };
                // Note: For true grayscale you'd need a shader, this is a simple approximation
                img.Opacity = 0.7;
            }

            // Unconscious/Prone - rotate slightly
            if (token.HasCondition(Models.Effects.Condition.Unconscious) || token.HasCondition(Models.Effects.Condition.Prone))
            {
                img.RenderTransformOrigin = new Point(0.5, 0.5);
                img.RenderTransform = new RotateTransform(token.HasCondition(Models.Effects.Condition.Unconscious) ? 90 : 15);
            }

            // Dead (HP <= -MaxHP) - very faded
            if (token.IsDead)
            {
                img.Opacity = 0.3;
            }
        }

        #endregion

        #region Condition Visual Refresh

        /// <summary>
        /// Refreshes condition badges and visual effects for a specific token
        /// by triggering a full token visual rebuild.
        /// </summary>
        // VISUAL REFRESH
        public void RefreshConditionVisuals(Token token)
        {
            if (token == null) return;
            RebuildTokenVisuals();
        }

        #endregion
    }
}
