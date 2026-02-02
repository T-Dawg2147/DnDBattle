using DnDBattle.Models.Tiles;
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
            // TODO: Create SecretMetadata editor
            MessageBox.Show("Secret editor coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddInteractive_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Create InteractiveMetadata editor
            MessageBox.Show("Interactive editor coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
            // TODO: Create SpawnMetadata
            MessageBox.Show("Spawn point editor coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TileMetadata metadata)
            {
                if (metadata is TrapMetadata trap)
                {
                    var dialog = new TrapEditorDialog(trap);
                    if (dialog.ShowDialog() == true)
                    {
                        RefreshMetadataList();
                        TilePropertiesChanged?.Invoke(_currentTile);
                    }
                }
                else
                {
                    MessageBox.Show($"Editor for {metadata.Type} not yet implemented.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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