using DnDBattle.Models;
using DnDBattle.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Views
{
    public partial class ActionResolutionDialog : Window
    {
        private Token _source;
        private Token _target;
        private Models.Action _action;
        private bool _manualMode;
        private DamageType _selectedDamageType;
        private bool _damageTypeAutoDetected;

        public ActionResult Result { get; private set; }

        public ActionResolutionDialog(Token source, Token target, Models.Action action, DamageType inferredDamageType, bool manualRollMode)
        {
            InitializeComponent();

            _source = source;
            _target = target;
            _action = action;
            _manualMode = manualRollMode;
            _selectedDamageType = inferredDamageType;
            _damageTypeAutoDetected = inferredDamageType != DamageType.None && inferredDamageType != DamageType.Slashing;

            SetupUI();
        }

        /// <summary>
        /// Handle Enter and Escape keys
        /// </summary>
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
                e.Handled = true;
            }
        }

        private void SetupUI()
        {
            // Header
            TxtActionName.Text = _action.Name ?? "Attack";
            TxtSourceTarget.Text = $"{_source.Name} → {_target.Name}";

            // Attack section
            if (_action.AttackBonus != null && _action.AttackBonus != 0)
            {
                AttackSection.Visibility = Visibility.Visible;
                TxtTargetAC.Text = _target.ArmorClass.ToString();

                if (_manualMode)
                {
                    ManualAttackPanel.Visibility = Visibility.Visible;
                    AutoAttackPanel.Visibility = Visibility.Collapsed;
                    TxtAttackRoll.Focus();
                }
                else
                {
                    ManualAttackPanel.Visibility = Visibility.Collapsed;
                    AutoAttackPanel.Visibility = Visibility.Visible;

                    // Roll automatically
                    var roll = Utils.DiceRoller.RollExpression("1d20");
                    int total = roll.Total + (_action.AttackBonus ?? 0);

                    TxtAutoAttackRoll.Text = roll.Total.ToString();
                    TxtAttackBonus.Text = $"+ {_action.AttackBonus}";
                    TxtAttackTotal.Text = total.ToString();

                    // Color for crit
                    if (roll.Total == 20)
                    {
                        TxtAutoAttackRoll.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                        TxtAutoAttackRoll.Text = "20 ⚡";
                    }
                    else if (roll.Total == 1)
                    {
                        TxtAutoAttackRoll.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                        TxtAutoAttackRoll.Text = "1 💀";
                    }
                }
            }
            else
            {
                AttackSection.Visibility = Visibility.Collapsed;

                // Focus damage roll if no attack roll needed
                if (_manualMode && !string.IsNullOrEmpty(_action.DamageExpression))
                {
                    TxtDamageRoll.Focus();
                }
            }

            // Damage section
            if (!string.IsNullOrEmpty(_action.DamageExpression))
            {
                DamageSection.Visibility = Visibility.Visible;
                TxtDamageExpression.Text = $"({_action.DamageExpression})";

                if (_manualMode)
                {
                    ManualDamagePanel.Visibility = Visibility.Visible;
                    AutoDamagePanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ManualDamagePanel.Visibility = Visibility.Collapsed;
                    AutoDamagePanel.Visibility = Visibility.Visible;

                    var roll = Utils.DiceRoller.RollExpression(_action.DamageExpression);
                    TxtAutoDamageRoll.Text = roll.Total.ToString();
                }
            }
            else
            {
                DamageSection.Visibility = Visibility.Collapsed;
            }

            // Damage type dropdown
            PopulateDamageTypes();

            // Show auto-detected indicator if applicable
            if (_damageTypeAutoDetected)
            {
                TxtAutoDetected.Visibility = Visibility.Visible;
            }

            // Target defenses
            PopulateDefenses();
        }

        private void PopulateDamageTypes()
        {
            var types = DamageTypeExtensions.GetAllDamageTypes();

            foreach (var type in types)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{type.GetIcon()} {type.GetDisplayName()}",
                    Tag = type,
                    Foreground = Brushes.White
                };

                if (type == _selectedDamageType)
                {
                    item.IsSelected = true;
                }

                CmbDamageType.Items.Add(item);
            }

            // If no match found, select slashing as default
            if (CmbDamageType.SelectedItem == null && CmbDamageType.Items.Count > 0)
            {
                CmbDamageType.SelectedIndex = 2; // Slashing
            }

            CmbDamageType.SelectionChanged += (s, e) =>
            {
                if (CmbDamageType.SelectedItem is ComboBoxItem selected && selected.Tag is DamageType dt)
                {
                    _selectedDamageType = dt;
                    PopulateDefenses(); // Refresh defenses display
                }
            };
        }

        private void PopulateDefenses()
        {
            DefensesBadges.Children.Clear();

            bool hasDefense = false;

            // Check immunity
            if ((_target.DamageImmunities & _selectedDamageType) != 0)
            {
                hasDefense = true;
                DefensesBadges.Children.Add(CreateDefenseBadge("🛡️ IMMUNE (0 damage)", Color.FromRgb(66, 66, 66)));
            }

            // Check resistance
            if ((_target.DamageResistances & _selectedDamageType) != 0)
            {
                hasDefense = true;
                DefensesBadges.Children.Add(CreateDefenseBadge("🔰 RESISTANT (½ damage)", Color.FromRgb(255, 152, 0)));
            }

            // Check vulnerability
            if ((_target.DamageVulnerabilities & _selectedDamageType) != 0)
            {
                hasDefense = true;
                DefensesBadges.Children.Add(CreateDefenseBadge("💥 VULNERABLE (×2 damage)", Color.FromRgb(244, 67, 54)));
            }

            TxtNoDefenses.Visibility = hasDefense ? Visibility.Collapsed : Visibility.Visible;
        }

        private Border CreateDefenseBadge(string text, Color color)
        {
            return new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 8, 8),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11
                }
            };
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Result = new ActionResult
            {
                Source = _source,
                Target = _target,
                Action = _action,
                DamageType = _selectedDamageType
            };

            // Get attack roll
            if (_action.AttackBonus != null && _action.AttackBonus != 0)
            {
                int attackRoll;
                if (_manualMode)
                {
                    if (!int.TryParse(TxtAttackRoll.Text, out attackRoll))
                    {
                        MessageBox.Show("Please enter a valid attack roll.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtAttackRoll.Focus();
                        TxtAttackRoll.SelectAll();
                        return;
                    }
                }
                else
                {
                    attackRoll = int.Parse(TxtAutoAttackRoll.Text.Replace(" ⚡", "").Replace(" 💀", ""));
                }

                Result.AttackRoll = attackRoll;
                Result.AttackTotal = attackRoll + (_action.AttackBonus ?? 0);
                Result.IsCriticalHit = attackRoll == 20;
                Result.IsCriticalMiss = attackRoll == 1;

                // Check if hit
                if (Result.IsCriticalMiss)
                {
                    Result.Success = false;
                    Result.Message = $"💀 Critical Miss! {_source.Name} misses {_target.Name}";
                    DialogResult = true;
                    Close();
                    return;
                }

                bool hits = Result.IsCriticalHit || Result.AttackTotal >= _target.ArmorClass;
                if (!hits)
                {
                    Result.Success = false;
                    Result.Message = $"🛡️ {_source.Name} attacks {_target.Name}: {Result.AttackTotal} vs AC {_target.ArmorClass} - Miss!";
                    DialogResult = true;
                    Close();
                    return;
                }
            }

            // Get damage roll
            if (!string.IsNullOrEmpty(_action.DamageExpression))
            {
                int damageRoll;
                if (_manualMode)
                {
                    if (!int.TryParse(TxtDamageRoll.Text, out damageRoll))
                    {
                        MessageBox.Show("Please enter a valid damage roll.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtDamageRoll.Focus();
                        TxtDamageRoll.SelectAll();
                        return;
                    }
                }
                else
                {
                    damageRoll = int.Parse(TxtAutoDamageRoll.Text);
                }

                // Double on crit (only in auto mode, manual users double themselves)
                if (Result.IsCriticalHit && !_manualMode)
                {
                    var critRoll = Utils.DiceRoller.RollExpression(_action.DamageExpression);
                    damageRoll += critRoll.Total;
                }

                Result.DamageRoll = damageRoll;

                // Apply damage with type consideration
                var (effectiveDamage, damageDesc) = _target.TakeDamage(damageRoll, _selectedDamageType);
                Result.TargetWasConcentrating = _target.IsConcentrating;
                Result.DamageForConcentration = effectiveDamage;
                Result.EffectiveDamage = effectiveDamage;
                Result.DamageModification = damageDesc;

                // Build message
                string critText = Result.IsCriticalHit ? " ⚡CRITICAL HIT!" : "";
                string icon = _selectedDamageType.GetIcon();
                string typeName = _selectedDamageType.GetDisplayName();

                if (!string.IsNullOrEmpty(damageDesc))
                {
                    Result.Message = $"🎯 {_source.Name} hits {_target.Name} with {_action.Name}!{critText}\n" +
                                    $"{icon} {damageRoll} {typeName} → {effectiveDamage} ({damageDesc})";
                }
                else
                {
                    Result.Message = $"🎯 {_source.Name} hits {_target.Name} with {_action.Name}!{critText}\n" +
                                    $"{icon} {effectiveDamage} {typeName} damage!";
                }

                Result.Success = true;
            }
            else
            {
                Result.Success = true;
                Result.Message = $"✨ {_source.Name} uses {_action.Name} on {_target.Name}";
            }

            DialogResult = true;
            Close();
        }
    }
}