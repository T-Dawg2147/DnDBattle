using DnDBattle.Models;
using DnDBattle.Utils;
using DnDBattle.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for CombatTrackerPanel.xaml
    /// </summary>
    public partial class CombatTrackerPanel : UserControl
    {
        private Token _currentToken;
        private MainViewModel _vm;

        public CombatTrackerPanel()
        {
            InitializeComponent();
            
        }

        private void CombatTrackerPanel_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as MainViewModel;

            if (_vm != null)
            {
                _vm.PropertyChanged += Vm_PropertyChanged;
                UpdateDisplay();
            }
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedToken))
                SetToken(_vm?.SelectedToken);
        }

        public void SetToken(Token token)
        {
            if (_currentToken != null)
                _currentToken.PropertyChanged -= Token_PropertyChanged;

            _currentToken = token;

            if (_currentToken != null)
                _currentToken.PropertyChanged += Token_PropertyChanged;
            
            UpdateDisplay();
        }

        private void Token_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
            Dispatcher.Invoke(() => UpdateDisplay());

        private void UpdateDisplay()
        {
            if (_currentToken == null)
            {
                TxtCreatureName.Text = "No Creature Selected";
                TxtCreatureInfo.Text = "Select a token to manage combat";
                TxtHPDisplay.Text = "0 / 0";
                HPBar.Width = 0;
                TempHPBar.Width = 0;
                DeathSavesGroup.Visibility = Visibility.Collapsed;
                return;
            }

            TxtCreatureName.Text = _currentToken.Name;
            TxtCreatureInfo.Text = $"AC {_currentToken.ArmorClass} | {_currentToken.Type} | CR {_currentToken.ChallengeRating}";

            int displayHP = _currentToken.HP;
            int displayMax = _currentToken.MaxHP;
            string hpText = $"{displayHP} / {displayHP}";

            if (_currentToken.TempHP > 0)
                hpText += $" (+{_currentToken.TempHP} temp)";

            TxtHPDisplay.Text = hpText;

            double hpPercent = displayMax > 0 ? (double)Math.Max(0, displayHP) / displayMax : 0;
            double barWidth = MainGrid.ActualWidth > 30 ? MainGrid.ActualWidth - 30 : 200;
            HPBar.Width = barWidth * hpPercent;

            if (hpPercent > 0.5)
                HPBar.Background = (Brush)FindResource("HPFullBrush");
            else if (hpPercent > 0.25)
                HPBar.Background = (Brush)FindResource("HPMidBrush");
            else
                HPBar.Background = (Brush)FindResource("HPLowBrush");

            if (_currentToken.TempHP > 0 && displayMax > 0)
            {
                double tempPercent = (double)(_currentToken.HP + _currentToken.TempHP) / displayMax;
                TempHPBar.Width = Math.Min(barWidth, barWidth * tempPercent);
            }
            else
                TempHPBar.Width = 0;

            if (_currentToken.IsPlayer && _currentToken.HP <= 0)
            {
                DeathSavesGroup.Visibility = Visibility.Visible;
                UpdateDeathSaveCheckboxes();
            }
            else
                DeathSavesGroup.Visibility = Visibility.Collapsed;

            UpdateConditionButtons();

            ChkConcentrating.IsChecked = _currentToken.HasCondition(Models.Condition.Concentrating);
            TxtConcentrationSpell.Text = _currentToken.ConcentrationSpell ?? "";
            TxtConcentrationSpell.IsEnabled = ChkConcentrating.IsChecked == true;
        }

        #region HP Management

        private void BtnDamage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null || !(sender is Button btn)) return;

            if (int.TryParse(btn.Tag?.ToString(), out int damage))
                ApplyDamage(damage);
        }

        private void BtnCustomDamage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;

            if (int.TryParse(TxtCustomDamage.Text, out int damage) && damage > 0)
            {
                ApplyDamage(damage);
                TxtCustomDamage.Text = "0";
            }
        }

        private void ApplyDamage(int damage)
        {
            if (_currentToken == null || damage <= 0) return;

            if (_currentToken.TempHP > 0)
            {
                if (_currentToken.TempHP >= damage)
                {
                    _currentToken.TempHP -= damage;
                    damage = 0;
                }
                else
                {
                    damage -= _currentToken.TempHP;
                    _currentToken.TempHP = 0;
                }
            }

            if (damage > 0)
                _currentToken.HP = Math.Max(-_currentToken.MaxHP, _currentToken.HP - damage);

            if (_currentToken.HasCondition(Models.Condition.Concentrating) && damage > 0)
            {
                int dc = Math.Max(10, damage / 2);
                MessageBox.Show($"{_currentToken.Name} took {damage} while concentrating! \n\n" +
                    $"Concentration save DC: {dc}\n" +
                    $"(Constitution saving throw required)",
                    "Concentration Check Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            UpdateDisplay();
        }

        private void BtnHeal_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null || !(sender is Button btn)) return;

            if (int.TryParse(btn.Tag?.ToString(), out int healing))
            {
                ApplyHealing(healing);
            }
        }

        private void BtnCustomHeal_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;

            if (int.TryParse(TxtCustomHeal.Text, out int healing) && healing > 0)
            {
                ApplyHealing(healing);
                TxtCustomHeal.Text = "0";
            }
        }

        private void ApplyHealing(int healing)
        {
            if (_currentToken == null || healing <= 0) return;

            // Reset death saves when healed from 0
            if (_currentToken.HP <= 0 && _currentToken.IsPlayer)
            {
                _currentToken.ResetDeathSaves();
            }

            _currentToken.HP = Math.Min(_currentToken.MaxHP, _currentToken.HP + healing);
            UpdateDisplay();
        }

        private void BtnSetTempHP_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;

            if (int.TryParse(TxtTempHP.Text, out int tempHP))
            {
                // Temp HP doesn't stack - only keep higher value
                _currentToken.TempHP = Math.Max(_currentToken.TempHP, tempHP);
                UpdateDisplay();
            }
        }

        private void BtnClearTempHP_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;
            _currentToken.TempHP = 0;
            TxtTempHP.Text = "0";
            UpdateDisplay();
        }

        #endregion

        #region Death Saves

        private void UpdateDeathSaveCheckboxes()
        {
            if (_currentToken == null) return;

            ChkSuccess1.IsChecked = _currentToken.DeathSaveSuccesses >= 1;
            ChkSuccess2.IsChecked = _currentToken.DeathSaveSuccesses >= 2;
            ChkSuccess3.IsChecked = _currentToken.DeathSaveSuccesses >= 3;

            ChkFailure1.IsChecked = _currentToken.DeathSaveFailures >= 1;
            ChkFailure2.IsChecked = _currentToken.DeathSaveFailures >= 2;
            ChkFailure3.IsChecked = _currentToken.DeathSaveFailures >= 3;
        }

        private void DeathSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;

            int successes = 0;
            if (ChkSuccess1.IsChecked == true) successes++;
            if (ChkSuccess2.IsChecked == true) successes++;
            if (ChkSuccess3.IsChecked == true) successes++;

            int failures = 0;
            if (ChkFailure1.IsChecked == true) failures++;
            if (ChkFailure2.IsChecked == true) failures++;
            if (ChkFailure3.IsChecked == true) failures++;

            _currentToken.DeathSaveSuccesses = successes;
            _currentToken.DeathSaveFailures = failures;

            if (successes >= 3)
            {
                MessageBox.Show($"{_currentToken.Name} has stabilized!", "Stable",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (failures >= 3)
            {
                MessageBox.Show($"{_currentToken.Name} has died.", "Death",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion


        #region Conditions

        private void PopulateConditionButton()
        {
            var conditions = new[]
            {
                Models.Condition.Blinded, Models.Condition.Charmed, Models.Condition.Deafened,
                Models.Condition.Frightened, Models.Condition.Grappled, Models.Condition.Incapacitated,
                Models.Condition.Invisible, Models.Condition.Paralyzed, Models.Condition.Petrified,
                Models.Condition.Poisoned, Models.Condition.Prone, Models.Condition.Restrained,
                Models.Condition.Stunned, Models.Condition.Unconscious, Models.Condition.Dodging, Models.Condition.Hidden
            };

            foreach (var condition in conditions)
            {
                var btn = new ToggleButton()
                {
                    Content = condition.ToString(),
                    Tag = condition,
                    Margin = new Thickness(0, 0, 4, 4),
                    Padding = new Thickness(6, 3, 6, 3),
                    FontSize = 11
                };

                btn.Checked += ConditionButton_Toggled;
                btn.Unchecked += ConditionButton_Toggled;

                ConditionsPanel.Children.Add(btn);
            }
        }

        private void UpdateConditionButtons()
        {
            if (_currentToken == null) return;

            foreach (var child in ConditionsPanel.Children)
            {
                if (child is ToggleButton btn && btn.Tag is Models.Condition condition)
                    btn.IsChecked = _currentToken.HasCondition(condition);
            }
        }

        private void ConditionButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null || !(sender is ToggleButton btn)) return;
            if (!(btn.Tag is Models.Condition condition)) return;

            if (btn.IsChecked == true)
                _currentToken.AddCondition(condition);
            else
                _currentToken.RemoveCondition(condition);
        }

        #endregion

        #region Concentration
        private void ChkConcentrating_Changed(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;

            if (ChkConcentrating.IsChecked == true)
            {
                _currentToken.AddCondition(Models.Condition.Concentrating);
                TxtConcentrationSpell.IsEnabled = true;
            }
            else
            {
                _currentToken.RemoveCondition(Models.Condition.Concentrating);
                _currentToken.ConcentrationSpell = null;
                TxtConcentrationSpell.IsEnabled = false;
                TxtConcentrationSpell.Text = "";
            }
        }

        private void TxtConcentrationSpell_Changed(object sender, RoutedEventArgs e)
        {
            if (_currentToken != null)
                _currentToken.ConcentrationSpell = TxtConcentrationSpell.Text;
        }

        private void BtnConcentrationCheck_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;

            int conMod = (_currentToken.Con - 10) / 2;
            var roll = DiceRoller.RollExpression("1d20");
            int total = roll.Total + conMod;

            string result = $"Concentration Check for {_currentToken.Name}:\n\n" +
                $"d20 roll: {roll.Total}\n" +
                $"Total: {total}\n\n" +
                $"(Compare again DC 10 or half damage taken, whichever is higher)";

            MessageBox.Show(result, "Concentration Check", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Quick Actions

        private void BtnRollInitiative_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;

            var roll = DiceRoller.RollExpression("1d20");
            int total = roll.Total + _currentToken.InitiativeModifier;
            _currentToken.Initiative = total;

            MessageBox.Show($"{_currentToken.Name} rolled initiative:\n\n" +
                $"d20: {roll.Total} + {_currentToken.InitiativeModifier} = {total}",
                "Initiative", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnResetCombat_Click(object sender, RoutedEventArgs e)
        {
            if (_currentToken == null) return;

            var result = MessageBox.Show(
                $"Reset {_currentToken.Name} for new combat?\n\n" +
                $"This will:\n" +
                $"Restore HP to maximum\n" +
                $"Clear all conditions" +
                $"Reset death saves" +
                $"Clear temporary HP",
                "Reset Combat", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _currentToken.HP = _currentToken.MaxHP;
                _currentToken.TempHP = 0;
                _currentToken.Conditions = Models.Condition.None;
                _currentToken.ResetDeathSaves();
                _currentToken.ConcentrationSpell = null;
                UpdateDisplay();
            }
        }

        #endregion

    }
}
