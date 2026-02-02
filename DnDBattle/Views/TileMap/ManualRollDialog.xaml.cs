using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Views.TileMap
{
    public partial class ManualRollDialog : Window
    {
        public int Roll { get; private set; }
        private int _modifier;
        private int _dc;

        public ManualRollDialog(string characterName, string skillName, int modifier, int dc)
        {
            InitializeComponent();

            _modifier = modifier;
            _dc = dc;

            TxtHeader.Text = $"🎲 {skillName} Check";
            TxtPrompt.Text = $"Ask {characterName} to roll 1d20 for {skillName}:";
            TxtCharacter.Text = characterName;
            TxtModifier.Text = modifier >= 0 ? $"+{modifier}" : modifier.ToString();
            TxtDC.Text = dc.ToString();

            TxtRoll.Focus();
        }

        private void TxtRoll_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(TxtRoll.Text, out int roll) && roll >= 1 && roll <= 20)
            {
                int total = roll + _modifier;
                bool success = total >= _dc;

                TxtResult.Text = $"{roll} + {_modifier} = {total} vs DC {_dc} → {(success ? "✅ SUCCESS" : "❌ FAIL")}";
                TxtResult.Foreground = success
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            else
            {
                TxtResult.Text = "";
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtRoll.Text, out int roll) && roll >= 1 && roll <= 20)
            {
                Roll = roll;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid d20 roll (1-20).", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }
    }
}