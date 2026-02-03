using DnDBattle.Models.Tiles;
using DnDBattle.Views.Editors;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DnDBattle.Views.TileMap
{
    public partial class TilePropertiesPanel : UserControl
    {
        private Tile _currentTile;

        public event Action<Tile> TilePropertiesChanged;

        public TilePropertiesPanel()
        {
            InitializeComponent();
        }

        public void SetTile(Tile tile)
        {
            _currentTile = tile;

            if (tile == null)
            {
                TxtNoSelection.Visibility = Visibility.Visible;
                PropertiesContainer.Visibility = Visibility.Collapsed;
                TxtTileInfo.Text = "No tile selected";
            }
            else
            {
                TxtNoSelection.Visibility = Visibility.Collapsed;
                PropertiesContainer.Visibility = Visibility.Visible;
                TxtTileInfo.Text = $"Tile at ({tile.GridX}, {tile.GridY})";

                RefreshMetadataList();
            }
        }

        private void RefreshMetadataList()
        {
            if (_currentTile == null) return;

            MetadataList.ItemsSource = null;
            MetadataList.ItemsSource = _currentTile.Metadata;
        }

        private void AddTrap_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TrapEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Trap);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
            }
        }

        private void AddSecret_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SecretEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Secret);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
            }
        }

        private void AddInteractive_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InteractiveEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Interactive);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
            }
        }


        private void AddTrigger_Click(object sender, RoutedEventArgs e)
        {
            var trigger = new TriggerMetadata
            {
                Name = "New Trigger",
                EventDescription = "Something happens..."
            };

            _currentTile.Metadata.Add(trigger);
            RefreshMetadataList();
            TilePropertiesChanged?.Invoke(_currentTile);
        }

        private void AddSpawn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SpawnEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Spawn);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
            }
        }

        private void AddHazard_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HazardEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Hazard);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
            }
        }

        private void AddTeleporter_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TeleporterEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Teleporter);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
            }
        }

        private void AddHealing_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HealingZoneEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.HealingZone);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
            }
        }

        private void EditMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileMetadata metadata)
            {
                Window dialog = null;

                if (metadata is TrapMetadata trap)
                {
                    dialog = new TrapEditorDialog(trap);
                }
                else if (metadata is SecretMetadata secret)
                {
                    dialog = new SecretEditorDialog(secret);
                }
                else if (metadata is InteractiveMetadata interactive)
                {
                    dialog = new InteractiveEditorDialog(interactive);
                }
                else if (metadata is SpawnMetadata spawn)
                {
                    dialog = new SpawnEditorDialog(spawn);
                }
                else if (metadata is HazardMetadata hazard)
                {
                    dialog = new HazardEditorDialog(hazard);
                }
                else if (metadata is TeleporterMetadata teleporter)
                {
                    dialog = new TeleporterEditorDialog(teleporter);
                }
                else if (metadata is HealingZoneMetadata healingZone)
                {
                    dialog = new HealingZoneEditorDialog(healingZone);
                }

                if (dialog != null && dialog.ShowDialog() == true)
                {
                    RefreshMetadataList();
                    TilePropertiesChanged?.Invoke(_currentTile);
                }
            }
        }

        private void DeleteMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileMetadata metadata)
            {
                var result = MessageBox.Show(
                    $"Delete '{metadata.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _currentTile.Metadata.Remove(metadata);
                    RefreshMetadataList();
                    TilePropertiesChanged?.Invoke(_currentTile);
                }
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            TilePropertiesChanged?.Invoke(_currentTile);
            MessageBox.Show("Tile properties saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}