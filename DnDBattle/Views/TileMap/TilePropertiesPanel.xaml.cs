using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using DnDBattle.Views.Editors;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DnDBattle.Views.TileMap
{
    /// <summary>
    /// Panel for editing tile instance and tile definition properties
    /// </summary>
    public partial class TilePropertiesPanel : UserControl
    {
        #region Fields

        private Tile _currentTile;
        private TileDefinition _currentTileDefinition;
        private bool _isLoadingProperties = false;

        #endregion

        #region Events

        public event Action<Tile> TilePropertiesChanged;
        public event Action<TileDefinition> TileDefinitionChanged;

        #endregion

        #region Constructor

        public TilePropertiesPanel()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads and displays all properties and metadata for a tile
        /// </summary>
        public void LoadTileProperties(Tile tile)
        {
            _isLoadingProperties = true;

            try
            {
                if (tile == null)
                {
                    _currentTile = null;
                    _currentTileDefinition = null;
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

                // Load tile definition
                _currentTileDefinition = TileLibraryService.Instance.GetTileById(tile.TileDefinitionId);
                LoadTileDefinitionProperties();

                // Load metadata list
                RefreshMetadataList();

                System.Diagnostics.Debug.WriteLine($"[TileProperties] Loaded properties for tile at ({tile.GridX},{tile.GridY}) with {tile.Metadata.Count} metadata items");
            }
            finally
            {
                _isLoadingProperties = false;
            }
        }

        /// <summary>
        /// Sets the tile to edit (legacy method for compatibility)
        /// </summary>
        public void SetTile(Tile tile)
        {
            LoadTileProperties(tile);
        }

        #endregion

        #region Private Methods - Property Loading

        private void ClearProperties()
        {
            _isLoadingProperties = true;

            try
            {
                if (TxtGridX != null) TxtGridX.Text = "";
                if (TxtGridY != null) TxtGridY.Text = "";
                if (TxtRotation != null) TxtRotation.Text = "";
                if (ChkFlipH != null) ChkFlipH.IsChecked = false;
                if (ChkFlipV != null) ChkFlipV.IsChecked = false;
                if (TxtNotes != null) TxtNotes.Text = "";
                if (MetadataList != null) MetadataList.Items.Clear();

                // Clear tile definition properties
                if (TxtTileDefName != null) TxtTileDefName.Text = "No tile selected";
                if (ChkBlocksMovement != null) ChkBlocksMovement.IsChecked = false;
                if (ChkBlocksSight != null) ChkBlocksSight.IsChecked = false;
                if (ChkBlocksLight != null) ChkBlocksLight.IsChecked = false;
                if (ChkDifficultTerrain != null) ChkDifficultTerrain.IsChecked = false;
                if (ChkIsDoor != null) ChkIsDoor.IsChecked = false;
                if (ChkProvidesCover != null) ChkProvidesCover.IsChecked = false;
                if (ChkIsInteractive != null) ChkIsInteractive.IsChecked = false;

                // Hide expandable sections
                if (DoorPropertiesPanel != null) DoorPropertiesPanel.Visibility = Visibility.Collapsed;
                if (CoverPropertiesPanel != null) CoverPropertiesPanel.Visibility = Visibility.Collapsed;
                if (InteractivePropertiesPanel != null) InteractivePropertiesPanel.Visibility = Visibility.Collapsed;
            }
            finally
            {
                _isLoadingProperties = false;
            }
        }

        private void LoadTileDefinitionProperties()
        {
            if (_currentTileDefinition == null)
            {
                if (TxtTileDefName != null) TxtTileDefName.Text = "Unknown tile definition";
                return;
            }

            // Display name
            if (TxtTileDefName != null)
                TxtTileDefName.Text = $"{_currentTileDefinition.Layer.GetIcon()} {_currentTileDefinition.DisplayName ?? _currentTileDefinition.Id}";

            // Collision & Vision
            if (ChkBlocksMovement != null) ChkBlocksMovement.IsChecked = _currentTileDefinition.BlocksMovement;
            if (ChkBlocksSight != null) ChkBlocksSight.IsChecked = _currentTileDefinition.BlocksSight;
            if (ChkBlocksLight != null) ChkBlocksLight.IsChecked = _currentTileDefinition.BlocksLight;

            // Terrain
            if (ChkDifficultTerrain != null) ChkDifficultTerrain.IsChecked = _currentTileDefinition.IsDifficultTerrain;

            // Door
            if (ChkIsDoor != null) ChkIsDoor.IsChecked = _currentTileDefinition.IsDoor;
            if (DoorPropertiesPanel != null) DoorPropertiesPanel.Visibility = _currentTileDefinition.IsDoor ? Visibility.Visible : Visibility.Collapsed;
            if (ChkIsLocked != null) ChkIsLocked.IsChecked = _currentTileDefinition.IsLockedByDefault;
            if (TxtLockDC != null) TxtLockDC.Text = _currentTileDefinition.LockDC.ToString();
            if (TxtBreakDC != null) TxtBreakDC.Text = _currentTileDefinition.BreakDC.ToString();

            // Cover
            if (ChkProvidesCover != null) ChkProvidesCover.IsChecked = _currentTileDefinition.ProvidesCover;
            if (CoverPropertiesPanel != null) CoverPropertiesPanel.Visibility = _currentTileDefinition.ProvidesCover ? Visibility.Visible : Visibility.Collapsed;
            if (CmbCoverType != null) CmbCoverType.SelectedIndex = (int)_currentTileDefinition.CoverType;

            // Interactive
            if (ChkIsInteractive != null) ChkIsInteractive.IsChecked = _currentTileDefinition.IsInteractive;
            if (InteractivePropertiesPanel != null) InteractivePropertiesPanel.Visibility = _currentTileDefinition.IsInteractive ? Visibility.Visible : Visibility.Collapsed;
            if (CmbInteractionType != null) SetInteractionTypeIndex(_currentTileDefinition.InteractionType);
            if (TxtInteractionData != null) TxtInteractionData.Text = _currentTileDefinition.InteractionData ?? "";
        }

        private void SetInteractionTypeIndex(string interactionType)
        {
            int index = interactionType switch
            {
                "OpenClose" => 0,
                "Loot" => 1,
                "Lever" => 2,
                "Button" => 3,
                "Custom" => 4,
                _ => 0
            };
            CmbInteractionType.SelectedIndex = index;
        }

        private string GetInteractionTypeFromIndex(int index)
        {
            return index switch
            {
                0 => "OpenClose",
                1 => "Loot",
                2 => "Lever",
                3 => "Button",
                4 => "Custom",
                _ => "OpenClose"
            };
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

        #endregion

        #region Event Handlers - Tile Definition Properties

        private void TileDefProperty_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingProperties || _currentTileDefinition == null) return;

            // Update collision/vision
            _currentTileDefinition.BlocksMovement = ChkBlocksMovement?.IsChecked ?? false;
            _currentTileDefinition.BlocksSight = ChkBlocksSight?.IsChecked ?? false;
            _currentTileDefinition.BlocksLight = ChkBlocksLight?.IsChecked ?? false;

            // Update terrain
            _currentTileDefinition.IsDifficultTerrain = ChkDifficultTerrain?.IsChecked ?? false;
            _currentTileDefinition.MovementCostMultiplier = _currentTileDefinition.IsDifficultTerrain ? 2.0 : 1.0;

            // Update door properties
            _currentTileDefinition.IsLockedByDefault = ChkIsLocked?.IsChecked ?? false;
            if (int.TryParse(TxtLockDC?.Text, out int lockDC))
                _currentTileDefinition.LockDC = lockDC;
            if (int.TryParse(TxtBreakDC?.Text, out int breakDC))
                _currentTileDefinition.BreakDC = breakDC;

            // Update interactive data
            _currentTileDefinition.InteractionData = TxtInteractionData?.Text;

            TileDefinitionChanged?.Invoke(_currentTileDefinition);
            System.Diagnostics.Debug.WriteLine($"[TileProperties] Updated tile definition: {_currentTileDefinition.DisplayName}");
        }

        private void TileDefProperty_Changed(object sender, TextChangedEventArgs e)
        {
            TileDefProperty_Changed(sender, (RoutedEventArgs)e);
        }

        private void IsDoor_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingProperties || _currentTileDefinition == null) return;

            _currentTileDefinition.IsDoor = ChkIsDoor?.IsChecked ?? false;
            DoorPropertiesPanel.Visibility = _currentTileDefinition.IsDoor ? Visibility.Visible : Visibility.Collapsed;

            // Auto-set blocking properties for doors
            if (_currentTileDefinition.IsDoor)
            {
                _currentTileDefinition.BlocksMovement = true;
                _currentTileDefinition.BlocksSight = true;
                ChkBlocksMovement.IsChecked = true;
                ChkBlocksSight.IsChecked = true;
            }

            TileDefinitionChanged?.Invoke(_currentTileDefinition);
        }

        private void ProvidesCover_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingProperties || _currentTileDefinition == null) return;

            _currentTileDefinition.ProvidesCover = ChkProvidesCover?.IsChecked ?? false;
            CoverPropertiesPanel.Visibility = _currentTileDefinition.ProvidesCover ? Visibility.Visible : Visibility.Collapsed;

            if (!_currentTileDefinition.ProvidesCover)
            {
                _currentTileDefinition.CoverType = CoverType.None;
            }
            else if (_currentTileDefinition.CoverType == CoverType.None)
            {
                _currentTileDefinition.CoverType = CoverType.Half;
                CmbCoverType.SelectedIndex = 1;
            }

            TileDefinitionChanged?.Invoke(_currentTileDefinition);
        }

        private void CoverType_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingProperties || _currentTileDefinition == null) return;

            _currentTileDefinition.CoverType = (CoverType)CmbCoverType.SelectedIndex;
            TileDefinitionChanged?.Invoke(_currentTileDefinition);
        }

        private void IsInteractive_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingProperties || _currentTileDefinition == null) return;

            _currentTileDefinition.IsInteractive = ChkIsInteractive?.IsChecked ?? false;
            InteractivePropertiesPanel.Visibility = _currentTileDefinition.IsInteractive ? Visibility.Visible : Visibility.Collapsed;

            TileDefinitionChanged?.Invoke(_currentTileDefinition);
        }

        private void InteractionType_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingProperties || _currentTileDefinition == null) return;

            _currentTileDefinition.InteractionType = GetInteractionTypeFromIndex(CmbInteractionType.SelectedIndex);
            TileDefinitionChanged?.Invoke(_currentTileDefinition);
        }

        private void Notes_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingProperties || _currentTile == null) return;

            _currentTile.Notes = TxtNotes?.Text;
            TilePropertiesChanged?.Invoke(_currentTile);
        }

        #endregion

        #region Event Handlers - Metadata Addition

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

        #endregion

        #region Event Handlers - Metadata Management

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

        #endregion
    }
}