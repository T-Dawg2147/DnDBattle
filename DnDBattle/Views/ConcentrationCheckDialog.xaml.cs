using DnDBattle.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Views
{
    public partial class ConcentrationCheckDialog : Window
    {
        private Token _token;
        private int _dc;
        private int _damageTaken;
        private bool _manualMode;
        private int _autoRollResult;

        public bool SaveSucceeded { get; private set; }
        public string ResultMessage { get; private set; }
        public int RollResult { get; private set; }
        public int TotalResult { get; private set; }

        public ConcentrationCheckDialog(Token token, int damageTaken, bool manualMode)
        {
            InitializeComponent();

            _token = token;
            _damageTaken = damageTaken;
            _dc = Token.CalculateConcentrationDC(damageTaken);
            _manualMode = manualMode;

            SetupUI();
        }

        private void SetupUI()
        {
            // Header info
            TxtCreatureName.Text = _token.Name;
            TxtSpellName.Text = $"Concentrating on: {_token.ConcentrationSpell ?? "Unknown Spell"}";

            // DC and stats
            TxtDC.Text = _dc.ToString();
            TxtDamageTaken.Text = _damageTaken.ToString();

            int conMod = _token.ConcentrationSaveModifier;
            string modText = conMod >= 0 ? $"+{conMod}" : conMod.ToString();
            TxtConMod.Text = modText;
            TxtModifierDisplay.Text = modText;

            if (_manualMode)
            {
                // Manual mode - user enters roll
                ManualRollPanel.Visibility = Visibility.Visible;
                AutoRollPanel.Visibility = Visibility.Collapsed;
                TxtRoll.Focus();

                // Update total as user types
                TxtRoll.TextChanged += (s, e) => UpdateManualTotal();
            }
            else
            {
                // Auto mode - roll automatically
                ManualRollPanel.Visibility = Visibility.Collapsed;
                AutoRollPanel.Visibility = Visibility.Visible;

                var roll = Utils.DiceRoller.RollExpression("1d20");
                _autoRollResult = roll.Total;
                int total = _autoRollResult + _token.ConcentrationSaveModifier;

                TxtAutoRoll.Text = _autoRollResult.ToString();
                TxtAutoModifier.Text = modText;
                TxtAutoTotal.Text = total.ToString();

                // Color based on result
                if (_autoRollResult == 20)
                {
                    TxtAutoRoll.Text = "20 ⭐";
                    TxtAutoRoll.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                }
                else if (_autoRollResult == 1)
                {
                    TxtAutoRoll.Text = "1 💀";
                    TxtAutoRoll.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }

                // Show result preview
                ShowResultPreview(total);
            }
        }

        private void UpdateManualTotal()
        {
            if (int.TryParse(TxtRoll.Text, out int roll))
            {
                int total = roll + _token.ConcentrationSaveModifier;
                TxtTotal.Text = total.ToString();
                ShowResultPreview(total);
            }
            else
            {
                TxtTotal.Text = "?";
                ResultPreview.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowResultPreview(int total)
        {
            ResultPreview.Visibility = Visibility.Visible;

            if (total >= _dc)
            {
                ResultPreview.Background = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                TxtResultPreview.Text = $"✓ SUCCESS! Concentration maintained.";
                TxtResultPreview.Foreground = new SolidColorBrush(Color.FromRgb(129, 199, 132));
            }
            else
            {
                ResultPreview.Background = new SolidColorBrush(Color.FromRgb(183, 28, 28));
                TxtResultPreview.Text = $"✗ FAILED! Concentration broken on {_token.ConcentrationSpell}!";
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
                DialogResult = false;
                Close();
            }
        }

        private void BtnAutoFail_Click(object sender, RoutedEventArgs e)
        {
            // Auto-fail (for incapacitated, etc.)
            RollResult = 0;
            TotalResult = 0;
            SaveSucceeded = false;
            ResultMessage = $"💔 {_token.Name} automatically fails concentration and loses {_token.ConcentrationSpell}!";

            _token.BreakConcentration();

            DialogResult = true;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            int roll;
            int total;

            if (_manualMode)
            {
                if (!int.TryParse(TxtRoll.Text, out roll))
                {
                    MessageBox.Show("Please enter a valid roll.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtRoll.Focus();
                    TxtRoll.SelectAll();
                    return;
                }
                total = roll + _token.ConcentrationSaveModifier;
            }
            else
            {
                roll = _autoRollResult;
                total = roll + _token.ConcentrationSaveModifier;
            }

            RollResult = roll;
            TotalResult = total;
            SaveSucceeded = total >= _dc;

            string modText = _token.ConcentrationSaveModifier >= 0
                ? $"+{_token.ConcentrationSaveModifier}"
                : _token.ConcentrationSaveModifier.ToString();

            if (SaveSucceeded)
            {
                ResultMessage = $"🎯 {_token.Name} CON save: {roll}{modText} = {total} vs DC {_dc} - ✓ Concentration maintained!";
            }
            else
            {
                ResultMessage = $"🎯 {_token.Name} CON save: {roll}{modText} = {total} vs DC {_dc} - ✗ Concentration broken!";
                _token.BreakConcentration();
            }

            DialogResult = true;
            Close();
        }
    }
}