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
    public partial class SpawnEditorDialog : Window
    {
        public SpawnMetadata Spawn { get; private set; }

        public SpawnEditorDialog(SpawnMetadata existing = null)
        {
            InitializeComponent();
            Spawn = existing ?? new SpawnMetadata();

            if (existing != null)
            {
                LoadData();
                ChkSpawnOnLoad.IsChecked = existing.SpawnOnMapLoad;
            }
        }

        private void LoadData()
        {
            TxtName.Text = Spawn.Name;
            TxtCreatureName.Text = Spawn.CreatureName;
            TxtSpawnCount.Text = Spawn.SpawnCount.ToString();
            TxtSpawnRadius.Text = Spawn.SpawnRadius.ToString();
            CmbTrigger.SelectedIndex = (int)Spawn.TriggerCondition;
            TxtSpawnRound.Text = Spawn.SpawnOnRound.ToString();
            TxtTriggerDistance.Text = Spawn.TriggerDistance.ToString();
            TxtSpawnDelay.Text = Spawn.SpawnDelay.ToString();
            ChkIsReusable.IsChecked = Spawn.IsReusable;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Please enter a name for the spawn point.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtCreatureName.Text))
            {
                MessageBox.Show("Please enter a creature name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Spawn.Name = TxtName.Text;
            Spawn.CreatureName = TxtCreatureName.Text;
            Spawn.SpawnCount = int.TryParse(TxtSpawnCount.Text, out int count) ? count : 1;
            Spawn.SpawnRadius = int.TryParse(TxtSpawnRadius.Text, out int radius) ? radius : 0;
            Spawn.TriggerCondition = (SpawnTrigger)CmbTrigger.SelectedIndex;
            Spawn.SpawnOnRound = int.TryParse(TxtSpawnRound.Text, out int round) ? round : 1;
            Spawn.TriggerDistance = int.TryParse(TxtTriggerDistance.Text, out int distance) ? distance : 5;
            Spawn.SpawnDelay = int.TryParse(TxtSpawnDelay.Text, out int delay) ? delay : 0;
            Spawn.IsReusable = ChkIsReusable.IsChecked ?? false;
            Spawn.SpawnOnMapLoad = ChkSpawnOnLoad.IsChecked == true;

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
