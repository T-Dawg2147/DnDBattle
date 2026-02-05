using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing token context menu and targeting functionality
    /// </summary>
    public partial class BattleGridControl
    {
        #region Token Context Menu

        private ContextMenu CreateTokenContextMenu(Token token)
        {
            var menu = new ContextMenu();

            var editItem = new MenuItem { Header = "📝 Edit Stats..." };
            editItem.Click += (s, e) => RequestEditToken?.Invoke(token);
            menu.Items.Add(editItem);

            var duplicateItem = new MenuItem { Header = "📋 Duplicate" };
            duplicateItem.Click += (s, e) => RequestDuplicateToken?.Invoke(token);
            menu.Items.Add(duplicateItem);

            menu.Items.Add(new Separator());

            // ===== TILE INTERACTION MENU =====
            var tileMenu = new MenuItem { Header = "🗺️ Tile Actions" };

            var tile = GetTileAt(token.GridX, token.GridY);
            bool hasTileActions = false;

            // Search for Traps
            var searchTrapsItem = new MenuItem { Header = "⚠️ Search for Traps (Shift+F)" };
            searchTrapsItem.Click += (s, e) => TriggerTrapDetection(token);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Trap))
            {
                var traps = tile.GetMetadata(TileMetadataType.Trap).OfType<TrapMetadata>().ToList();
                if (traps.Any(t => !t.IsDetected && !t.IsDisarmed))
                {
                    searchTrapsItem.IsEnabled = true;
                    hasTileActions = true;
                }
                else
                {
                    searchTrapsItem.IsEnabled = false;
                }
            }
            else
            {
                searchTrapsItem.IsEnabled = false;
            }
            tileMenu.Items.Add(searchTrapsItem);

            // Disarm Trap
            var disarmItem = new MenuItem { Header = "🔧 Disarm Trap (Shift+T)" };
            disarmItem.Click += (s, e) => AttemptDisarmTrap(token);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Trap))
            {
                var traps = tile.GetMetadata(TileMetadataType.Trap).OfType<TrapMetadata>().ToList();
                if (traps.Any(t => t.IsDetected && !t.IsDisarmed))
                {
                    disarmItem.IsEnabled = true;
                    hasTileActions = true;
                }
                else
                {
                    disarmItem.IsEnabled = false;
                }
            }
            else
            {
                disarmItem.IsEnabled = false;
            }
            tileMenu.Items.Add(disarmItem);

            tileMenu.Items.Add(new Separator());

            // Search for Secrets
            var searchSecretsItem = new MenuItem { Header = "🔍 Search for Secrets (Shift+S)" };
            searchSecretsItem.Click += (s, e) => SearchForSecrets(token);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Secret))
            {
                var secrets = tile.GetMetadata(TileMetadataType.Secret).OfType<SecretMetadata>().ToList();
                if (secrets.Any(s => !s.IsDiscovered))
                {
                    searchSecretsItem.IsEnabled = true;
                    hasTileActions = true;
                }
                else
                {
                    searchSecretsItem.Header = "✅ Secret Already Discovered";
                    searchSecretsItem.IsEnabled = false;
                }
            }
            else
            {
                searchSecretsItem.IsEnabled = false;
            }
            tileMenu.Items.Add(searchSecretsItem);

            // Interact with Object
            var interactItem = new MenuItem { Header = "⚙️ Interact (Shift+E)" };
            interactItem.Click += (s, e) => InteractWithObject(token);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Interactive))
            {
                interactItem.IsEnabled = true;
                hasTileActions = true;
            }
            else
            {
                interactItem.IsEnabled = false;
            }
            tileMenu.Items.Add(interactItem);

            // Use Healing Zone
            var healZone = new MenuItem { Header = "💚 Use Healing Source (Shift+H)" };
            healZone.Click += (s, e) => ActivateHealingZone(token);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Healing))
            {
                var healingZone = tile.GetMetadata(TileMetadataType.Healing).OfType<HealingZoneMetadata>().FirstOrDefault();
                if (healingZone != null && healingZone.CanHeal(token.Id))
                {
                    healZone.IsEnabled = true;
                    hasTileActions = true;
                }
                else
                {
                    healZone.Header = "💚 Already Used Healing Source";
                    healZone.IsEnabled = false;
                }
            }
            else
            {
                healZone.IsEnabled = false;
            }
            tileMenu.Items.Add(healZone);

            tileMenu.IsEnabled = hasTileActions;
            menu.Items.Add(tileMenu);

            tileMenu.Items.Add(new Separator());

            // View Tile Info
            var infoItem = new MenuItem { Header = "ℹ️ View Tile Info" };
            infoItem.Click += (s, e) =>
            {
                if (tile != null)
                {
                    string info = $"Tile at ({tile.GridX}, {tile.GridY})\n\n";
                    if (tile.HasMetadata)
                    {
                        info += $"Metadata ({tile.Metadata.Count}):\n";
                        foreach (var meta in tile.Metadata)
                        {
                            info += $"  • {meta.Type.GetIcon()} {meta.Name ?? meta.Type.GetDisplayName()}\n";
                        }
                    }
                    else
                    {
                        info += "No metadata";
                    }

                    MessageBox.Show(info, "Tile Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            infoItem.IsEnabled = tile != null;
            tileMenu.Items.Add(infoItem);

            // === CONDITIONS SUBMENU ===
            var conditionsMenu = new MenuItem { Header = "🏷️ Conditions" };

            // Common conditions
            var commonConditions = new[] {
                Models.Condition.Blinded, Models.Condition.Charmed, Models.Condition.Deafened, Models.Condition.Frightened,
                Models.Condition.Grappled, Models.Condition.Incapacitated, Models.Condition.Invisible, Models.Condition.Paralyzed,
                Models.Condition.Petrified, Models.Condition.Poisoned, Models.Condition.Prone, Models.Condition.Restrained,
                Models.Condition.Stunned, Models.Condition.Unconscious
            };

            foreach (var condition in commonConditions)
            {
                var condItem = new MenuItem
                {
                    Header = $"{ConditionExtensions.GetConditionIcon(condition)} {ConditionExtensions.GetConditionName(condition)}",
                    IsCheckable = true,
                    IsChecked = token.HasCondition(condition),
                    Tag = condition
                };
                condItem.Click += (s, e) =>
                {
                    token.ToggleCondition((Models.Condition)((MenuItem)s).Tag);
                    ((MenuItem)s).IsChecked = token.HasCondition((Models.Condition)((MenuItem)s).Tag);
                    RebuildTokenVisuals();
                    AddToActionLog("Condition", $"{token.Name}: {(token.HasCondition((Models.Condition)((MenuItem)s).Tag) ? "+" : "-")}{ConditionExtensions.GetConditionName((Models.Condition)((MenuItem)s).Tag)}");
                };
                conditionsMenu.Items.Add(condItem);
            }

            conditionsMenu.Items.Add(new Separator());

            // Exhaustion submenu
            var exhaustionMenu = new MenuItem { Header = "😓 Exhaustion" };
            for (int i = 0; i <= 6; i++)
            {
                var level = i;
                var exhItem = new MenuItem
                {
                    Header = i == 0 ? "None" : $"Level {i}",
                    IsCheckable = true,
                    IsChecked = token.Conditions.GetExhaustionLevel() == i
                };
                exhItem.Click += (s, e) =>
                {
                    token.Conditions = token.Conditions.SetExhaustionLevel(level);
                    RebuildTokenVisuals();
                    AddToActionLog("Exhaustion", $"{token.Name}: Exhaustion level {level}");
                };
                exhaustionMenu.Items.Add(exhItem);
            }
            conditionsMenu.Items.Add(exhaustionMenu);

            conditionsMenu.Items.Add(new Separator());

            // Special conditions
            var specialConditions = new[] {
                Models.Condition.Concentrating, Models.Condition.Dodging, Models.Condition.Hidden,
                Models.Condition.Blessed, Models.Condition.Cursed, Models.Condition.Hasted, Models.Condition.Slowed,
                Models.Condition.Flying, Models.Condition.Raging, Models.Condition.Marked, Models.Condition.HuntersMark
            };

            foreach (var condition in specialConditions)
            {
                var condItem = new MenuItem
                {
                    Header = $"{ConditionExtensions.GetConditionIcon(condition)} {ConditionExtensions.GetConditionName(condition)}",
                    IsCheckable = true,
                    IsChecked = token.HasCondition(condition),
                    Tag = condition
                };
                condItem.Click += (s, e) =>
                {
                    token.ToggleCondition((Models.Condition)((MenuItem)s).Tag);
                    ((MenuItem)s).IsChecked = token.HasCondition((Models.Condition)((MenuItem)s).Tag);
                    RebuildTokenVisuals();
                    AddToActionLog("Condition", $"{token.Name}: {(token.HasCondition((Models.Condition)((MenuItem)s).Tag) ? "+" : "-")}{ConditionExtensions.GetConditionName((Models.Condition)((MenuItem)s).Tag)}");
                };
                conditionsMenu.Items.Add(condItem);
            }

            conditionsMenu.Items.Add(new Separator());

            // Clear all conditions
            var clearAllItem = new MenuItem { Header = "❌ Clear All Conditions" };
            clearAllItem.Click += (s, e) =>
            {
                token.Conditions = Models.Condition.None;
                RebuildTokenVisuals();
                AddToActionLog("Condition", $"{token.Name}: All conditions cleared");
            };
            conditionsMenu.Items.Add(clearAllItem);

            menu.Items.Add(conditionsMenu);

            menu.Items.Add(new Separator());

            // Combat actions
            var rollInitItem = new MenuItem { Header = "🎲 Roll Initiative" };
            rollInitItem.Click += (s, e) =>
            {
                var roll = Utils.DiceRoller.RollExpression("1d20");
                token.Initiative = roll.Total + token.InitiativeModifier;
                AddToActionLog("Initiative", $"{token.Name} rolled {roll.Total} + {token.InitiativeModifier} = {token.Initiative}");
            };
            menu.Items.Add(rollInitItem);

            var healItem = new MenuItem { Header = "💚 Heal..." };
            healItem.Click += (s, e) => ShowHealDialog(token);
            menu.Items.Add(healItem);

            var damageItem = new MenuItem { Header = "💔 Damage..." };
            damageItem.Click += (s, e) => ShowDamageDialog(token);
            menu.Items.Add(damageItem);

            menu.Items.Add(new Separator());

            var deleteItem = new MenuItem { Header = "🗑️ Remove from Map" };
            deleteItem.Click += (s, e) => RequestDeleteToken?.Invoke(token);
            menu.Items.Add(deleteItem);

            return menu;
        }

        private void ShowHealDialog(Token token)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Heal {token.Name} by how much?", "Heal", "0");

            if (int.TryParse(input, out int amount) && amount > 0)
            {
                token.HP = Math.Min(token.HP + amount, token.MaxHP);
            }
        }

        private void ShowDamageDialog(Token token)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Damage {token.Name} by how much?", "Damage", "0");

            if (int.TryParse(input, out int amount) && amount > 0)
            {
                token.HP = Math.Max(token.HP - amount, 0);
            }
        }

        #endregion

        #region Tile Context Menu

        private ContextMenu CreateTileContextMenu(int gridX, int gridY)
        {
            var menu = new ContextMenu();
            var tile = GetTileAt(gridX, gridY);

            if (tile != null && tile.HasMetadataType(TileMetadataType.Spawn))
            {
                var spawns = tile.GetMetadata(TileMetadataType.Spawn).OfType<SpawnMetadata>().ToList();

                var spawnMenu = new MenuItem { Header = "👹 Spawn Points" };

                foreach (var spawn in spawns)
                {
                    var spawnItem = new MenuItem();

                    if (spawn.HasSpawned && !spawn.IsReusable)
                    {
                        spawnItem.Header = $"✅ {spawn.Name} (already spawned)";
                        spawnItem.IsEnabled = false;
                    }
                    else
                    {
                        spawnItem.Header = $"👹 Activate: {spawn.Name} ({spawn.SpawnCount}× {spawn.CreatureName})";
                        spawnItem.Click += (s, e) => ActivateSpawnPoint(spawn);
                    }

                    spawnMenu.Items.Add(spawnItem);
                }

                menu.Items.Add(spawnMenu);
            }
            else
            {
                var noSpawnItem = new MenuItem
                {
                    Header = "No spawn points here",
                    IsEnabled = false
                };
                menu.Items.Add(noSpawnItem);
            }

            return menu;
        }

        #endregion

        #region Targeting Mode

        /// <summary>
        /// Enters targeting mode - highlights valid targets
        /// </summary>
        public void EnterTargetingMode(TargetingState state)
        {
            _isInTargetingMode = true;
            _currentTargetingState = state;

            // Highlight valid targets
            HighlightValidTargets();

            // Change cursor
            RenderCanvas.Cursor = System.Windows.Input.Cursors.Cross;

            // Update status
            System.Diagnostics.Debug.WriteLine($"Entered targeting mode for {state.SelectedAction?.Name}");
        }

        /// <summary>
        /// Exits targeting mode
        /// </summary>
        public void ExitTargetingMode()
        {
            _isInTargetingMode = false;
            _currentTargetingState = null;

            // Remove highlights
            ClearTargetHighlights();

            // Reset cursor
            RenderCanvas.Cursor = System.Windows.Input.Cursors.Arrow;

            System.Diagnostics.Debug.WriteLine("Exited targeting mode");
        }

        /// <summary>
        /// Highlights all valid targets on the grid
        /// </summary>
        private void HighlightValidTargets()
        {
            ClearTargetHighlights();

            if (_currentTargetingState == null || Tokens == null) return;

            var sourceToken = _currentTargetingState.SourceToken;
            int actionRange = _currentTargetingState.ActionRange;

            foreach (var token in Tokens)
            {
                // Skip the source token
                if (token.Id == sourceToken.Id) continue;

                // Calculate distance
                int dx = Math.Abs(token.GridX - sourceToken.GridX);
                int dy = Math.Abs(token.GridY - sourceToken.GridY);
                int distance = Math.Max(dx, dy);

                // Determine if in range (considering movement for melee)
                bool inRange = false;
                bool inRangeWithMovement = false;

                if (_currentTargetingState.IsRangedAction)
                {
                    inRange = distance <= actionRange;
                }
                else // Melee
                {
                    int meleeRange = actionRange > 0 ? actionRange : 1;
                    inRange = distance <= meleeRange;

                    if (!inRange)
                    {
                        int movementNeeded = distance - meleeRange;
                        inRangeWithMovement = movementNeeded <= sourceToken.MovementRemainingThisTurn;
                    }
                }

                // Create highlight
                if (inRange || inRangeWithMovement)
                {
                    var highlight = CreateTargetHighlight(token, inRange, inRangeWithMovement);
                    _targetHighlights[token] = highlight;
                    RenderCanvas.Children.Add(highlight);
                }
            }
        }

        /// <summary>
        /// Creates a highlight border around a potential target
        /// </summary>
        private Border CreateTargetHighlight(Token token, bool inRange, bool requiresMovement)
        {
            double size = token.SizeInSquares * GridCellSize;
            double x = token.GridX * GridCellSize;
            double y = token.GridY * GridCellSize;

            Color highlightColor;
            if (inRange)
            {
                highlightColor = Color.FromArgb(100, 76, 175, 80); // Green - in range
            }
            else if (requiresMovement)
            {
                highlightColor = Color.FromArgb(100, 255, 193, 7); // Yellow - requires movement
            }
            else
            {
                highlightColor = Color.FromArgb(100, 244, 67, 54); // Red - out of range
            }

            var highlight = new Border
            {
                Width = size + 8,
                Height = size + 8,
                Background = new SolidColorBrush(highlightColor),
                BorderBrush = new SolidColorBrush(Color.FromArgb(200, highlightColor.R, highlightColor.G, highlightColor.B)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(size / 2 + 4),
                IsHitTestVisible = false,
                Tag = "TargetHighlight"
            };

            Canvas.SetLeft(highlight, x - 4);
            Canvas.SetTop(highlight, y - 4);
            Canvas.SetZIndex(highlight, 50); // Above grid, below tokens

            return highlight;
        }

        /// <summary>
        /// Clears all target highlights
        /// </summary>
        private void ClearTargetHighlights()
        {
            foreach (var highlight in _targetHighlights.Values)
            {
                RenderCanvas.Children.Remove(highlight);
            }
            _targetHighlights.Clear();

            // Also remove any stray highlights
            var toRemove = RenderCanvas.Children.OfType<Border>()
                .Where(b => b.Tag as string == "TargetHighlight")
                .ToList();

            foreach (var item in toRemove)
            {
                RenderCanvas.Children.Remove(item);
            }
        }

        #endregion
    }
}
