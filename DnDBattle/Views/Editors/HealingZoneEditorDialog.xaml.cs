using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DnDBattle.Views.Editors
{
    public partial class HealingZoneEditorDialog : Window
    {
        public HealingZoneMetadata HealingZone { get; private set; }

        public HealingZoneEditorDialog(HealingZoneMetadata existing = null)
        {
            InitializeComponent();
            HealingZone = existing ?? new HealingZoneMetadata();

            if (existing != null)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            TxtName.Text = HealingZone.Name;
            TxtDescription.Text = HealingZone.Description;
            TxtHealingDice.Text = HealingZone.HealingDice;
            CmbHealingTrigger.SelectedIndex = (int)HealingZone.HealingTrigger;
            ChkOncePerCreature.IsChecked = HealingZone.OncePerCreature;
            ChkHasCharges.IsChecked = HealingZone.HasCharges;
            TxtCharges.Text = HealingZone.ChargesRemaining.ToString();
            ChkRemovesConditions.IsChecked = HealingZone.RemovesConditions;
            TxtConditionsRemoved.Text = HealingZone.ConditionsRemoved;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Please enter a name for the healing zone.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            HealingZone.Name = TxtName.Text;
            HealingZone.Description = TxtDescription.Text;
            HealingZone.HealingDice = TxtHealingDice.Text;
            HealingZone.HealingTrigger = (HealingTrigger)CmbHealingTrigger.SelectedIndex;
            HealingZone.OncePerCreature = ChkOncePerCreature.IsChecked ?? true;
            HealingZone.HasCharges = ChkHasCharges.IsChecked ?? false;
            HealingZone.ChargesRemaining = int.TryParse(TxtCharges.Text, out int charges) ? charges : 3;
            HealingZone.RemovesConditions = ChkRemovesConditions.IsChecked ?? false;
            HealingZone.ConditionsRemoved = TxtConditionsRemoved.Text;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
