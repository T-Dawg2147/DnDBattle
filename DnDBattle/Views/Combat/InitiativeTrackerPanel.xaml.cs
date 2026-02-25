using DnDBattle.Models;
using DnDBattle.Utils;
using DnDBattle.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    public partial class InitiativeTrackerPanel : UserControl
    {
        public ObservableCollection<InitiativeEntry> Entries { get; } = new ObservableCollection<InitiativeEntry>();

        public event Action<string> LogAction;
        public event Action<Token> TokenSelected;
        public event System.Action CombatStarted;
        public event System.Action CombatEnded;
        public event System.Action TurnChanged;

        private MainViewModel _vm;
        private int _currentRound = 1;
        private int _currentTurnIndex = -1;
        private bool _isInCombat;
        private Point _dragStartPoint;
        private InitiativeEntry _draggedEntry;

        public bool IsInCombat
        {
            get => _isInCombat;
            private set
            {
                _isInCombat = value;
                UpdateCombatButton();
            }
        }

        public int CurrentRound => _currentRound;
        public InitiativeEntry CurrentTurnEntry => _currentTurnIndex >= 0 && _currentTurnIndex < Entries.Count
            ? Entries[_currentTurnIndex]
            : null;

        public InitiativeTrackerPanel()
        {
            InitializeComponent();
            InitiativeList.ItemsSource = Entries;
            UpdateDisplay();
        }

        public void SetViewModel(MainViewModel vm)
        {
            _vm = vm;

            // Sync tokens when collection changes
            if (_vm != null)
            {
                _vm.Tokens.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null)
                    {
                        foreach (Token token in e.NewItems)
                        {
                            if (!Entries.Any(entry => entry.Token?.Id == token.Id))
                            {
                                // Optionally auto-add new tokens
                            }
                        }
                    }
                    if (e.OldItems != null)
                    {
                        foreach (Token token in e.OldItems)
                        {
                            var entry = Entries.FirstOrDefault(ent => ent.Token?.Id == token.Id);
                            if (entry != null)
                            {
                                Entries.Remove(entry);
                            }
                        }
                    }
                    UpdateDisplay();
                };
            }
        }

        /// <summary>
        /// Populates the tracker with tokens from the map
        /// </summary>
        public void PopulateFromTokens(ObservableCollection<Token> tokens)
        {
            Entries.Clear();
            foreach (var token in tokens)
            {
                Entries.Add(new InitiativeEntry(token));
            }
            UpdateDisplay();
        }

        /// <summary>
        /// Adds a single token to the tracker
        /// </summary>
        public void AddToken(Token token)
        {
            if (Entries.Any(e => e.Token?.Id == token.Id))
                return;

            var entry = new InitiativeEntry(token);

            // If combat is active, roll initiative for the new creature
            if (IsInCombat)
            {
                var roll = DiceRoller.RollExpression("1d20");
                entry.InitiativeRoll = roll.Total;
                entry.InitiativeTotal = roll.Total + token.InitiativeModifier;
                token.Initiative = entry.InitiativeTotal;
                LogAction?.Invoke($"{token.Name} joins combat! Initiative: {roll.Total} + {token.InitiativeModifier} = {entry.InitiativeTotal}");
            }

            Entries.Add(entry);
            SortByInitiative();
            UpdateDisplay();
        }

        /// <summary>
        /// Removes a token from the tracker
        /// </summary>
        public void RemoveToken(Token token)
        {
            var entry = Entries.FirstOrDefault(e => e.Token?.Id == token.Id);
            if (entry != null)
            {
                bool wasCurrentTurn = entry.IsCurrentTurn;
                Entries.Remove(entry);

                if (wasCurrentTurn && Entries.Count > 0)
                {
                    _currentTurnIndex = Math.Min(_currentTurnIndex, Entries.Count - 1);
                    SetCurrentTurn(_currentTurnIndex);
                }

                UpdateDisplay();
            }
        }

        // VISUAL REFRESH - INITIATIVE_TRACKER
        private void UpdateDisplay()
        {
            TxtRound.Text = _currentRound.ToString();
            TxtTurnCount.Text = $"{Entries.Count} creature{(Entries.Count != 1 ? "s" : "")}";
            UpdateCombatButton();
        }

        // VISUAL REFRESH - INITIATIVE_TRACKER
        private void UpdateCombatButton()
        {
            if (IsInCombat)
            {
                BtnStartCombat.Content = "⏹️ End Combat";
                BtnStartCombat.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            else
            {
                BtnStartCombat.Content = "⚔️ Start Combat";
                BtnStartCombat.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
        }

        #region Combat Flow

        private void BtnStartCombat_Click(object sender, RoutedEventArgs e)
        {
            if (IsInCombat)
            {
                EndCombat();
            }
            else
            {
                StartCombat();
            }
        }

        public void StartCombat()
        {
            if (Entries.Count == 0)
            {
                // Auto-populate from map tokens
                if (_vm?.Tokens != null && _vm.Tokens.Count > 0)
                {
                    PopulateFromTokens(_vm.Tokens);
                }
                else
                {
                    MessageBox.Show("Add creatures to the map first!", "No Creatures",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            // Always roll initiative when starting combat
            RollAllInitiative();
            SortByInitiative();

            _currentRound = 1;
            _currentTurnIndex = 0;
            IsInCombat = true;

            // Set first creature's turn
            SetCurrentTurn(0);

            if (_vm != null)
                _vm.IsInCombat = true;

            LogAction?.Invoke($"⚔️ Combat started! Round {_currentRound}");
            CombatStarted?.Invoke();
            UpdateDisplay();
        }

        public void EndCombat()
        {
            IsInCombat = false;
            _currentTurnIndex = -1;

            // Clear all turn markers
            foreach (var entry in Entries)
            {
                entry.IsCurrentTurn = false;
                entry.HasActed = false;
                entry.IsDelaying = false;
                entry.IsReadying = false;
            }

            if (_vm != null)
                _vm.IsInCombat = false;

            LogAction?.Invoke("🏁 Combat ended");
            CombatEnded?.Invoke();
            UpdateDisplay();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            NextTurn();
        }

        public void NextTurn()
        {
            if (!IsInCombat || Entries.Count == 0) return;

            // Mark current as acted
            if (CurrentTurnEntry != null)
            {
                CurrentTurnEntry.HasActed = true;
                CurrentTurnEntry.IsCurrentTurn = false;

                // Reset movement for the token
                CurrentTurnEntry.Token?.ResetMovementForTurn();
            }

            // Find next non-delaying creature
            int startIndex = _currentTurnIndex;
            do
            {
                _currentTurnIndex++;

                // Check for new round
                if (_currentTurnIndex >= Entries.Count)
                {
                    _currentTurnIndex = 0;
                    _currentRound++;

                    // Reset all entries for new round
                    foreach (var entry in Entries)
                    {
                        entry.ResetForNewRound();
                    }

                    LogAction?.Invoke($"📢 Round {_currentRound} begins!");
                }

                // Skip delaying creatures (they'll jump back in when ready)
                if (!Entries[_currentTurnIndex].IsDelaying)
                    break;

            } while (_currentTurnIndex != startIndex);

            SetCurrentTurn(_currentTurnIndex);
            UpdateDisplay();
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            PreviousTurn();
        }

        public void PreviousTurn()
        {
            if (!IsInCombat || Entries.Count == 0) return;

            // Clear current turn
            if (CurrentTurnEntry != null)
            {
                CurrentTurnEntry.IsCurrentTurn = false;
                CurrentTurnEntry.HasActed = false;
            }

            _currentTurnIndex--;
            if (_currentTurnIndex < 0)
            {
                _currentTurnIndex = Entries.Count - 1;
                if (_currentRound > 1)
                {
                    _currentRound--;
                    LogAction?.Invoke($"↩️ Back to Round {_currentRound}");
                }
            }

            SetCurrentTurn(_currentTurnIndex);
            UpdateDisplay();
        }

        // VISUAL REFRESH - INITIATIVE_TRACKER
        private void SetCurrentTurn(int index)
        {
            if (index < 0 || index >= Entries.Count) return;

            // Clear all turn markers
            foreach (var entry in Entries)
            {
                entry.IsCurrentTurn = false;
            }

            // Set new current turn
            _currentTurnIndex = index;
            var current = Entries[index];
            current.IsCurrentTurn = true;
            current.Token?.ResetMovementForTurn();

            // Update VM
            if (_vm != null)
            {
                _vm.CurrentTurnToken = current.Token;
                _vm.SelectedToken = current.Token;
            }

            LogAction?.Invoke($"➡️ {current.DisplayName}'s turn");
            TokenSelected?.Invoke(current.Token);
            TurnChanged?.Invoke();
        }

        #endregion

        #region Initiative Rolling

        private void BtnRollAll_Click(object sender, RoutedEventArgs e)
        {
            RollAllInitiative();
        }

        public void RollAllInitiative()
        {
            foreach (var entry in Entries)
            {
                if (entry.Token == null) continue;

                var roll = DiceRoller.RollExpression("1d20");
                entry.InitiativeRoll = roll.Total;
                entry.InitiativeTotal = roll.Total + entry.Token.InitiativeModifier;
                entry.Token.Initiative = entry.InitiativeTotal;

                string modStr = entry.Token.InitiativeModifier >= 0
                    ? $"+{entry.Token.InitiativeModifier}"
                    : entry.Token.InitiativeModifier.ToString();

                LogAction?.Invoke($"🎲 {entry.DisplayName}: {roll.Total} {modStr} = {entry.InitiativeTotal}");
            }

            SortByInitiative();
            UpdateDisplay();
        }

        public void RollSingleInitiative(InitiativeEntry entry)
        {
            if (entry?.Token == null) return;

            var roll = DiceRoller.RollExpression("1d20");
            entry.InitiativeRoll = roll.Total;
            entry.InitiativeTotal = roll.Total + entry.Token.InitiativeModifier;
            entry.Token.Initiative = entry.InitiativeTotal;

            LogAction?.Invoke($"🎲 {entry.DisplayName} rolls initiative: {entry.InitiativeTotal}");

            if (IsInCombat)
            {
                SortByInitiative();
            }
            UpdateDisplay();
        }

        // VISUAL REFRESH - INITIATIVE_TRACKER
        private void SortByInitiative()
        {
            var sorted = Entries.OrderByDescending(e => e.InitiativeTotal)
                                .ThenByDescending(e => e.Token?.Dex ?? 0)
                                .ToList();

            Entries.Clear();
            foreach (var entry in sorted)
            {
                Entries.Add(entry);
            }

            // Update current turn index if in combat
            if (IsInCombat && CurrentTurnEntry != null)
            {
                _currentTurnIndex = Entries.IndexOf(CurrentTurnEntry);
            }
        }

        #endregion

        #region Delay and Ready Actions

        private void BtnDelay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InitiativeEntry entry)
            {
                ToggleDelay(entry);
            }
        }

        public void ToggleDelay(InitiativeEntry entry)
        {
            entry.IsDelaying = !entry.IsDelaying;
            entry.IsReadying = false;

            if (entry.IsDelaying)
            {
                LogAction?.Invoke($"⏸️ {entry.DisplayName} is delaying their turn");

                // If it's their turn and they delay, move to next
                if (entry.IsCurrentTurn)
                {
                    NextTurn();
                }
            }
            else
            {
                LogAction?.Invoke($"▶️ {entry.DisplayName} takes their delayed turn");

                // Jump them into initiative right after current creature
                if (IsInCombat)
                {
                    int insertIndex = _currentTurnIndex + 1;
                    if (insertIndex > Entries.Count) insertIndex = Entries.Count;

                    Entries.Remove(entry);
                    Entries.Insert(insertIndex, entry);

                    SetCurrentTurn(insertIndex);
                }
            }

            UpdateDisplay();
        }

        private void BtnReady_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InitiativeEntry entry)
            {
                PromptReadyAction(entry);
            }
        }

        private void PromptReadyAction(InitiativeEntry entry)
        {
            if (entry.IsReadying)
            {
                // Trigger the readied action
                entry.IsReadying = false;
                entry.ReadiedAction = null;
                LogAction?.Invoke($"⚡ {entry.DisplayName} uses their readied action!");
            }
            else
            {
                string action = Microsoft.VisualBasic.Interaction.InputBox(
                    "What action is being readied?\n(e.g., 'Attack when enemy approaches', 'Cast spell when door opens')",
                    "Ready Action",
                    "Attack");

                if (!string.IsNullOrWhiteSpace(action))
                {
                    entry.IsReadying = true;
                    entry.IsDelaying = false;
                    entry.ReadiedAction = action;
                    LogAction?.Invoke($"⚡ {entry.DisplayName} readies an action: {action}");

                    // Move to next turn
                    if (entry.IsCurrentTurn)
                    {
                        NextTurn();
                    }
                }
            }

            UpdateDisplay();
        }

        #endregion

        #region Drag and Drop Reordering

        private void Entry_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);

            if (sender is Border border && border.Tag is InitiativeEntry entry)
            {
                // Select the token
                TokenSelected?.Invoke(entry.Token);
                if (_vm != null)
                {
                    _vm.SelectedToken = entry.Token;
                }
            }
        }

        private void Entry_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            Point currentPos = e.GetPosition(null);
            Vector diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is Border border && border.Tag is InitiativeEntry entry)
                {
                    _draggedEntry = entry;
                    DragDrop.DoDragDrop(border, entry, DragDropEffects.Move);
                    _draggedEntry = null;
                }
            }
        }

        private void Entry_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void Entry_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border border && border.Tag is InitiativeEntry targetEntry)
            {
                if (e.Data.GetData(typeof(InitiativeEntry)) is InitiativeEntry sourceEntry)
                {
                    if (sourceEntry != targetEntry)
                    {
                        int sourceIndex = Entries.IndexOf(sourceEntry);
                        int targetIndex = Entries.IndexOf(targetEntry);

                        Entries.Move(sourceIndex, targetIndex);

                        // Update current turn index
                        if (IsInCombat && CurrentTurnEntry != null)
                        {
                            _currentTurnIndex = Entries.IndexOf(CurrentTurnEntry);
                        }

                        LogAction?.Invoke($"📋 Initiative order changed: {sourceEntry.DisplayName} moved");
                        UpdateDisplay();
                    }
                }
            }
            e.Handled = true;
        }

        private void InitiativeList_Drop(object sender, DragEventArgs e)
        {
            // Handle drop on empty space
            e.Handled = true;
        }

        #endregion

        #region Other Actions

        private void BtnAddCreature_Click(object sender, RoutedEventArgs e)
        {
            // Add all tokens from map that aren't already in the tracker
            if (_vm?.Tokens != null)
            {
                int added = 0;
                foreach (var token in _vm.Tokens)
                {
                    if (!Entries.Any(ent => ent.Token?.Id == token.Id))
                    {
                        AddToken(token);
                        added++;
                    }
                }

                if (added > 0)
                {
                    LogAction?.Invoke($"➕ Added {added} creature(s) to initiative");
                }
                else
                {
                    MessageBox.Show("All creatures from the map are already in the tracker.",
                        "No New Creatures", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Reset all initiative rolls? This will clear all rolls but keep creatures in the tracker.",
                "Reset Initiative",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var entry in Entries)
                {
                    entry.InitiativeRoll = 0;
                    entry.InitiativeTotal = 0;
                    entry.HasActed = false;
                    entry.IsDelaying = false;
                    entry.IsReadying = false;
                    if (entry.Token != null)
                        entry.Token.Initiative = 0;
                }

                _currentRound = 1;
                LogAction?.Invoke("🔄 Initiative reset");
                UpdateDisplay();
            }
        }

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            SortByInitiative();
            LogAction?.Invoke("🎯 Initiative sorted");
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "End combat and clear the initiative tracker?",
                "Clear Tracker",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                EndCombat();
                Entries.Clear();
                _currentRound = 1;
                LogAction?.Invoke("🗑️ Initiative tracker cleared");
                UpdateDisplay();
            }
        }

        #endregion
    }

}