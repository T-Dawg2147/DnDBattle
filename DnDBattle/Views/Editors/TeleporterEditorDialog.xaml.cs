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
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Views.Editors;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Views.Editors
{
    public partial class TeleporterEditorDialog : Window
    {
        public TeleporterMetadata Teleporter { get; private set; }

        public TeleporterEditorDialog(TeleporterMetadata existing = null)
        {
            InitializeComponent();
            Teleporter = existing ?? new TeleporterMetadata();

            if (existing != null)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            TxtName.Text = Teleporter.Name;
            TxtDescription.Text = Teleporter.TeleportDescription;
            TxtDestX.Text = Teleporter.DestinationX.ToString();
            TxtDestY.Text = Teleporter.DestinationY.ToString();
            ChkIsVisible.IsChecked = Teleporter.IsVisible;
            ChkIsActive.IsChecked = Teleporter.IsActive;
            ChkRequiresConsent.IsChecked = Teleporter.RequiresConsent;
            ChkIsOneWay.IsChecked = Teleporter.IsOneWay;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Please enter a name for the teleporter.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtDestX.Text, out int destX) || !int.TryParse(TxtDestY.Text, out int destY))
            {
                MessageBox.Show("Please enter valid destination coordinates.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Teleporter.Name = TxtName.Text;
            Teleporter.TeleportDescription = TxtDescription.Text;
            Teleporter.DestinationX = destX;
            Teleporter.DestinationY = destY;
            Teleporter.IsVisible = ChkIsVisible.IsChecked ?? true;
            Teleporter.IsActive = ChkIsActive.IsChecked ?? true;
            Teleporter.RequiresConsent = ChkRequiresConsent.IsChecked ?? true;
            Teleporter.IsOneWay = ChkIsOneWay.IsChecked ?? false;

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
