using DnDBattle.Models;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing token rendering and visual management functionality
    /// </summary>
    public partial class BattleGridControl
    {
        #region Token Visual Rebuilding

        public void RebuildTokenVisuals()
        {
            System.Diagnostics.Debug.WriteLine($"=== RebuildTokenVisuals called ===");

            var existing = RenderCanvas.Children.OfType<FrameworkElement>().Where(c => c.Tag is Token).ToList();
            foreach (var c in existing)
                RenderCanvas.Children.Remove(c);

            if (Tokens == null) return;

            foreach (var token in Tokens)
            {
                try
                {
                    // Subscribe to HP changes to update the visual
                    token.PropertyChanged -= Token_PropertyChanged;
                    token.PropertyChanged += Token_PropertyChanged;

                    var container = new Grid()
                    {
                        Width = GridCellSize * token.SizeInSquares + 8,
                        Height = GridCellSize * token.SizeInSquares + 8,
                        Tag = token,
                        Background = Brushes.Transparent
                    };

                    // Current turn glow
                    if (token.IsCurrentTurn)
                    {
                        var glowBorder = new Border()
                        {
                            Width = GridCellSize * token.SizeInSquares + 4,
                            Height = GridCellSize * token.SizeInSquares + 4,
                            CornerRadius = new CornerRadius(4),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                            BorderThickness = new Thickness(3),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Effect = new System.Windows.Media.Effects.DropShadowEffect()
                            {
                                Color = Color.FromRgb(76, 175, 80),
                                BlurRadius = 15,
                                ShadowDepth = 0,
                                Opacity = 0.8
                            }
                        };
                        container.Children.Add(glowBorder);
                    }

                    ImageSource imageSource = token.DisplayImage ?? token.Image ?? LoadDefaultTokenImage();

                    var img = new Image
                    {
                        Width = GridCellSize * token.SizeInSquares,
                        Height = GridCellSize * token.SizeInSquares,
                        Stretch = Stretch.UniformToFill,
                        Source = imageSource,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = token
                    };

                    // Apply visual effects based on conditions
                    ApplyConditionVisualEffects(img, token);

                    try
                    {
                        img.ToolTip = CreateTokenTooltip(token);
                    }
                    catch
                    {
                        img.ToolTip = token.Name;
                    }

                    img.MouseLeftButtonDown += (s, e) =>
                    {
                        if (e.ClickCount == 2)
                        {
                            if (((FrameworkElement)s).Tag is Token t)
                            {
                                TokenDoubleClicked?.Invoke(t);
                            }
                            e.Handled = true;
                        }
                        else
                        {
                            Token_MouseLeftButtonDown(container, e);
                        }
                    };

                    try
                    {
                        img.ContextMenu = CreateTokenContextMenu(token);
                    }
                    catch { }

                    ToolTipService.SetInitialShowDelay(img, 100);
                    ToolTipService.SetShowDuration(img, 30000);
                    ToolTipService.SetBetweenShowDelay(img, 0);

                    container.Children.Add(img);

                    // Add condition badges
                    var conditionBadges = CreateConditionBadges(token);
                    if (conditionBadges != null)
                    {
                        container.Children.Add(conditionBadges);
                    }

                    // Add HP indicator bar
                    var hpBar = CreateTokenHPBar(token);
                    if (hpBar != null)
                    {
                        container.Children.Add(hpBar);
                    }

                    // Phase 5: Elevation badge
                    var elevBadge = CreateElevationBadge(token);
                    if (elevBadge != null)
                        container.Children.Add(elevBadge);

                    // Phase 5: Facing arrow
                    var facingArrow = CreateFacingArrow(token);
                    if (facingArrow != null)
                        container.Children.Add(facingArrow);

                    container.MouseLeftButtonDown += Token_MouseLeftButtonDown;
                    container.MouseMove += Token_MouseMove;
                    container.MouseLeftButtonUp += Token_MouseLeftButtonUp;

                    RenderCanvas.Children.Add(container);
                    Canvas.SetZIndex(container, token.IsCurrentTurn ? 150 : 100);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR creating token visual for {token.Name}: {ex.Message}");
                }
            }

            LayoutTokens();
            UpdateGridVisual();
            RedrawLighting();
            RedrawAuras();
        }

        #endregion

        #region Token Tooltip

        private ToolTip CreateTokenTooltip(Token token)
        {
            var tooltip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                HasDropShadow = true
            };

            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                MinWidth = 180
            };

            var stack = new StackPanel();

            // Name (bold, larger)
            stack.Children.Add(new TextBlock
            {
                Text = token.Name ?? "Unknown",
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 2)
            });

            // Type and Size
            var subtitleText = "";
            if (!string.IsNullOrEmpty(token.Size)) subtitleText += token.Size;
            if (!string.IsNullOrEmpty(token.Type))
            {
                if (subtitleText.Length > 0) subtitleText += " ";
                subtitleText += token.Type;
            }
            if (!string.IsNullOrEmpty(subtitleText))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = subtitleText,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // HP Bar
            var hpPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            var hpHeader = new Grid();
            hpHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hpHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            hpHeader.Children.Add(new TextBlock
            {
                Text = "Hit Points",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130))
            });

            var hpText = new TextBlock
            {
                Text = $"{token.HP} / {token.MaxHP}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Color the HP text based on health percentage
            double hpPercent = token.MaxHP > 0 ? (double)Math.Max(0, token.HP) / token.MaxHP : 0;
            hpText.Foreground = hpPercent > 0.5 ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                                hpPercent > 0.25 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) :
                                new SolidColorBrush(Color.FromRgb(244, 67, 54));

            Grid.SetColumn(hpText, 1);
            hpHeader.Children.Add(hpText);
            hpPanel.Children.Add(hpHeader);

            // HP Bar visual
            var hpBarBg = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                CornerRadius = new CornerRadius(3),
                Height = 8,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var hpBarFill = new Border
            {
                Background = hpPercent > 0.5 ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                             hpPercent > 0.25 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) :
                             new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                CornerRadius = new CornerRadius(3),
                Height = 8,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(0, 156 * hpPercent) // 156 = minWidth - padding
            };

            var hpBarGrid = new Grid { Height = 8, Margin = new Thickness(0, 4, 0, 0) };
            hpBarGrid.Children.Add(hpBarBg);
            hpBarGrid.Children.Add(hpBarFill);
            hpPanel.Children.Add(hpBarGrid);

            stack.Children.Add(hpPanel);

            // Stats row (AC, CR, Speed)
            var statsGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // AC
            var acPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            acPanel.Children.Add(new TextBlock
            {
                Text = "AC",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            acPanel.Children.Add(new TextBlock
            {
                Text = token.ArmorClass.ToString(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(acPanel, 0);
            statsGrid.Children.Add(acPanel);

            // CR
            var crPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            crPanel.Children.Add(new TextBlock
            {
                Text = "CR",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            crPanel.Children.Add(new TextBlock
            {
                Text = token.ChallengeRating ?? "—",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(crPanel, 1);
            statsGrid.Children.Add(crPanel);

            // Initiative (if rolled)
            var initPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            initPanel.Children.Add(new TextBlock
            {
                Text = "Init",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            initPanel.Children.Add(new TextBlock
            {
                Text = token.Initiative > 0 ? token.Initiative.ToString() : "—",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(186, 104, 200)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(initPanel, 2);
            statsGrid.Children.Add(initPanel);

            stack.Children.Add(statsGrid);

            // Speed (smaller, below stats)
            if (!string.IsNullOrEmpty(token.Speed))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Speed: {token.Speed}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }

            bool showMovement = false;
            if (Application.Current?.MainWindow?.DataContext is MainViewModel vm && vm.IsInCombat)
            {
                showMovement = true;
            }

            if (showMovement)
            {
                var movementPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

                var movementHeader = new Grid();
                movementHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                movementHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                movementHeader.Children.Add(new TextBlock
                {
                    Text = "Movement",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130))
                });

                var movementText = new TextBlock
                {
                    Text = $"{token.MovementRemainingThisTurn} / {token.SpeedSquares}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Foreground = token.CanMoveThisTurn
                        ? new SolidColorBrush(Color.FromRgb(100, 181, 246))
                        : new SolidColorBrush(Color.FromRgb(244, 67, 54))
                };
                Grid.SetColumn(movementText, 1);
                movementHeader.Children.Add(movementText);
                movementPanel.Children.Add(movementHeader);

                // Movement bar
                double movePercent = token.SpeedSquares > 0
                    ? (double)token.MovementRemainingThisTurn / token.SpeedSquares
                    : 0;

                var moveBarGrid = new Grid { Height = 6, Margin = new Thickness(0, 4, 0, 0) };
                moveBarGrid.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    CornerRadius = new CornerRadius(3)
                });
                moveBarGrid.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    CornerRadius = new CornerRadius(3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = Math.Max(0, 156 * movePercent)
                });
                movementPanel.Children.Add(moveBarGrid);

                stack.Children.Add(movementPanel);
            }

            // Conditions
            if (token.Conditions != Models.Condition.None)
            {
                var conditionsBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(50, 40, 30)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 8, 0, 0)
                };

                var conditionsPanel = new WrapPanel();
                foreach (var condition in token.Conditions.GetActiveConditions())
                {
                    conditionsPanel.Children.Add(new TextBlock
                    {
                        Text = $"{ConditionExtensions.GetConditionIcon(condition)} ",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                var condText = new TextBlock
                {
                    Text = token.ConditionsDisplay,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var condStack = new StackPanel { Orientation = Orientation.Horizontal };
                condStack.Children.Add(conditionsPanel);
                condStack.Children.Add(condText);

                conditionsBorder.Child = condStack;
                stack.Children.Add(conditionsBorder);
            }

            // Tags (if any)
            if (token.Tags != null && token.Tags.Count > 0)
            {
                var tagsPanel = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
                foreach (var tag in token.Tags.Take(4)) // Limit to 4 tags
                {
                    tagsPanel.Children.Add(new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(40, 80, 35)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 4, 0),
                        Child = new TextBlock
                        {
                            Text = tag,
                            FontSize = 9,
                            Foreground = Brushes.White
                        }
                    });
                }
                if (token.Tags.Count > 4)
                {
                    tagsPanel.Children.Add(new TextBlock
                    {
                        Text = $"+{token.Tags.Count - 4} more",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
                stack.Children.Add(tagsPanel);
            }

            mainBorder.Child = stack;
            tooltip.Content = mainBorder;
            return tooltip;
        }

        #endregion

        #region Default Token Image

        private ImageSource LoadDefaultTokenImage()
        {
            try
            {
                // Try to load the default token image from resources
                var uri = new Uri("pack://application:,,,/Resources/Entities/Tokens/default-token.png", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not load default token image: {ex.Message}");

                // Create a simple colored circle as fallback
                return CreateFallbackTokenImage();
            }
        }

        private ImageSource CreateFallbackTokenImage()
        {
            // Create a simple circle as a fallback token image
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Draw a circle with gradient
                var gradientBrush = new RadialGradientBrush(
                    Color.FromRgb(100, 149, 237),  // Cornflower blue center
                    Color.FromRgb(65, 105, 225));   // Royal blue edge
                gradientBrush.Freeze();

                var pen = new Pen(Brushes.White, 2);
                pen.Freeze();

                dc.DrawEllipse(gradientBrush, pen, new Point(24, 24), 22, 22);

                // Add a question mark or initial
                var text = new FormattedText(
                    "?",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    20,
                    Brushes.White,
                    1.0);
                dc.DrawText(text, new Point(24 - text.Width / 2, 24 - text.Height / 2));
            }

            var rtb = new RenderTargetBitmap(48, 48, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            return rtb;
        }

        #endregion

        #region Token Layout

        private void LayoutTokens()
        {
            var tokenVisuals = RenderCanvas.Children.OfType<FrameworkElement>().Where(c => c.Tag is Token);
            foreach (var vis in tokenVisuals)
            {
                var token = (Token)vis.Tag;
                Canvas.SetLeft(vis, token.GridX * GridCellSize);
                Canvas.SetTop(vis, token.GridY * GridCellSize);
                vis.Width = GridCellSize * token.SizeInSquares;
                vis.Height = GridCellSize * token.SizeInSquares;
            }
        }

        #endregion

        #region Token Property Change Handling

        private void Token_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Token token)
            {
                // Properties that require full visual rebuild
                if (e.PropertyName == nameof(Token.Conditions) ||
                    e.PropertyName == nameof(Token.IsCurrentTurn) ||
                    e.PropertyName == nameof(Token.IsConcentrating))
                {
                    // Full rebuild needed for visual effects
                    RebuildTokenVisuals();
                    return;
                }

                // HP changes - just update the HP bar
                if (e.PropertyName == nameof(Token.HP) ||
                    e.PropertyName == nameof(Token.MaxHP) ||
                    e.PropertyName == nameof(Token.TempHP))
                {
                    UpdateTokenHPBar(token);

                    // Also rebuild if HP dropped to 0 (for death visual effects)
                    if (token.HP <= 0)
                    {
                        RebuildTokenVisuals();
                    }
                    return;
                }

                // Position changes
                if (e.PropertyName == nameof(Token.GridX) ||
                    e.PropertyName == nameof(Token.GridY))
                {
                    LayoutTokens();
                    return;
                }

                // Image changes
                if (e.PropertyName == nameof(Token.Image) ||
                    e.PropertyName == nameof(Token.DisplayImage))
                {
                    RebuildTokenVisuals();
                    return;
                }
            }
        }

        private void UpdateSingleTokenVisual(Token token)
        {
            // Find the token's container
            var container = RenderCanvas.Children.OfType<FrameworkElement>()
                .FirstOrDefault(c => c.Tag is Token t && t.Id == token.Id);

            if (container is Grid grid)
            {
                // Update HP bar
                var hpBar = FindHPBar(grid);
                if (hpBar != null)
                {
                    double hpPercent = token.MaxHP > 0 ? (double)Math.Max(0, token.HP) / token.MaxHP : 0;
                    double barWidth = GridCellSize * token.SizeInSquares - 8;

                    hpBar.Width = Math.Max(0, barWidth * hpPercent);

                    // Update color
                    if (hpPercent > 0.5)
                        hpBar.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    else if (hpPercent > 0.25)
                        hpBar.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    else
                        hpBar.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }

                // Update condition badges - just rebuild them
                var oldBadges = grid.Children.OfType<WrapPanel>().FirstOrDefault();
                if (oldBadges != null)
                    grid.Children.Remove(oldBadges);

                var newBadges = CreateConditionBadges(token);
                if (newBadges != null)
                    grid.Children.Add(newBadges);

                // Update Z-index for current turn
                Canvas.SetZIndex(container, token.IsCurrentTurn ? 150 : 100);
            }
        }

        private Border FindHPBar(Grid container)
        {
            // Find the HP bar container (Grid at the bottom with the fill border)
            foreach (var child in container.Children)
            {
                if (child is Grid hpGrid && hpGrid.VerticalAlignment == VerticalAlignment.Bottom)
                {
                    // Return the fill border (second child)
                    return hpGrid.Children.OfType<Border>().Skip(1).FirstOrDefault();
                }
            }
            return null;
        }

        /// <summary>
        /// Updates just the HP bar for a specific token without rebuilding all visuals
        /// </summary>
        private void UpdateTokenHPBar(Token token)
        {
            if (token == null) return;

            // Find the container for this token
            var container = RenderCanvas.Children.OfType<Grid>()
                .FirstOrDefault(g => g.Tag is Token t && t.Id == token.Id);

            if (container == null) return;

            // Find the HP bar grid within the container
            var hpBarContainer = container.Children.OfType<Grid>()
                .FirstOrDefault(g => g.VerticalAlignment == VerticalAlignment.Bottom && g.Height == 4);

            if (hpBarContainer == null) return;

            // Calculate new HP percentage
            double hpPercent = token.MaxHP > 0 ? (double)Math.Max(0, token.HP) / token.MaxHP : 0;

            // Get the fill border (second child)
            var fillBorder = hpBarContainer.Children.OfType<Border>().Skip(1).FirstOrDefault();

            if (fillBorder != null)
            {
                // Update width
                double barWidth = hpBarContainer.Width;
                fillBorder.Width = barWidth * hpPercent;

                // Update color based on HP percentage
                Color fillColor = hpPercent > 0.5 ? Color.FromRgb(76, 175, 80) :    // Green
                                  hpPercent > 0.25 ? Color.FromRgb(255, 193, 7) :   // Yellow
                                  Color.FromRgb(244, 67, 54);                        // Red

                fillBorder.Background = new SolidColorBrush(fillColor);
            }

            // Update tooltip as well
            var img = container.Children.OfType<Image>().FirstOrDefault();
            if (img != null)
            {
                try
                {
                    img.ToolTip = CreateTokenTooltip(token);
                }
                catch { }
            }
        }

        #endregion

        #region Token HP Bar

        /// <summary>
        /// Creates a small HP bar under the token
        /// </summary>
        private FrameworkElement CreateTokenHPBar(Token token)
        {
            double hpPercent = token.MaxHP > 0 ? (double)Math.Max(0, token.HP) / token.MaxHP : 0;

            var barWidth = GridCellSize * token.SizeInSquares - 8;

            var container = new Grid
            {
                Width = barWidth,
                Height = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 2)
            };

            // Background
            container.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                CornerRadius = new CornerRadius(2),
                Opacity = 0.8
            });

            // HP fill
            var fillColor = hpPercent > 0.5 ? Color.FromRgb(76, 175, 80) :
                            hpPercent > 0.25 ? Color.FromRgb(255, 193, 7) :
                            Color.FromRgb(244, 67, 54);

            container.Children.Add(new Border
            {
                Background = new SolidColorBrush(fillColor),
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(0, barWidth * hpPercent)
            });

            return container;
        }

        #endregion
    }
}
