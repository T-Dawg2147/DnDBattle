using DnDBattle.Models.Tiles;
using DnDBattle.Views.Editors;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

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

        /// <summary>
        /// Loads and displays all properties and metadata for a tile
        /// </summary>
        public void LoadTileProperties(Tile tile)
        {
            if (tile == null)
            {
                _currentTile = null;
                ClearProperties();
                return;
            }

            _currentTile = tile;

            // Load basic properties
            if (TxtGridX != null) TxtGridX.Text = tile.GridX.ToString();
            if (TxtGridY != null) TxtGridY.Text = tile.GridY.ToString();
            if (TxtRotation != null) TxtRotation.Text = tile.Rotation.ToString();
            if (ChkFlipH != null) ChkFlipH.IsChecked = tile.FlipHorizontal;
            if (ChkFlipV != null) ChkFlipV.IsChecked = tile.FlipVertical;
            if (TxtNotes != null) TxtNotes.Text = tile.Notes ?? "";

            // Load metadata list
            RefreshMetadataList();

            System.Diagnostics.Debug.WriteLine($"[TileProperties] Loaded properties for tile at ({tile.GridX},{tile.GridY}) with {tile.Metadata.Count} metadata items");
        }

        private void ClearProperties()
        {
            if (TxtGridX != null) TxtGridX.Text = "";
            if (TxtGridY != null) TxtGridY.Text = "";
            if (TxtRotation != null) TxtRotation.Text = "";
            if (ChkFlipH != null) ChkFlipH.IsChecked = false;
            if (ChkFlipV != null) ChkFlipV.IsChecked = false;
            if (TxtNotes != null) TxtNotes.Text = "";
            if (MetadataList != null) MetadataList.Items.Clear();
        }

        private void RefreshMetadataList()
        {
            if (_currentTile == null || MetadataList == null) return;

            MetadataList.Items.Clear();

            foreach (var metadata in _currentTile.Metadata)
            {
                var item = new ListBoxItem
                {
                    Content = $"{metadata.Type.GetIcon()} {metadata.Name ?? metadata.Type.GetDisplayName()}",
                    Tag = metadata,
                    ToolTip = CreateMetadataTooltip(metadata)
                };

                MetadataList.Items.Add(item);
            }

            System.Diagnostics.Debug.WriteLine($"[TileProperties] Refreshed metadata list: {_currentTile.Metadata.Count} items");
        }

        private string CreateMetadataTooltip(TileMetadata metadata)
        {
            string tooltip = $"{metadata.Type.GetDisplayName()}\n";
            tooltip += $"Name: {metadata.Name ?? "Unnamed"}\n";

            if (!string.IsNullOrEmpty(metadata.DMNotes))
                tooltip += $"Notes: {metadata.DMNotes}\n";

            switch (metadata)
            {
                case TrapMetadata trap:
                    tooltip += $"Detection DC: {trap.DetectionDC}\n";
                    tooltip += $"Damage: {trap.DamageDice} {trap.DamageType}\n";
                    tooltip += trap.IsDetected ? "✅ Detected" : "❌ Not Detected";
                    break;

                case SecretMetadata secret:
                    tooltip += $"Investigation DC: {secret.InvestigationDC}\n";
                    tooltip += secret.IsDiscovered ? "✅ Discovered" : "❌ Hidden";
                    break;

                case InteractiveMetadata interactive:
                    tooltip += $"Type: {interactive.ObjectType}\n";
                    tooltip += $"State: {interactive.State}";
                    break;

                case HazardMetadata hazard:
                    tooltip += $"Type: {hazard.HazardKind}\n";
                    tooltip += $"Damage: {hazard.DamageDice} {hazard.DamageType}";
                    break;

                case TeleporterMetadata teleporter:
                    tooltip += $"Destination: ({teleporter.DestinationX}, {teleporter.DestinationY})";
                    break;

                case HealingZoneMetadata healing:
                    tooltip += $"Healing: {healing.HealingDice}\n";
                    tooltip += healing.HasCharges ? $"Charges: {healing.ChargesRemaining}" : "Unlimited";
                    break;

                case SpawnMetadata spawn:
                    tooltip += $"Creature: {spawn.CreatureName}\n";
                    tooltip += $"Count: {spawn.SpawnCount}\n";
                    tooltip += spawn.HasSpawned ? "✅ Spawned" : "❌ Not Spawned";
                    break;
            }

            return tooltip;
        }

        // ===== METADATA ADDITION HANDLERS =====

        private void AddTrap_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTile == null) return;

            var dialog = new TrapEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Trap);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
                LoadTileProperties(_currentTile);
            }
        }

        private void AddSecret_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTile == null) return;

            var dialog = new SecretEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Secret);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
                LoadTileProperties(_currentTile);
            }
        }

        private void AddInteractive_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTile == null) return;

            var dialog = new InteractiveEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Interactive);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
                LoadTileProperties(_currentTile);
            }
        }

        private void AddTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTile == null) return;

            var trigger = new TriggerMetadata
            {
                Name = "New Trigger",
                EventDescription = "Trigger event"
            };

            _currentTile.Metadata.Add(trigger);
            RefreshMetadataList();
            TilePropertiesChanged?.Invoke(_currentTile);
            LoadTileProperties(_currentTile);
        }

        private void AddHazard_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTile == null) return;

            var dialog = new HazardEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Hazard);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
                LoadTileProperties(_currentTile);
            }
        }

        private void AddTeleporter_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTile == null) return;

            var dialog = new TeleporterEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Teleporter);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
                LoadTileProperties(_currentTile);
            }
        }

        private void AddHealing_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTile == null) return;

            var dialog = new HealingZoneEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.HealingZone);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
                LoadTileProperties(_currentTile);
            }
        }

        private void AddSpawn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTile == null) return;

            var dialog = new SpawnEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                _currentTile.Metadata.Add(dialog.Spawn);
                RefreshMetadataList();
                TilePropertiesChanged?.Invoke(_currentTile);
                LoadTileProperties(_currentTile);
            }
        }

        // ===== METADATA MANAGEMENT =====

        private void EditMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (MetadataList.SelectedItem is ListBoxItem item && item.Tag is TileMetadata metadata)
            {
                // Open appropriate editor based on metadata type
                bool edited = false;

                switch (metadata)
                {
                    case TrapMetadata trap:
                        var trapDialog = new TrapEditorDialog(trap);
                        edited = trapDialog.ShowDialog() == true;
                        break;

                    case SecretMetadata secret:
                        var secretDialog = new SecretEditorDialog(secret);
                        edited = secretDialog.ShowDialog() == true;
                        break;

                    case InteractiveMetadata interactive:
                        var interactiveDialog = new InteractiveEditorDialog(interactive);
                        edited = interactiveDialog.ShowDialog() == true;
                        break;

                    case HazardMetadata hazard:
                        var hazardDialog = new HazardEditorDialog(hazard);
                        edited = hazardDialog.ShowDialog() == true;
                        break;

                    case TeleporterMetadata teleporter:
                        var teleporterDialog = new TeleporterEditorDialog(teleporter);
                        edited = teleporterDialog.ShowDialog() == true;
                        break;

                    case HealingZoneMetadata healing:
                        var healingDialog = new HealingZoneEditorDialog(healing);
                        edited = healingDialog.ShowDialog() == true;
                        break;

                    case SpawnMetadata spawn:
                        var spawnDialog = new SpawnEditorDialog(spawn);
                        edited = spawnDialog.ShowDialog() == true;
                        break;
                }

                if (edited)
                {
                    RefreshMetadataList();
                    TilePropertiesChanged?.Invoke(_currentTile);
                }
            }
        }

        private void RemoveMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (MetadataList.SelectedItem is ListBoxItem item && item.Tag is TileMetadata metadata)
            {
                var result = MessageBox.Show(
                    $"Remove {metadata.Type.GetDisplayName()}: {metadata.Name}?",
                    "Remove Metadata",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _currentTile.Metadata.Remove(metadata);
                    RefreshMetadataList();
                    TilePropertiesChanged?.Invoke(_currentTile);
                }
            }
        }

        public void SetTile(Tile tile)
        {
            LoadTileProperties(tile);
        }
    }
}