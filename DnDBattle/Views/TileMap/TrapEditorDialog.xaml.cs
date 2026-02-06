using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Views.TileMap
{
    public partial class TrapEditorDialog : Window
    {
        public TrapMetadata Trap { get; private set; }
        private bool _isEditMode;

        public TrapEditorDialog(TrapMetadata existingTrap = null)
        {
            InitializeComponent();

            _isEditMode = existingTrap != null;
            Trap = existingTrap ?? new TrapMetadata();

            if (_isEditMode)
            {
                LoadTrapData();
            }
        }

        private void LoadTrapData()
        {
            TxtName.Text = Trap.Name;
            CmbTriggerType.SelectedIndex = (int)Trap.TriggerType;

            // Roll modes
            ChkAutoRollDetection.IsChecked = Trap.AutoRollDetection;
            ChkAutoRollDisarm.IsChecked = Trap.AutoRollDisarm;
            ChkAutoRollSave.IsChecked = Trap.AutoRollSave;
            ChkAutoRollDamage.IsChecked = Trap.AutoRollDamage;

            // Detection
            TxtDetectionDC.Text = Trap.DetectionDC.ToString();
            TxtDetectionDesc.Text = Trap.DetectionDescription;

            // Disarm
            ChkCanBeDisarmed.IsChecked = Trap.CanBeDisarmed;
            TxtDisarmDC.Text = Trap.DisarmDC.ToString();
            ChkFailTriggers.IsChecked = Trap.FailedDisarmTriggersTrap;

            // Save
            CmbSaveAbility.SelectedIndex = Array.IndexOf(new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" }, Trap.SaveAbility);
            TxtSaveDC.Text = Trap.SaveDC.ToString();

            // Damage
            TxtDamageDice.Text = Trap.DamageDice;
            CmbDamageType.SelectedIndex = GetDamageTypeIndex(Trap.DamageType);
            ChkHalfDamageOnSave.IsChecked = Trap.HalfDamageOnSave;

            // Flavor
            TxtTriggerDesc.Text = Trap.TriggerDescription;
            TxtEffectDesc.Text = Trap.EffectDescription;

            // Reusability
            ChkReusable.IsChecked = Trap.IsReusable;
            TxtMaxTriggers.Text = Trap.MaxTriggers.ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Please enter a trap name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtDetectionDC.Text, out int detectionDC) || detectionDC < 1)
            {
                MessageBox.Show("Detection DC must be a positive number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save data
            Trap.Name = TxtName.Text;
            Trap.TriggerType = (TrapTriggerType)CmbTriggerType.SelectedIndex;

            // Roll modes
            Trap.AutoRollDetection = ChkAutoRollDetection.IsChecked ?? true;
            Trap.AutoRollDisarm = ChkAutoRollDisarm.IsChecked ?? true;
            Trap.AutoRollSave = ChkAutoRollSave.IsChecked ?? true;
            Trap.AutoRollDamage = ChkAutoRollDamage.IsChecked ?? true;

            // Detection
            Trap.DetectionDC = detectionDC;
            Trap.DetectionDescription = TxtDetectionDesc.Text;

            // Disarm
            Trap.CanBeDisarmed = ChkCanBeDisarmed.IsChecked ?? true;
            Trap.DisarmSkill = (CmbDisarmSkill.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Thieves' Tools";
            Trap.DisarmDC = int.TryParse(TxtDisarmDC.Text, out int disarmDC) ? disarmDC : 15;
            Trap.FailedDisarmTriggersTrap = ChkFailTriggers.IsChecked ?? true;

            // Save
            Trap.SaveAbility = (CmbSaveAbility.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "DEX";
            Trap.SaveDC = int.TryParse(TxtSaveDC.Text, out int saveDC) ? saveDC : 13;

            // Damage
            Trap.DamageDice = TxtDamageDice.Text;
            Trap.DamageType = GetDamageTypeFromIndex(CmbDamageType.SelectedIndex);
            Trap.HalfDamageOnSave = ChkHalfDamageOnSave.IsChecked ?? true;

            // Flavor
            Trap.TriggerDescription = TxtTriggerDesc.Text;
            Trap.EffectDescription = TxtEffectDesc.Text;

            // Reusability
            Trap.IsReusable = ChkReusable.IsChecked ?? false;
            Trap.MaxTriggers = int.TryParse(TxtMaxTriggers.Text, out int max) ? max : 1;

            DialogResult = true;
            Close();
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

        private int GetDamageTypeIndex(DamageType type)
        {
            return type switch
            {
                DamageType.Bludgeoning => 0,
                DamageType.Piercing => 1,
                DamageType.Slashing => 2,
                DamageType.Fire => 3,
                DamageType.Cold => 4,
                DamageType.Lightning => 5,
                DamageType.Thunder => 6,
                DamageType.Acid => 7,
                DamageType.Poison => 8,
                DamageType.Necrotic => 9,
                DamageType.Radiant => 10,
                DamageType.Force => 11,
                DamageType.Psychic => 12,
                _ => 1
            };
        }

        private DamageType GetDamageTypeFromIndex(int index)
        {
            return index switch
            {
                0 => DamageType.Bludgeoning,
                1 => DamageType.Piercing,
                2 => DamageType.Slashing,
                3 => DamageType.Fire,
                4 => DamageType.Cold,
                5 => DamageType.Lightning,
                6 => DamageType.Thunder,
                7 => DamageType.Acid,
                8 => DamageType.Poison,
                9 => DamageType.Necrotic,
                10 => DamageType.Radiant,
                11 => DamageType.Force,
                12 => DamageType.Psychic,
                _ => DamageType.Piercing
            };
        }
    }
}