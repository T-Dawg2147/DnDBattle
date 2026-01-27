using DnDBattle.Models;
using DnDBattle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DnDBattle.Views
{
    public partial class SelectedTokenPanel : UserControl
    {
        private Token _token;
        private ViewModels.MainViewModel _vm;
        private List<QuickAction> _quickActions;
        private ActionTargetingService _targetingService;

        // Events
        public event Action<string> LogAction;
        public event Action<Token, Models.Action> ActionSelected;
        public event Action<TargetingState> TargetingStarted;
        public event System.Action TargetingCancelled;
        public event Action<ActionResult> ActionResolved;

        public ActionTargetingService TargetingService => _targetingService;
        public bool IsTargeting => _targetingService?.CurrentState?.IsTargeting ?? false;

        public SelectedTokenPanel()
        {
            InitializeComponent();
            Loaded += SelectedTokenPanel_Loaded;

            _quickActions = QuickActionsService.GetEnabledQuickActions();
            _targetingService = new ActionTargetingService();
            _targetingService.LogMessage += (msg) => LogAction?.Invoke(msg);
        }

        private void SelectedTokenPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                _vm = vm;
            }
        }

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

            // Concentration
            UpdateConcentrationDisplay();

            // Death Saves
            UpdateDeathSavesDisplay();

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

            // Stats
            TxtAC.Text = _token.ArmorClass.ToString();
            TxtInitiative.Text = _token.InitiativeModifier >= 0 ? $"+{_token.InitiativeModifier}" : _token.InitiativeModifier.ToString();
            TxtCR.Text = _token.ChallengeRating ?? "-";

            // Ability Scores
            UpdateAbilityScores();

            // Speed
            TxtSpeed.Text = _token.Speed ?? "30 ft.";

            // Legendary Actions
            UpdateLegendaryActionsDisplay();

            // Spell Slots
            UpdateSpellSlotsDisplay();

            // Notes
            UpdateNotesDisplay();

            // Actions
            PopulateActions();

            // Quick Actions
            PopulateQuickActions();
        }

        #region HP Management

        private void UpdateHPColor()
        {
            double percent = _token.MaxHP > 0 ? (double)_token.HP / _token.MaxHP : 0;

            if (percent > 0.5)
                TxtCurrentHP.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            else if (percent > 0.25)
                TxtCurrentHP.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            else
                TxtCurrentHP.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }

        private void UpdateHPBar()
        {
            double percent = _token.MaxHP > 0 ? (double)Math.Max(0, _token.HP) / _token.MaxHP : 0;
            HPBarFill.Width = ActualWidth > 0 ? (ActualWidth - 44) * percent : 100 * percent;

            if (percent > 0.5)
                HPBarFill.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            else if (percent > 0.25)
                HPBarFill.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            else
                HPBarFill.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }

        private void BtnHeal_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox("Heal amount:", "Heal", "1");
            if (int.TryParse(input, out int amount) && amount > 0)
            {
                _token.HP = Math.Min(_token.HP + amount, _token.MaxHP);
                if (_token.HP > 0) _token.ResetDeathSaves();
                LogAction?.Invoke($"💚 {_token.Name} healed for {amount} HP ({_token.HP}/{_token.MaxHP})");
                UpdateDisplay();
            }
        }

        private void BtnDamage_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox("Damage amount:", "Damage", "1");
            if (int.TryParse(input, out int amount) && amount > 0)
            {
                bool wasConcentrating = _token.IsConcentrating;
                _token.HP = Math.Max(0, _token.HP - amount);
                LogAction?.Invoke($"💔 {_token.Name} took {amount} damage ({_token.HP}/{_token.MaxHP})");

                if (wasConcentrating && _token.IsConcentrating)
                {
                    PromptConcentrationCheck(amount);
                }

                UpdateDisplay();
            }
        }

        private void BtnResetMovement_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;
            _token.ResetMovementForNewTurn();
            UpdateDisplay();
        }

        #endregion

        #region Ability Scores

        private void UpdateAbilityScores()
        {
            TxtStr.Text = _token.Str.ToString();
            TxtStrMod.Text = FormatModifier((_token.Str - 10) / 2);

            TxtDex.Text = _token.Dex.ToString();
            TxtDexMod.Text = FormatModifier((_token.Dex - 10) / 2);

            TxtCon.Text = _token.Con.ToString();
            TxtConMod.Text = FormatModifier((_token.Con - 10) / 2);

            TxtInt.Text = _token.Int.ToString();
            TxtIntMod.Text = FormatModifier((_token.Int - 10) / 2);

            TxtWis.Text = _token.Wis.ToString();
            TxtWisMod.Text = FormatModifier((_token.Wis - 10) / 2);

            TxtCha.Text = _token.Cha.ToString();
            TxtChaMod.Text = FormatModifier((_token.Cha - 10) / 2);
        }

        private string FormatModifier(int mod)
        {
            return mod >= 0 ? $"+{mod}" : mod.ToString();
        }

        #endregion

        #region Conditions Display

        private void UpdateConditionsDisplay()
        {
            ConditionIconsPanel.Children.Clear();

            if (_token.Conditions == Models.Condition.None)
            {
                ConditionsBar.Visibility = Visibility.Collapsed;
                return;
            }

            var activeConditions = _token.Conditions.GetActiveConditions().ToList();
            if (activeConditions.Count == 0)
            {
                ConditionsBar.Visibility = Visibility.Collapsed;
                return;
            }

            ConditionsBar.Visibility = Visibility.Visible;

            foreach (var condition in activeConditions)
            {
                var badge = new Border
                {
                    Width = 20,
                    Height = 20,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(ConditionExtensions.GetConditionColor(condition)),
                    Margin = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = ConditionExtensions.GetConditionIcon(condition),
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                badge.ToolTip = ConditionExtensions.GetConditionName(condition);
                ConditionIconsPanel.Children.Add(badge);
            }
        }

        #endregion

        #region Concentration Tracking

        private void UpdateConcentrationDisplay()
        {
            if (_token == null || !_token.IsConcentrating)
            {
                ConcentrationBar.Visibility = Visibility.Collapsed;
                return;
            }

            ConcentrationBar.Visibility = Visibility.Visible;
            TxtConcentrationSpell.Text = _token.ConcentrationSpell ?? "Unknown Spell";
        }

        private void BtnBreakConcentration_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null || !_token.IsConcentrating) return;

            string spell = _token.ConcentrationSpell;
            _token.BreakConcentration();
            UpdateConcentrationDisplay();
            LogAction?.Invoke($"💔 {_token.Name} loses concentration on {spell}");
        }

        public void PromptConcentrationCheck(int damageTaken)
        {
            if (_token == null || !_token.IsConcentrating) return;

            bool manualMode = !Options.LiveMode;

            var dialog = new ConcentrationCheckDialog(_token, damageTaken, manualMode)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                LogAction?.Invoke(dialog.ResultMessage);
                UpdateConcentrationDisplay();
                UpdateDisplay();
            }
        }

        #endregion

        #region Death Saves Tracking

        private void UpdateDeathSavesDisplay()
        {
            if (_token == null || _token.HP > 0)
            {
                DeathSavesPanel.Visibility = Visibility.Collapsed;
                return;
            }

            DeathSavesPanel.Visibility = Visibility.Visible;

            FillDeathSaveIndicator(DeathSuccess1, _token.DeathSaveSuccesses >= 1, true);
            FillDeathSaveIndicator(DeathSuccess2, _token.DeathSaveSuccesses >= 2, true);
            FillDeathSaveIndicator(DeathSuccess3, _token.DeathSaveSuccesses >= 3, true);

            FillDeathSaveIndicator(DeathFailure1, _token.DeathSaveFailures >= 1, false);
            FillDeathSaveIndicator(DeathFailure2, _token.DeathSaveFailures >= 2, false);
            FillDeathSaveIndicator(DeathFailure3, _token.DeathSaveFailures >= 3, false);

            if (_token.IsStabilized)
            {
                TxtDeathSaveStatus.Text = "💤 Stabilized";
                TxtDeathSaveStatus.Foreground = new SolidColorBrush(Color.FromRgb(129, 199, 132));
                BtnRollDeathSave.Visibility = Visibility.Collapsed;
            }
            else if (_token.IsDead)
            {
                TxtDeathSaveStatus.Text = "💀 DEAD";
                TxtDeathSaveStatus.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                BtnRollDeathSave.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtDeathSaveStatus.Text = $"{_token.DeathSaveSuccesses}✓ / {_token.DeathSaveFailures}✗";
                TxtDeathSaveStatus.Foreground = new SolidColorBrush(Color.FromRgb(176, 190, 197));
                BtnRollDeathSave.Visibility = Visibility.Visible;
            }
        }

        private void FillDeathSaveIndicator(Ellipse indicator, bool filled, bool isSuccess)
        {
            if (indicator == null) return;

            indicator.Fill = filled
                ? new SolidColorBrush(isSuccess ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54))
                : Brushes.Transparent;
        }

        private void BtnRollDeathSave_Click(object sender, RoutedEventArgs e)
        {
            PromptDeathSave();
        }

        public void PromptDeathSave()
        {
            if (_token == null || _token.HP > 0 || _token.IsDead || _token.IsStabilized) return;

            bool manualMode = !Options.LiveMode;

            var dialog = new DeathSaveDialog(_token, manualMode)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                LogAction?.Invoke(dialog.ResultMessage);
                UpdateDeathSavesDisplay();
                UpdateDisplay();
            }
        }

        #endregion

        #region Legendary Actions

        private void UpdateLegendaryActionsDisplay()
        {
            LegendaryPointsContainer.Children.Clear();

            if (_token == null || !_token.HasLegendaryActions)
            {
                LegendaryActionsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            LegendaryActionsPanel.Visibility = Visibility.Visible;

            for (int i = 0; i < _token.LegendaryActionsMax; i++)
            {
                bool isAvailable = i < _token.LegendaryActionsRemaining;
                var point = CreateLegendaryActionPoint(i, isAvailable);
                LegendaryPointsContainer.Children.Add(point);
            }

            TxtLegendaryRemaining.Text = $"{_token.LegendaryActionsRemaining} of {_token.LegendaryActionsMax} remaining";

            BtnResetLegendary.Visibility = _token.LegendaryActionsRemaining < _token.LegendaryActionsMax
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private Border CreateLegendaryActionPoint(int index, bool isAvailable)
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

            border.Child = new TextBlock
            {
                Text = "★",
                Foreground = isAvailable ? Brushes.Black : new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.ToolTip = isAvailable ? "Click to use" : "Click to restore";

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (isAvailable)
                {
                    _token.UseLegendaryAction(1);
                    LogAction?.Invoke($"⭐ {_token.Name} used a legendary action ({_token.LegendaryActionsRemaining}/{_token.LegendaryActionsMax})");
                }
                else
                {
                    _token.LegendaryActionsRemaining++;
                    LogAction?.Invoke($"⭐ {_token.Name} restored a legendary action ({_token.LegendaryActionsRemaining}/{_token.LegendaryActionsMax})");
                }
                UpdateLegendaryActionsDisplay();
            };

            return border;
        }

        private void BtnResetLegendary_Click(object sender, RoutedEventArgs e)
        {
            if (_token == null) return;
            _token.ResetLegendaryActions();
            LogAction?.Invoke($"⭐ {_token.Name}'s legendary actions reset to {_token.LegendaryActionsMax}");
            UpdateLegendaryActionsDisplay();
        }

        #endregion

        #region Spell Slots

        private void UpdateSpellSlotsDisplay()
        {
            SpellSlotsContainer.Children.Clear();

            if (_token?.SpellSlots == null || !_token.HasSpellSlots)
            {
                SpellSlotsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            SpellSlotsPanel.Visibility = Visibility.Visible;

            for (int level = 1; level <= 9; level++)
            {
                int max = _token.SpellSlots.GetMaxSlots(level);
                if (max == 0) continue;

                int current = _token.SpellSlots.GetCurrentSlots(level);
                var slotGroup = CreateSpellSlotGroup(level, current, max);
                SpellSlotsContainer.Children.Add(slotGroup);
            }
        }

        private Border CreateSpellSlotGroup(int level, int current, int max)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 6, 6)
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = level == 1 ? "1st" : level == 2 ? "2nd" : level == 3 ? "3rd" : $"{level}th",
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var circlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            for (int i = 0; i < max; i++)
            {
                bool isFilled = i < current;
                var circle = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Stroke = new SolidColorBrush(Color.FromRgb(186, 104, 200)),
                    StrokeThickness = 2,
                    Fill = isFilled ? new SolidColorBrush(Color.FromRgb(186, 104, 200)) : Brushes.Transparent,
                    Margin = new Thickness(2, 0, 2, 0),
                    Cursor = Cursors.Hand,
                    Tag = new Tuple<int, int>(level, i)
                };

                int slotIndex = i;
                circle.MouseLeftButtonDown += (s, e) =>
                {
                    if (slotIndex < _token.SpellSlots.GetCurrentSlots(level))
                    {
                        _token.SpellSlots.UseSlot(level);
                        LogAction?.Invoke($"✨ {_token.Name} used a level {level} spell slot");
                    }
                    else
                    {
                        _token.SpellSlots.RestoreSlot(level);
                        LogAction?.Invoke($"✨ {_token.Name} restored a level {level} spell slot");
                    }
                    UpdateSpellSlotsDisplay();
                };

                circlePanel.Children.Add(circle);
            }

            stack.Children.Add(circlePanel);
            border.Child = stack;
            return border;
        }

        private void BtnLongRest_Click(object sender, RoutedEventArgs e)
        {
            if (_token?.SpellSlots == null) return;
            _token.SpellSlots.LongRest();
            LogAction?.Invoke($"🌙 {_token.Name} completed a long rest - all spell slots restored!");
            UpdateSpellSlotsDisplay();
        }

        #endregion

        #region Notes

        private void UpdateNotesDisplay()
        {
            NotesContainer.Children.Clear();

            if (_token?.CombatNotes == null || _token.CombatNotes.Count == 0)
            {
                NotesContainer.Children.Add(new TextBlock
                {
                    Text = "No notes",
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    FontStyle = FontStyles.Italic,
                    FontSize = 10
                });
                return;
            }

            foreach (var note in _token.CombatNotes)
            {
                NotesContainer.Children.Add(CreateNoteItem(note));
            }
        }

        private Border CreateNoteItem(TokenNote note)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new TextBlock
            {
                Text = note.Type.GetIcon(),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var text = new TextBlock
            {
                Text = note.Text,
                Foreground = Brushes.White,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            var deleteBtn = new Button
            {
                Content = "×",
                Width = 18,
                Height = 18,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = note.Id,
                Margin = new Thickness(5, 0, 0, 0)
            };
            deleteBtn.Click += (s, e) =>
            {
                _token.RemoveNote(note.Id);
                UpdateNotesDisplay();
            };
            Grid.SetColumn(deleteBtn, 2);
            grid.Children.Add(deleteBtn);

            border.Child = grid;
            return border;
        }

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            QuickAddNotePanel.Visibility = Visibility.Visible;
            TxtQuickNote.Focus();
        }

        private void BtnConfirmNote_Click(object sender, RoutedEventArgs e)
        {
            AddNoteFromInput();
        }

        private void TxtQuickNote_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNoteFromInput();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                QuickAddNotePanel.Visibility = Visibility.Collapsed;
                TxtQuickNote.Text = "";
                e.Handled = true;
            }
        }

        private void AddNoteFromInput()
        {
            string text = TxtQuickNote.Text?.Trim();
            if (string.IsNullOrEmpty(text) || _token == null) return;

            _token.AddNote(text, NoteType.General);
            LogAction?.Invoke($"📝 Added note to {_token.Name}: {text}");

            TxtQuickNote.Text = "";
            QuickAddNotePanel.Visibility = Visibility.Collapsed;
            UpdateNotesDisplay();
        }

        private void TxtNotes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_token != null)
            {
                _token.Notes = TxtDMNotes.Text;
            }
        }

        #endregion

        #region Actions Display

        private void PopulateActions()
        {
            ActionsPanel.Children.Clear();
            BonusActionsPanel.Children.Clear();
            ReactionsPanel.Children.Clear();
            LegendaryActionsListPanel.Children.Clear();

            if (_token == null) return;

            bool hasActions = _token.Actions?.Count > 0;
            bool hasBonusActions = _token.BonusActions?.Count > 0;
            bool hasReactions = _token.Reactions?.Count > 0;
            bool hasLegendaryActions = _token.LegendaryActions?.Count > 0;

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

            BonusActionsSection.Visibility = hasBonusActions ? Visibility.Visible : Visibility.Collapsed;
            if (hasBonusActions)
            {
                foreach (var action in _token.BonusActions)
                {
                    BonusActionsPanel.Children.Add(CreateActionElement(action, "Bonus"));
                }
            }

            ReactionsSection.Visibility = hasReactions ? Visibility.Visible : Visibility.Collapsed;
            if (hasReactions)
            {
                foreach (var action in _token.Reactions)
                {
                    ReactionsPanel.Children.Add(CreateActionElement(action, "Reaction"));
                }
            }

            LegendaryActionsSection.Visibility = hasLegendaryActions ? Visibility.Visible : Visibility.Collapsed;
            if (hasLegendaryActions)
            {
                foreach (var action in _token.LegendaryActions)
                {
                    LegendaryActionsListPanel.Children.Add(CreateActionElement(action, "Legendary"));
                }
            }
        }

        private UIElement CreateActionElement(Models.Action action, string actionType)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand,
                Tag = action
            };

            border.MouseEnter += (s, e) => border.Background = new SolidColorBrush(Color.FromRgb(60, 60, 64));
            border.MouseLeave += (s, e) => border.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (border.Tag is Models.Action clickedAction && _token != null)
                {
                    StartActionTargeting(clickedAction, actionType);
                    e.Handled = true;
                }
            };

            var content = new StackPanel();
            var nameRow = new WrapPanel { Orientation = Orientation.Horizontal };

            nameRow.Children.Add(new TextBlock
            {
                Text = action.Name ?? "Unknown Action",
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });

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
                MaxWidth = 350
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = action.Name ?? "Unknown Action",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            });

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

        #endregion

        #region Action Targeting

        private void StartActionTargeting(Models.Action action, string actionType)
        {
            if (_token == null || action == null) return;

            _targetingService.StartTargeting(_token, action);

            ShowTargetingIndicator(action.Name);

            TargetingStarted?.Invoke(_targetingService.CurrentState);
            ActionSelected?.Invoke(_token, action);

            LogAction?.Invoke($"🎯 Select a target for {action.Name} (Click a token on the map, or press Escape to cancel)");
        }

        private void ShowTargetingIndicator(string actionName)
        {
            TxtTargetingAction.Text = $"🎯 Using: {actionName}";
            TargetingIndicator.Visibility = Visibility.Visible;
        }

        private void HideTargetingIndicator()
        {
            TargetingIndicator.Visibility = Visibility.Collapsed;
        }

        private void BtnCancelTargeting_Click(object sender, RoutedEventArgs e)
        {
            CancelTargeting();
        }

        public void CancelTargeting()
        {
            _targetingService.CancelTargeting();
            HideTargetingIndicator();
            TargetingCancelled?.Invoke();
            LogAction?.Invoke("❌ Targeting cancelled");
        }

        public TargetingState GetTargetingState() => _targetingService?.CurrentState;

        public void OnTargetSelected(Token target)
        {
            if (!_targetingService.CurrentState.IsTargeting) return;

            var (isValid, reason) = _targetingService.ValidateTarget(target, _vm?.GridCellSize ?? 48);

            if (!isValid)
            {
                LogAction?.Invoke($"❌ Invalid target: {reason}");
                return;
            }

            var state = _targetingService.CurrentState;
            bool manualMode = !Options.LiveMode;

            var dialog = new ActionResolutionDialog(
                state.SourceToken,
                target,
                state.SelectedAction,
                state.DamageType,
                manualMode)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                LogAction?.Invoke(dialog.Result.Message);
                ActionResolved?.Invoke(dialog.Result);
                UpdateDisplay();
            }

            CancelTargeting();
        }

        #endregion

        #region Quick Actions

        private void PopulateQuickActions()
        {
            QuickActionsPanel.Children.Clear();

            if (_quickActions == null) return;

            foreach (var qa in _quickActions)
            {
                var btn = new Button
                {
                    Content = $"{qa.Icon} {qa.Name}",
                    Style = (Style)FindResource("QuickActionButtonStyle"),
                    Tag = qa
                };

                btn.Click += QuickAction_Click;
                QuickActionsPanel.Children.Add(btn);
            }

            // Add Set Concentration button
            var concBtn = new Button
            {
                Content = "🎯 Set Concentration",
                Style = (Style)FindResource("QuickActionButtonStyle")
            };
            concBtn.Click += (s, e) => PromptSetConcentration();
            QuickActionsPanel.Children.Add(concBtn);
        }

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuickAction qa && _token != null)
            {
                ExecuteQuickAction(qa);
            }
        }

        private void ExecuteQuickAction(QuickAction qa)
        {
            if (_token == null) return;

            switch (qa.ActionType)
            {
                case QuickActionType.RollInitiative:
                    var initRoll = Utils.DiceRoller.RollExpression("1d20");
                    int initTotal = initRoll.Total + _token.InitiativeModifier;
                    _token.Initiative = initTotal;
                    LogAction?.Invoke($"🎲 {_token.Name} rolled initiative: {initRoll.Total} + {_token.InitiativeModifier} = {initTotal}");
                    break;

                case QuickActionType.RollSavingThrow:
                    PromptSavingThrow(qa.Parameter);
                    break;

                case QuickActionType.RollAbilityCheck:
                    PromptAbilityCheck(qa.Parameter);
                    break;

                case QuickActionType.ApplyCondition:
                    if (Enum.TryParse<Models.Condition>(qa.Parameter, out var condition))
                    {
                        _token.ToggleCondition(condition);
                        LogAction?.Invoke($"🎯 {_token.Name}: {(qa.Parameter)} {(_token.HasCondition(condition) ? "applied" : "removed")}");
                        UpdateDisplay();
                    }
                    break;

                case QuickActionType.Custom:
                    LogAction?.Invoke($"✨ {_token.Name}: {qa.Name}");
                    break;
            }
        }

        private void PromptSavingThrow(string ability)
        {
            if (_token == null) return;

            int mod = ability?.ToUpper() switch
            {
                "STR" => (_token.Str - 10) / 2,
                "DEX" => (_token.Dex - 10) / 2,
                "CON" => (_token.Con - 10) / 2,
                "INT" => (_token.Int - 10) / 2,
                "WIS" => (_token.Wis - 10) / 2,
                "CHA" => (_token.Cha - 10) / 2,
                _ => 0
            };

            var roll = Utils.DiceRoller.RollExpression("1d20");
            int total = roll.Total + mod;
            string modStr = mod >= 0 ? $"+{mod}" : mod.ToString();

            LogAction?.Invoke($"🎲 {_token.Name} {ability} Save: {roll.Total}{modStr} = {total}");
        }

        private void PromptAbilityCheck(string ability)
        {
            if (_token == null) return;

            int mod = ability?.ToUpper() switch
            {
                "STR" => (_token.Str - 10) / 2,
                "DEX" => (_token.Dex - 10) / 2,
                "CON" => (_token.Con - 10) / 2,
                "INT" => (_token.Int - 10) / 2,
                "WIS" => (_token.Wis - 10) / 2,
                "CHA" => (_token.Cha - 10) / 2,
                _ => 0
            };

            var roll = Utils.DiceRoller.RollExpression("1d20");
            int total = roll.Total + mod;
            string modStr = mod >= 0 ? $"+{mod}" : mod.ToString();

            LogAction?.Invoke($"🎲 {_token.Name} {ability} Check: {roll.Total}{modStr} = {total}");
        }

        public void PromptSetConcentration()
        {
            if (_token == null) return;

            string spellName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the spell name:",
                "Set Concentration",
                _token.ConcentrationSpell ?? "");

            if (!string.IsNullOrWhiteSpace(spellName))
            {
                _token.SetConcentration(spellName);
                UpdateConcentrationDisplay();
                LogAction?.Invoke($"🎯 {_token.Name} is now concentrating on {spellName}");
            }
        }

        #endregion
    }
}