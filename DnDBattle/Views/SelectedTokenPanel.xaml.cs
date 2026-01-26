using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Views
{
    public partial class SelectedTokenPanel : UserControl
    {
        private Token _token;
        private MainViewModel _vm;
        private List<QuickAction> _quickActions;

        public event Action<Token, Models.Action> ActionSelected;
        public event Action<string> LogAction;
        public event System.Action QuickActionsConfigChanged;

        public SelectedTokenPanel()
        {
            InitializeComponent();
            Loaded += SelectedTokenPanel_Loaded;

            _quickActions = QuickActionsService.GetEnabledQuickActions();
        }

        private void SelectedTokenPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
            {
                _vm = vm;
            }

            BuildQuickActionsUI();
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

            ImgToken.Source = _token.DisplayImage ?? _token.Image;

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
            // Clear all panels
            ActionsPanel.Children.Clear();
            BonusActionsPanel.Children.Clear();
            ReactionsPanel.Children.Clear();
            LegendaryActionsPanel.Children.Clear();

            if (_token == null) return;

            // Debug output
            System.Diagnostics.Debug.WriteLine($"PopulateActions for {_token?.Name}");
            System.Diagnostics.Debug.WriteLine($"  Actions: {_token?.Actions?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  BonusActions: {_token?.BonusActions?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  Reactions: {_token?.Reactions?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  LegendaryActions: {_token?.LegendaryActions?.Count ?? 0}");

            bool hasActions = _token?.Actions?.Count > 0;
            bool hasBonusActions = _token?.BonusActions?.Count > 0;
            bool hasReactions = _token?.Reactions?.Count > 0;
            bool hasLegendaryActions = _token?.LegendaryActions?.Count > 0;

            // Actions
            if (hasActions)
            {
                NoActionsText.Visibility = Visibility.Collapsed;
                foreach (var action in _token.Actions)
                {
                    ActionsPanel.Children.Add(CreateActionElement(action, "Action"));
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
                    BonusActionsPanel.Children.Add(CreateActionElement(action, "Bonus"));
                }
            }

            // Reactions
            ReactionsSection.Visibility = hasReactions ? Visibility.Visible : Visibility.Collapsed;
            if (hasReactions)
            {
                foreach (var action in _token.Reactions)
                {
                    ReactionsPanel.Children.Add(CreateActionElement(action, "Reaction"));
                }
            }

            // Legendary Actions
            LegendaryActionsSection.Visibility = hasLegendaryActions ? Visibility.Visible : Visibility.Collapsed;
            if (hasLegendaryActions)
            {
                foreach (var action in _token.LegendaryActions)
                {
                    LegendaryActionsPanel.Children.Add(CreateActionElement(action, "Legendary"));
                }
            }
        }

        /// <summary>
        /// Creates a styled action element with badges and tooltip
        /// </summary>
        private UIElement CreateActionElement(Models.Action action, string actionType)
        {
            // Main container - clickable button with hover effect
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand,
                Tag = action
            };

            // Hover effects
            border.MouseEnter += (s, e) => border.Background = new SolidColorBrush(Color.FromRgb(60, 60, 64));
            border.MouseLeave += (s, e) => border.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

            // Click to use action
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (border.Tag is Models.Action clickedAction)
                {
                    UseAction(clickedAction, actionType);
                    ActionSelected?.Invoke(_token, clickedAction);
                }
            };

            var content = new StackPanel();

            // First row: Name + badges
            var nameRow = new WrapPanel { Orientation = Orientation.Horizontal };

            // Action name
            nameRow.Children.Add(new TextBlock
            {
                Text = action.Name ?? "Unknown Action",
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });

            // Attack bonus badge (green)
            if (action.AttackBonus != null && action.AttackBonus != 0)
            {
                nameRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = action.AttackBonus > 0 ? $"+{action.AttackBonus}" : action.AttackBonus.ToString(),
                        Foreground = Brushes.White,
                        FontSize = 9,
                        FontWeight = FontWeights.Bold
                    }
                });
            }

            // Damage badge (red)
            if (!string.IsNullOrEmpty(action.DamageExpression))
            {
                nameRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(4, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = action.DamageExpression,
                        Foreground = Brushes.White,
                        FontSize = 9,
                        FontWeight = FontWeights.Bold
                    }
                });
            }

            // Range badge (blue)
            if (!string.IsNullOrEmpty(action.Range))
            {
                nameRow.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(4, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = action.Range,
                        Foreground = Brushes.Black,
                        FontSize = 9
                    }
                });
            }

            content.Children.Add(nameRow);

            // Description (truncated)
            if (!string.IsNullOrEmpty(action.Description))
            {
                string truncatedDesc = action.Description.Length > 80
                    ? action.Description.Substring(0, 77) + "..."
                    : action.Description;

                content.Children.Add(new TextBlock
                {
                    Text = truncatedDesc,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 0)
                });
            }

            border.Child = content;

            // Create detailed tooltip
            border.ToolTip = CreateActionTooltip(action);

            return border;
        }

        /// <summary>
        /// Creates a detailed tooltip for an action
        /// </summary>
        private ToolTip CreateActionTooltip(Models.Action action)
        {
            var tooltip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                MaxWidth = 350
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
                    Margin = new Thickness(0, 0, 15, 0),
                    FontWeight = FontWeights.SemiBold
                });
            }

            if (!string.IsNullOrEmpty(action.DamageExpression))
            {
                statsPanel.Children.Add(new TextBlock
                {
                    Text = $"Damage: {action.DamageExpression}",
                    Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    Margin = new Thickness(0, 0, 15, 0),
                    FontWeight = FontWeights.SemiBold
                });
            }

            if (!string.IsNullOrEmpty(action.Range))
            {
                statsPanel.Children.Add(new TextBlock
                {
                    Text = $"Range: {action.Range}",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    FontWeight = FontWeights.SemiBold
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

            // Click hint
            stack.Children.Add(new TextBlock
            {
                Text = "Click to use this action",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                FontStyle = FontStyles.Italic,
                FontSize = 10,
                Margin = new Thickness(0, 10, 0, 0)
            });

            tooltip.Content = stack;
            return tooltip;
        }

        /// <summary>
        /// Executes an action - rolls attack and damage
        /// </summary>
        private void UseAction(Models.Action action, string actionType)
        {
            if (_token == null || action == null) return;

            // Roll attack if applicable
            if (action.AttackBonus != null && action.AttackBonus != 0)
            {
                var attackRoll = Utils.DiceRoller.RollExpression("1d20");
                int total = attackRoll.Total + (action.AttackBonus ?? 0);

                string message = $"🎯 {_token.Name} uses {action.Name}: Attack roll {attackRoll.Total} + {action.AttackBonus} = {total}";

                if (attackRoll.Total == 20)
                    message += " ⚡ CRITICAL HIT!";
                else if (attackRoll.Total == 1)
                    message += " 💀 CRITICAL MISS!";

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
                            LogAction?.Invoke($"💥 {_token.Name} deals {damage} damage (CRIT: {damageRoll.Total} + {critRoll.Total})!");
                        }
                        else
                        {
                            LogAction?.Invoke($"💥 {_token.Name} deals {damage} damage ({damageRoll.Total})!");
                        }
                    }
                }
            }
            else
            {
                // Non-attack action (like abilities or spells without attack rolls)
                LogAction?.Invoke($"✨ {_token.Name} uses {action.Name}");

                // Still roll damage if there is any (like saving throw spells)
                if (!string.IsNullOrEmpty(action.DamageExpression))
                {
                    var result = MessageBox.Show(
                        $"{action.Name}\n\nRoll damage ({action.DamageExpression})?",
                        action.Name,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var damageRoll = Utils.DiceRoller.RollExpression(action.DamageExpression);
                        LogAction?.Invoke($"💥 {action.Name} deals {damageRoll.Total} damage!");
                    }
                }
            }
        }

        /// <summary>
        /// Rebuilds the quick actions buttons based on user configuration
        /// </summary>
        public void BuildQuickActionsUI()
        {
            _quickActions = QuickActionsService.GetEnabledQuickActions();

            // Find the Quick Actions section in XAML and rebuild it
            // We need to find the WrapPanel that contains the quick action buttons

            // Clear existing quick action buttons (find the WrapPanel after "Quick Actions" header)
            var quickActionsPanel = FindQuickActionsPanel();
            if (quickActionsPanel == null) return;

            quickActionsPanel.Children.Clear();

            foreach (var action in _quickActions)
            {
                var button = new Button
                {
                    Content = $"{action.Icon} {action.Name}",
                    Tag = action,
                    ToolTip = action.Description,
                    Style = (Style)FindResource("QuickActionButtonStyle"),
                    Margin = new Thickness(0, 0, 4, 4)
                };

                button.Click += QuickActionButton_Click;
                quickActionsPanel.Children.Add(button);
            }

            // Add configure button
            var configButton = new Button
            {
                Content = "⚙️",
                ToolTip = "Configure Quick Actions",
                Style = (Style)FindResource("QuickActionButtonStyle"),
                Margin = new Thickness(0, 0, 4, 4),
                Width = 28
            };
            configButton.Click += BtnConfigureQuickActions_Click;
            quickActionsPanel.Children.Add(configButton);
        }

        private WrapPanel FindQuickActionsPanel()
        {
            return QuickActionsPanel;
        }

        private void QuickActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;
            if (sender is not Button btn || btn.Tag is not QuickAction action) return;

            ExecuteQuickAction(action);
        }

        private void ExecuteQuickAction(QuickAction action)
        {
            switch (action.ActionType)
            {
                case QuickActionType.ToggleCondition:
                    // Safely get the condition value
                    Models.Condition? conditionNullable = action.ConditionToToggle;
                    if (conditionNullable.HasValue && conditionNullable.Value != Models.Condition.None)
                    {
                        Models.Condition condition = conditionNullable.Value;

                        // Special handling for Concentration
                        if (condition == Models.Condition.Concentrating)
                        {
                            HandleConcentration();
                        }
                        else
                        {
                            _token.ToggleCondition(condition);
                            bool isActive = _token.HasCondition(condition);
                            LogAction?.Invoke($"{_token.Name}: {(isActive ? "+" : "-")}{action.Name}");
                        }
                        UpdateDisplay();
                    }
                    break;

                case QuickActionType.RollInitiative:
                    RollInitiative();
                    break;

                case QuickActionType.RollSave:
                    RollSavingThrow(action.CustomCommand);
                    break;

                case QuickActionType.RollAbilityCheck:
                    RollAbilityCheck(action.CustomCommand);
                    break;

                case QuickActionType.RollDice:
                    RollDice(action.Name, action.DiceExpression);
                    break;

                case QuickActionType.Custom:
                    // Future: Handle custom actions
                    break;
            }
        }

        private void HandleConcentration()
        {
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

        private void RollInitiative()
        {
            var roll = Utils.DiceRoller.RollExpression("1d20");
            _token.Initiative = roll.Total + _token.InitiativeModifier;
            UpdateDisplay();
            LogAction?.Invoke($"{_token.Name} rolled initiative: {roll.Total} + {_token.InitiativeModifier} = {_token.Initiative}");
        }

        private void RollSavingThrow(string ability)
        {
            int modifier = GetAbilityModifier(ability);
            var roll = Utils.DiceRoller.RollExpression("1d20");
            int total = roll.Total + modifier;

            string modStr = modifier >= 0 ? $"+{modifier}" : modifier.ToString();
            LogAction?.Invoke($"{_token.Name} {ability} Save: {roll.Total} {modStr} = {total}");

            MessageBox.Show(
                $"{ability} Saving Throw\n\nRoll: {roll.Total}\nModifier: {modStr}\nTotal: {total}",
                $"{_token.Name} - {ability} Save",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RollAbilityCheck(string command)
        {
            // Format: "ABILITY:Skill" or just "ABILITY"
            var parts = command.Split(':');
            var ability = parts[0];
            var skill = parts.Length > 1 ? parts[1] : null;

            int modifier = GetAbilityModifier(ability);

            // TODO: Add proficiency if the token has the skill

            var roll = Utils.DiceRoller.RollExpression("1d20");
            int total = roll.Total + modifier;

            string checkName = skill ?? ability;
            string modStr = modifier >= 0 ? $"+{modifier}" : modifier.ToString();
            LogAction?.Invoke($"{_token.Name} {checkName}: {roll.Total} {modStr} = {total}");

            MessageBox.Show(
                $"{checkName} Check\n\nRoll: {roll.Total}\nModifier: {modStr}\nTotal: {total}",
                $"{_token.Name} - {checkName}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RollDice(string name, string expression)
        {
            var roll = Utils.DiceRoller.RollExpression(expression);
            LogAction?.Invoke($"{_token.Name} {name}: {roll.Total}");

            // Special handling for death saves
            if (name == "Death Save")
            {
                if (roll.Total >= 10)
                {
                    _token.DeathSaveSuccesses++;
                    if (roll.Total == 20)
                    {
                        _token.HP = 1;
                        _token.DeathSaveSuccesses = 0;
                        _token.DeathSaveFailures = 0;
                        LogAction?.Invoke($"{_token.Name} rolled a 20! Regains 1 HP and is conscious!");
                    }
                    else if (_token.DeathSaveSuccesses >= 3)
                    {
                        LogAction?.Invoke($"{_token.Name} is stable!");
                    }
                }
                else
                {
                    _token.DeathSaveFailures++;
                    if (roll.Total == 1)
                    {
                        _token.DeathSaveFailures++; // Natural 1 = 2 failures
                    }
                    if (_token.DeathSaveFailures >= 3)
                    {
                        LogAction?.Invoke($"{_token.Name} has died!");
                    }
                }
                UpdateDisplay();
            }
        }

        private int GetAbilityModifier(string ability)
        {
            return ability?.ToUpper() switch
            {
                "STR" => (_token.Str - 10) / 2,
                "DEX" => (_token.Dex - 10) / 2,
                "CON" => (_token.Con - 10) / 2,
                "INT" => (_token.Int - 10) / 2,
                "WIS" => (_token.Wis - 10) / 2,
                "CHA" => (_token.Cha - 10) / 2,
                _ => 0
            };
        }

        private void BtnConfigureQuickActions_Click(object sender, RoutedEventArgs e)
        {
            var allActions = QuickActionsService.GetQuickActions();
            var window = new QuickActionsSettingsWindow(allActions)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true && window.ResultActions != null)
            {
                QuickActionsService.SaveQuickActions(window.ResultActions);
                BuildQuickActionsUI();
                QuickActionsConfigChanged?.Invoke();
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