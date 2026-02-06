using DnDBattle.Models;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
using DnDBattle.Views.Combat;
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
    public partial class DeathSaveDialog : Window
    {
        private Token _token;
        private bool _manualMode;
        private int _autoRollResult;

        public string ResultMessage { get; private set; }
        public int RollResult { get; private set; }

        public DeathSaveDialog(Token token, bool manualMode)
        {
            InitializeComponent();

            _token = token;
            _manualMode = manualMode;

            SetupUI();
        }

        private void SetupUI()
        {
            TxtCreatureName.Text = _token.Name;

            // Show current death save state BEFORE this roll
            UpdateDeathSaveIndicators();

            if (_manualMode)
            {
                ManualRollPanel.Visibility = Visibility.Visible;
                AutoRollPanel.Visibility = Visibility.Collapsed;
                TxtRoll.Focus();

                TxtRollHint.Text = "Nat 1 = 2 failures, Nat 20 = regain 1 HP!";
                TxtRoll.TextChanged += (s, e) => UpdateManualPreview();
            }
            else
            {
                ManualRollPanel.Visibility = Visibility.Collapsed;
                AutoRollPanel.Visibility = Visibility.Visible;

                var roll = Utils.DiceRoller.RollExpression("1d20");
                _autoRollResult = roll.Total;

                TxtAutoRoll.Text = _autoRollResult.ToString();

                // Style based on roll
                if (_autoRollResult == 20)
                {
                    TxtAutoRoll.Text = "20 🌟";
                    TxtAutoRoll.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                }
                else if (_autoRollResult == 1)
                {
                    TxtAutoRoll.Text = "1 💀";
                    TxtAutoRoll.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
                else if (_autoRollResult >= 10)
                {
                    TxtAutoRoll.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
                else
                {
                    TxtAutoRoll.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }

                ShowResultPreview(_autoRollResult);
            }
        }

        /// <summary>
        /// Updates the death save indicators to show current state
        /// </summary>
        private void UpdateDeathSaveIndicators()
        {
            // Current successes
            int successes = _token.DeathSaveSuccesses;
            int failures = _token.DeathSaveFailures;

            System.Diagnostics.Debug.WriteLine($"Death saves for {_token.Name}: {successes} successes, {failures} failures");

            // Fill success indicators
            FillIndicator(Success1, successes >= 1, true);
            FillIndicator(Success2, successes >= 2, true);
            FillIndicator(Success3, successes >= 3, true);

            // Fill failure indicators
            FillIndicator(Failure1, failures >= 1, false);
            FillIndicator(Failure2, failures >= 2, false);
            FillIndicator(Failure3, failures >= 3, false);
        }

        private void FillIndicator(Ellipse indicator, bool filled, bool isSuccess)
        {
            if (filled)
            {
                indicator.Fill = new SolidColorBrush(isSuccess
                    ? Color.FromRgb(76, 175, 80)
                    : Color.FromRgb(244, 67, 54));
            }
            else
            {
                indicator.Fill = Brushes.Transparent;
            }
        }

        private void UpdateManualPreview()
        {
            if (int.TryParse(TxtRoll.Text, out int roll))
            {
                ShowResultPreview(roll);
            }
            else
            {
                ResultPreview.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowResultPreview(int roll)
        {
            ResultPreview.Visibility = Visibility.Visible;

            // Calculate what the NEW totals would be after this roll
            int newSuccesses = _token.DeathSaveSuccesses;
            int newFailures = _token.DeathSaveFailures;

            if (roll == 20)
            {
                ResultPreview.Background = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                TxtResultPreview.Text = $"🌟 NATURAL 20! {_token.Name} regains 1 HP and wakes up!";
                TxtResultPreview.Foreground = Brushes.Black;
            }
            else if (roll == 1)
            {
                newFailures += 2;
                ResultPreview.Background = new SolidColorBrush(Color.FromRgb(183, 28, 28));
                if (newFailures >= 3)
                    TxtResultPreview.Text = $"💀 NATURAL 1! Two failures - {_token.Name} DIES!";
                else
                    TxtResultPreview.Text = $"💀 NATURAL 1! Two death save failures! ({newSuccesses}✓ / {newFailures}✗)";
                TxtResultPreview.Foreground = Brushes.White;
            }
            else if (roll >= 10)
            {
                newSuccesses += 1;
                ResultPreview.Background = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                if (newSuccesses >= 3)
                    TxtResultPreview.Text = $"✓ SUCCESS! {_token.Name} is STABILIZED!";
                else
                    TxtResultPreview.Text = $"✓ SUCCESS! ({newSuccesses}✓ / {newFailures}✗)";
                TxtResultPreview.Foreground = new SolidColorBrush(Color.FromRgb(129, 199, 132));
            }
            else
            {
                newFailures += 1;
                ResultPreview.Background = new SolidColorBrush(Color.FromRgb(183, 28, 28));
                if (newFailures >= 3)
                    TxtResultPreview.Text = $"✗ FAILURE! {_token.Name} DIES!";
                else
                    TxtResultPreview.Text = $"✗ FAILURE! ({newSuccesses}✓ / {newFailures}✗)";
                TxtResultPreview.Foreground = new SolidColorBrush(Color.FromRgb(239, 154, 154));
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnConfirm_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                BtnCancel_Click(this, new RoutedEventArgs());
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            int roll;

            if (_manualMode)
            {
                if (!int.TryParse(TxtRoll.Text, out roll))
                {
                    MessageBox.Show("Please enter a valid roll (1-20).", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtRoll.Focus();
                    TxtRoll.SelectAll();
                    return;
                }

                // Validate roll is in range
                if (roll < 1 || roll > 20)
                {
                    MessageBox.Show("Roll must be between 1 and 20.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtRoll.Focus();
                    TxtRoll.SelectAll();
                    return;
                }
            }
            else
            {
                roll = _autoRollResult;
            }

            RollResult = roll;

            // Record the death save - this updates the token's DeathSaveSuccesses/Failures
            ResultMessage = _token.RecordDeathSave(roll);

            System.Diagnostics.Debug.WriteLine($"After death save: {_token.DeathSaveSuccesses} successes, {_token.DeathSaveFailures} failures");

            DialogResult = true;
            Close();
        }
    }
}