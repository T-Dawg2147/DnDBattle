using DnDBattle.Models;
using DnDBattle.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DnDBattle.Views
{
    public partial class SelectedTokenPanel : UserControl
    {
        private Token _token;
        private MainViewModel _vm;

        public event Action<Token, Models.Action> ActionSelected;
        public event Action<string> LogAction;

        public SelectedTokenPanel()
        {
            InitializeComponent();
            Loaded += SelectedTokenPanel_Loaded;
        }

        private void SelectedTokenPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
            {
                _vm = vm;
            }
        }

        /// <summary>
        /// Updates the panel to display the selected token
        /// </summary>
        public void SetToken(Token token)
        {
            _token = token;

            if (token == null)
            {
                NoSelectionMessage.Visibility = Visibility.Visible;
                TokenDetailsScroller.Visibility = Visibility.Collapsed;
                return;
            }

            NoSelectionMessage.Visibility = Visibility.Collapsed;
            TokenDetailsScroller.Visibility = Visibility.Visible;

            UpdateDisplay();
        }

        /// <summary>
        /// Refreshes the display with current token data
        /// </summary>
        public void UpdateDisplay()
        {
            if (_token == null) return;

            // Header
            TxtTokenName.Text = _token.Name ?? "Unknown";
            TxtTokenSubtitle.Text = $"{_token.Size} {_token.Type}".Trim();
            if (!string.IsNullOrEmpty(_token.Alignment))
                TxtTokenSubtitle.Text += $", {_token.Alignment}";

            ImgToken.Source = _token.Image;

            // Conditions
            UpdateConditionsDisplay();

            // HP
            TxtCurrentHP.Text = _token.HP.ToString();
            TxtMaxHP.Text = _token.MaxHP.ToString();
            UpdateHPColor();
            UpdateHPBar();

            // Temp HP
            if (_token.TempHP > 0)
            {
                TempHPPanel.Visibility = Visibility.Visible;
                TxtTempHP.Text = _token.TempHP.ToString();
            }
            else
            {
                TempHPPanel.Visibility = Visibility.Collapsed;
            }

            // Core Stats
            TxtAC.Text = _token.ArmorClass.ToString();
            TxtInitiative.Text = _token.Initiative > 0 ? $"+{_token.Initiative}" : (_token.Initiative == 0 ? "—" : _token.Initiative.ToString());
            TxtCR.Text = _token.ChallengeRating ?? "—";

            // Ability Scores
            SetAbilityScore(TxtStr, TxtStrMod, _token.Str);
            SetAbilityScore(TxtDex, TxtDexMod, _token.Dex);
            SetAbilityScore(TxtCon, TxtConMod, _token.Con);
            SetAbilityScore(TxtInt, TxtIntMod, _token.Int);
            SetAbilityScore(TxtWis, TxtWisMod, _token.Wis);
            SetAbilityScore(TxtCha, TxtChaMod, _token.Cha);

            // Speed
            TxtSpeed.Text = !string.IsNullOrEmpty(_token.Speed) ? _token.Speed : "30 ft.";

            // Movement (during combat)
            if (_vm?.IsInCombat == true && _token.IsCurrentTurn)
            {
                MovementSection.Visibility = Visibility.Visible;
                TxtMovementRemaining.Text = $"{_token.MovementRemainingThisTurn} / {_token.SpeedSquares} squares remaining";
                UpdateMovementBar();
            }
            else
            {
                MovementSection.Visibility = Visibility.Collapsed;
            }

            // Actions
            PopulateActions();

            // Traits
            if (!string.IsNullOrWhiteSpace(_token.Traits))
            {
                TraitsExpander.Visibility = Visibility.Visible;
                TxtTraits.Text = _token.Traits;
            }
            else
            {
                TraitsExpander.Visibility = Visibility.Collapsed;
            }

            // Notes
            TxtNotes.Text = _token.Notes ?? "";
        }

        private void SetAbilityScore(TextBlock scoreText, TextBlock modText, int score)
        {
            scoreText.Text = score.ToString();
            int mod = (score - 10) / 2;
            modText.Text = mod >= 0 ? $"+{mod}" : mod.ToString();
        }

        private void UpdateHPColor()
        {
            double hpPercent = _token.MaxHP > 0 ? (double)Math.Max(0, _token.HP) / _token.MaxHP : 0;

            if (hpPercent > 0.5)
                TxtCurrentHP.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            else if (hpPercent > 0.25)
                TxtCurrentHP.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
            else
                TxtCurrentHP.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
        }

        private void UpdateHPBar()
        {
            double hpPercent = _token.MaxHP > 0 ? (double)Math.Max(0, _token.HP) / _token.MaxHP : 0;
            HPBarFill.Width = HPBarFill.Parent is Grid parent ? parent.ActualWidth * hpPercent : 0;

            if (hpPercent > 0.5)
                HPBarFill.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            else if (hpPercent > 0.25)
                HPBarFill.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            else
                HPBarFill.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }

        private void UpdateMovementBar()
        {
            double movePercent = _token.SpeedSquares > 0
                ? (double)_token.MovementRemainingThisTurn / _token.SpeedSquares
                : 0;
            MovementBarFill.Width = MovementBarFill.Parent is Grid parent ? parent.ActualWidth * movePercent : 0;
        }

        private void UpdateConditionsDisplay()
        {
            ConditionIconsPanel.Children.Clear();

            if (_token.Conditions == Models.Condition.None)
            {
                ConditionsBar.Visibility = Visibility.Collapsed;
                return;
            }

            ConditionsBar.Visibility = Visibility.Visible;

            foreach (var condition in _token.Conditions.GetActiveConditions())
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(ConditionExtensions.GetConditionColor(condition)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 3, 6, 3),
                    Margin = new Thickness(0, 0, 5, 5),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = condition,
                    ToolTip = $"{ConditionExtensions.GetConditionName(condition)}\n{ConditionExtensions.GetConditionDescription(condition)}"
                };

                badge.Child = new TextBlock
                {
                    Text = $"{ConditionExtensions.GetConditionIcon(condition)} {ConditionExtensions.GetConditionName(condition)}",
                    Foreground = Brushes.White,
                    FontSize = 11
                };

                badge.MouseLeftButtonDown += (s, e) =>
                {
                    if (s is FrameworkElement fe && fe.Tag is Models.Condition c)
                    {
                        _token.ToggleCondition(c);
                        UpdateConditionsDisplay();
                        LogAction?.Invoke($"{_token.Name}: -{ConditionExtensions.GetConditionName(c)}");
                    }
                };

                ConditionIconsPanel.Children.Add(badge);
            }
        }

        private void PopulateActions()
        {
            ActionsPanel.Children.Clear();
            BonusActionsPanel.Children.Clear();
            ReactionsPanel.Children.Clear();
            LegendaryActionsPanel.Children.Clear();

            bool hasActions = _token.Actions?.Count > 0;
            bool hasBonusActions = _token.BonusActions?.Count > 0;
            bool hasReactions = _token.Reactions?.Count > 0;
            bool hasLegendaryActions = _token.LegendaryActions?.Count > 0;

            // Actions
            if (hasActions)
            {
                NoActionsText.Visibility = Visibility.Collapsed;
                foreach (var action in _token.Actions)
                {
                    ActionsPanel.Children.Add(CreateActionButton(action, "Action"));
                }
            }
            else
            {
                NoActionsText.Visibility = Visibility.Visible;
            }

            // Bonus Actions
            BonusActionsSection.Visibility = hasBonusActions ? Visibility.Visible : Visibility.Collapsed;
            if (hasBonusActions)
            {
                foreach (var action in _token.BonusActions)
                {
                    BonusActionsPanel.Children.Add(CreateActionButton(action, "Bonus"));
                }
            }

            // Reactions
            ReactionsSection.Visibility = hasReactions ? Visibility.Visible : Visibility.Collapsed;
            if (hasReactions)
            {
                foreach (var action in _token.Reactions)
                {
                    ReactionsPanel.Children.Add(CreateActionButton(action, "Reaction"));
                }
            }

            // Legendary Actions
            LegendaryActionsSection.Visibility = hasLegendaryActions ? Visibility.Visible : Visibility.Collapsed;
            if (hasLegendaryActions)
            {
                foreach (var action in _token.LegendaryActions)
                {
                    LegendaryActionsPanel.Children.Add(CreateActionButton(action, "Legendary"));
                }
            }
        }

        private UIElement CreateActionButton(Models.Action action, string actionType)
        {
            var button = new Button
            {
                Tag = action,
                Style = (Style)FindResource("ActionButtonStyle"),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var content = new StackPanel();

            // Action name row
            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            nameRow.Children.Add(new TextBlock
            {
                Text = action.Name ?? "Unknown Action",
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            });

            // Attack bonus badge
            if (action.AttackBonus != 0)
            {
                nameRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(8, 0, 0, 0),
                    Child = new TextBlock
                    {
                        Text = $"+{action.AttackBonus}",
                        FontSize = 10,
                        Foreground = Brushes.White
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
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(5, 0, 0, 0),
                    Child = new TextBlock
                    {
                        Text = action.DamageExpression,
                        FontSize = 10,
                        Foreground = Brushes.White
                    }
                });
            }

            content.Children.Add(nameRow);

            // Description (truncated)
            if (!string.IsNullOrEmpty(action.Description))
            {
                var desc = action.Description;
                if (desc.Length > 80)
                    desc = desc.Substring(0, 77) + "...";

                content.Children.Add(new TextBlock
                {
                    Text = desc,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 0)
                });
            }

            button.Content = content;

            // Create detailed tooltip
            button.ToolTip = CreateActionTooltip(action);

            // Click handler
            button.Click += (s, e) =>
            {
                ActionSelected?.Invoke(_token, action);
                UseAction(action, actionType);
            };

            return button;
        }

        private ToolTip CreateActionTooltip(Models.Action action)
        {
            var tooltip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                Padding = new Thickness(12),
                MaxWidth = 350
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = action.Name,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            });

            if (action.AttackBonus != 0 || !string.IsNullOrEmpty(action.Range))
            {
                var statsText = "";
                if (action.AttackBonus != 0)
                    statsText += $"Attack: +{action.AttackBonus}  ";
                if (!string.IsNullOrEmpty(action.Range))
                    statsText += $"Range: {action.Range}";

                stack.Children.Add(new TextBlock
                {
                    Text = statsText,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    Margin = new Thickness(0, 0, 0, 5)
                });
            }

            if (!string.IsNullOrEmpty(action.DamageExpression))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Damage: {action.DamageExpression}",
                    Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    Margin = new Thickness(0, 0, 0, 5)
                });
            }

            if (!string.IsNullOrEmpty(action.Description))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = action.Description,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            tooltip.Content = stack;
            return tooltip;
        }

        private void UseAction(Models.Action action, string actionType)
        {
            // Roll attack if applicable
            if (action.AttackBonus != 0)
            {
                var attackRoll = Utils.DiceRoller.RollExpression("1d20");
                int total = attackRoll.Total + action.AttackBonus ?? 0;

                string message = $"{_token.Name} uses {action.Name}: Attack roll {attackRoll.Total} + {action.AttackBonus} = {total}";

                if (attackRoll.Total == 20)
                    message += " (CRITICAL HIT!)";
                else if (attackRoll.Total == 1)
                    message += " (CRITICAL MISS!)";

                LogAction?.Invoke(message);

                // If hit, offer to roll damage
                if (!string.IsNullOrEmpty(action.DamageExpression))
                {
                    var result = MessageBox.Show(
                        $"Attack roll: {total}\n\nRoll damage ({action.DamageExpression})?",
                        action.Name,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var damageRoll = Utils.DiceRoller.RollExpression(action.DamageExpression);
                        int damage = damageRoll.Total;

                        if (attackRoll.Total == 20) // Critical hit - double dice
                        {
                            var critRoll = Utils.DiceRoller.RollExpression(action.DamageExpression);
                            damage += critRoll.Total;
                            LogAction?.Invoke($"{_token.Name} deals {damage} damage (CRIT: {damageRoll.Total} + {critRoll.Total})!");
                        }
                        else
                        {
                            LogAction?.Invoke($"{_token.Name} deals {damage} damage!");
                        }
                    }
                }
            }
            else
            {
                // Non-attack action
                LogAction?.Invoke($"{_token.Name} uses {action.Name}");
            }
        }

        #region Button Handlers

        private void BtnHeal_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Heal {_token.Name} by how much?", "Heal", "0");

            if (int.TryParse(input, out int amount) && amount > 0)
            {
                int oldHP = _token.HP;
                _token.HP = Math.Min(_token.HP + amount, _token.MaxHP);
                UpdateDisplay();
                LogAction?.Invoke($"{_token.Name} healed for {_token.HP - oldHP} HP ({_token.HP}/{_token.MaxHP})");
            }
        }

        private void BtnDamage_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Damage {_token.Name} by how much?", "Damage", "0");

            if (int.TryParse(input, out int amount) && amount > 0)
            {
                // Apply to Temp HP first
                if (_token.TempHP > 0)
                {
                    int tempDamage = Math.Min(amount, _token.TempHP);
                    _token.TempHP -= tempDamage;
                    amount -= tempDamage;
                }

                if (amount > 0)
                {
                    _token.HP = Math.Max(_token.HP - amount, 0);
                }

                UpdateDisplay();
                LogAction?.Invoke($"{_token.Name} took {amount} damage ({_token.HP}/{_token.MaxHP})");

                // Check for concentration
                if (_token.HasCondition(Models.Condition.Concentrating))
                {
                    int dc = Math.Max(10, amount / 2);
                    MessageBox.Show(
                        $"{_token.Name} must make a DC {dc} Constitution save to maintain concentration!",
                        "Concentration Check",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private void BtnResetMovement_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;
            _token.ResetMovementForTurn();
            UpdateDisplay();
            LogAction?.Invoke($"{_token.Name} movement reset");
        }

        private void BtnRollInitiative_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;
            var roll = Utils.DiceRoller.RollExpression("1d20");
            _token.Initiative = roll.Total + _token.InitiativeModifier;
            UpdateDisplay();
            LogAction?.Invoke($"{_token.Name} rolled initiative: {roll.Total} + {_token.InitiativeModifier} = {_token.Initiative}");
        }

        private void BtnDodge_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;
            _token.ToggleCondition(Models.Condition.Dodging);
            UpdateDisplay();
            LogAction?.Invoke($"{_token.Name} {(_token.HasCondition(Models.Condition.Dodging) ? "takes the Dodge action" : "stops dodging")}");
        }

        private void BtnConcentrate_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;

            if (_token.HasCondition(Models.Condition.Concentrating))
            {
                _token.RemoveCondition(Models.Condition.Concentrating);
                _token.ConcentrationSpell = null;
                LogAction?.Invoke($"{_token.Name} drops concentration");
            }
            else
            {
                string spell = Microsoft.VisualBasic.Interaction.InputBox(
                    "What spell is being concentrated on?", "Concentration", "");

                if (!string.IsNullOrWhiteSpace(spell))
                {
                    _token.AddCondition(Models.Condition.Concentrating);
                    _token.ConcentrationSpell = spell;
                    LogAction?.Invoke($"{_token.Name} concentrating on {spell}");
                }
            }
            UpdateDisplay();
        }

        private void BtnHide_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;
            _token.ToggleCondition(Models.Condition.Hidden);
            UpdateDisplay();
            LogAction?.Invoke($"{_token.Name} {(_token.HasCondition(Models.Condition.Hidden) ? "hides" : "is revealed")}");
        }

        private void TxtNotes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_token != null)
            {
                _token.Notes = TxtNotes.Text;
            }
        }

        #endregion
    }
}