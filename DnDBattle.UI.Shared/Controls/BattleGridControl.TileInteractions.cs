using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.ViewModels;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DnDBattle.Views.Editors;
using DnDBattle.Views.TileMap;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Tile Interaction functionality (traps, secrets, interactive objects, etc.)
    /// </summary>
    public partial class BattleGridControl
    {
        #region Tile Metadata Services Setup

        private void SetupMetadataServices()
        {
            // Trap service (already exists)
            _trapService.LogMessage += (message) => AddToActionLog("Trap", message);
            _trapService.TrapDetected += (token, trap) =>
            {
                System.Windows.MessageBox.Show(
                    $"{token.Name} detected a trap!\n\n{trap.DetectionDescription}",
                    "🔍 Trap Detected",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            };
            _trapService.TrapTriggered += (token, trap) =>
            {
                ShowTrapTriggerEffect(token.GridX, token.GridY);
                RebuildTokenVisuals();
            };
            _trapService.TrapDisarmed += (token, trap) =>
            {
                System.Windows.MessageBox.Show(
                    $"{token.Name} successfully disarmed the trap!",
                    "✅ Trap Disarmed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            };
            _trapService.RequestManualRoll += OnRequestManualRoll;

            // Metadata interaction service (NEW)
            _metadataService.LogMessage += (message) => AddToActionLog("Metadata", message);

            _metadataService.SecretDiscovered += (secret) =>
            {
                System.Windows.MessageBox.Show(
                    $"Secret Discovered!\n\n{secret.DiscoveryDescription}",
                    "🔍 Secret Found",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            };

            _metadataService.ObjectActivated += (interactive) =>
            {
                RebuildTokenVisuals(); // Refresh in case state changed
            };

            _metadataService.HazardTriggered += (token, hazard) =>
            {
                ShowHazardEffect(token.GridX, token.GridY, hazard.DamageType);
                RebuildTokenVisuals(); // Update HP display
            };
            _hazardTracking.LogMessage += (message) => AddToActionLog("Hazard", message);
            _hazardTracking.HazardDamageApplied += (token, hazard, damage) =>
            {
                ShowHazardEffect(token.GridX, token.GridY, hazard.DamageType);
                RebuildTokenVisuals();
            };


            _spawnService.LogMessage += (message) => AddToActionLog("Spawn", message);
            _spawnService.CreaturesSpawned += (spawn, tokens) =>
            {
                // Add tokens to the battle
                if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
                {
                    foreach (var token in tokens)
                    {
                        vm.Tokens.Add(token);
                    }
                }
                RebuildTokenVisuals();
            };

            _metadataService.TokenTeleported += (token, delay) =>
            {
                RebuildTokenVisuals(); // Refresh token position
                RedrawMovementOverlay();
            };

            _metadataService.TokenHealed += (token, amount) =>
            {
                ShowHealingEffect(token.GridX, token.GridY);
                RebuildTokenVisuals(); // Update HP display
            };

            _metadataService.RequestManualRoll += OnRequestManualRoll;
        }

        #endregion

        #region Trap Detection and Disarm

        /// <summary>
        /// Check for traps when a token moves to a new position
        /// </summary>
        private void CheckForTrapsAtPosition(Token token, int gridX, int gridY)
        {
            if (_loadedTileMap == null || token == null) return;

            var tile = GetTileAt(gridX, gridY);
            if (tile != null && tile.HasMetadata)
            {
                _trapService.CheckForTraps(token, tile);
            }
        }

        /// <summary>
        /// Manually trigger trap detection for a token at current position
        /// </summary>
        public void TriggerTrapDetection(Token token)
        {
            if (token == null) return;

            var tile = GetTileAt(token.GridX, token.GridY);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Trap))
            {
                var traps = tile.GetMetadata(TileMetadataType.Trap).OfType<TrapMetadata>().ToList();

                if (traps.Count == 0) return;

                // Show detection prompt
                var trap = traps.First(); // Handle first trap for now

                if (trap.IsDetected || trap.IsDisarmed)
                {
                    AddToActionLog("Trap", $"This trap has already been {(trap.IsDetected ? "detected" : "disarmed")}.");
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    $"{token.Name} wants to search for traps.\n\nRoll Perception (DC {trap.DetectionDC})?",
                    "🔍 Search for Traps",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _trapService.AttemptDetection(token, trap);
                }
            }
        }

        /// <summary>
        /// Manually attempt to disarm a trap
        /// </summary>
        public void AttemptDisarmTrap(Token token)
        {
            if (token == null) return;

            var tile = GetTileAt(token.GridX, token.GridY);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Trap))
            {
                var traps = tile.GetMetadata(TileMetadataType.Trap).OfType<TrapMetadata>().ToList();
                var trap = traps.FirstOrDefault(t => t.IsDetected && !t.IsDisarmed);

                if (trap == null)
                {
                    AddToActionLog("Trap", "No detected traps here to disarm.");
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    $"{token.Name} attempts to disarm the trap.\n\nRoll {trap.DisarmSkill} (DC {trap.DisarmDC})?",
                    "🔧 Disarm Trap",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _trapService.AttemptDisarm(token, trap);
                }
            }
        }

        #endregion

        #region Manual Roll Request

        private (bool proceed, int roll) OnRequestManualRoll(Token token, TileMetadata metadata, string skillName, int dc)
        {
            var dialog = new Views.TileMap.ManualRollDialog(
                token.Name,
                skillName,
                GetSkillModifier(token, skillName),
                dc);

            if (dialog.ShowDialog() == true)
            {
                return (true, dialog.Roll);
            }
            return (false, 0);
        }

        #endregion

        #region All Metadata Check

        // Update CheckForTrapsAtPosition to check ALL metadata
        private void CheckForAllMetadataAtPosition(Token token, int gridX, int gridY)
        {
            if (_loadedTileMap == null || token == null) return;

            var tile = GetTileAt(gridX, gridY);
            if (tile == null || !tile.HasMetadata) return;

            // Check for traps (auto-trigger)
            if (tile.HasMetadataType(TileMetadataType.Trap))
            {
                _trapService.CheckForTraps(token, tile);
            }

            // Check for hazards (auto-trigger)
            if (tile.HasMetadataType(TileMetadataType.Hazard))
            {
                _hazardTracking.UpdateTokenPosition(token, _loadedTileMap);
            }

            // Check for teleporters (prompt)
            if (tile.HasMetadataType(TileMetadataType.Teleporter))
            {
                var teleporter = tile.GetMetadata(TileMetadataType.Teleporter).OfType<TeleporterMetadata>().FirstOrDefault();
                if (teleporter != null && teleporter.IsActive)
                {
                    _metadataService.TriggerTeleporter(token, teleporter);
                }
            }

            // Check for healing zones
            if (tile.HasMetadataType(TileMetadataType.Healing))
            {
                var healingZone = tile.GetMetadata(TileMetadataType.Healing).OfType<HealingZoneMetadata>().FirstOrDefault();
                if (healingZone != null && healingZone.HealingTrigger == HealingTrigger.OnEnter)
                {
                    _metadataService.TriggerHealingZone(token, healingZone);
                }
            }

            // Check for proximity-triggered spawns (across entire map)
            if (token.IsPlayer)
            {
                CheckSpawnTriggers();
            }
        }

        #endregion

        #region Secret and Interactive Object Methods

        // Add these interaction methods
        public void SearchForSecrets(Token token)
        {
            if (token == null) return;

            var tile = GetTileAt(token.GridX, token.GridY);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Secret))
            {
                var secrets = tile.GetMetadata(TileMetadataType.Secret).OfType<SecretMetadata>().ToList();
                var secret = secrets.FirstOrDefault(s => !s.IsDiscovered);

                if (secret == null)
                {
                    AddToActionLog("Secret", "No undiscovered secrets here.");
                    return;
                }

                _metadataService.AttemptSecretDiscovery(token, secret);
            }
            else
            {
                AddToActionLog("Secret", $"{token.Name} searches but finds nothing.");
            }
        }

        public void InteractWithObject(Token token)
        {
            if (token == null) return;

            var tile = GetTileAt(token.GridX, token.GridY);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Interactive))
            {
                var interactive = tile.GetMetadata(TileMetadataType.Interactive).OfType<InteractiveMetadata>().FirstOrDefault();
                if (interactive != null)
                {
                    _metadataService.InteractWithObject(token, interactive);
                }
            }
            else
            {
                AddToActionLog("Interact", "Nothing to interact with here.");
            }
        }

        public void ActivateHealingZone(Token token)
        {
            if (token == null) return;

            var tile = GetTileAt(token.GridX, token.GridY);
            if (tile != null && tile.HasMetadataType(TileMetadataType.Healing))
            {
                var healingZone = tile.GetMetadata(TileMetadataType.Healing).OfType<HealingZoneMetadata>().FirstOrDefault();
                if (healingZone != null)
                {
                    _metadataService.TriggerHealingZone(token, healingZone);
                }
            }
        }

        #endregion

        #region Visual Effects

        // Add visual effect methods
        // VISUAL REFRESH
        private void ShowHazardEffect(int gridX, int gridY, DamageType damageType)
        {
            var color = damageType switch
            {
                DamageType.Fire => Colors.OrangeRed,
                DamageType.Cold => Colors.Cyan,
                DamageType.Acid => Colors.LimeGreen,
                DamageType.Lightning => Colors.Yellow,
                DamageType.Poison => Colors.Purple,
                _ => Colors.Red
            };

            ShowColoredEffect(gridX, gridY, color);
        }

        // VISUAL REFRESH
        private void ShowHealingEffect(int gridX, int gridY)
        {
            ShowColoredEffect(gridX, gridY, Colors.LightGreen);
        }

        // VISUAL REFRESH
        private void ShowColoredEffect(int gridX, int gridY, Color color)
        {
            var overlay = new System.Windows.Shapes.Rectangle
            {
                Width = GridCellSize,
                Height = GridCellSize,
                Fill = new SolidColorBrush(Color.FromArgb(150, color.R, color.G, color.B)),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 3
            };

            Canvas.SetLeft(overlay, gridX * GridCellSize);
            Canvas.SetTop(overlay, gridY * GridCellSize);
            Canvas.SetZIndex(overlay, 999);

            RenderCanvas.Children.Add(overlay);

            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(1.5)
            };

            animation.Completed += (s, e) => RenderCanvas.Children.Remove(overlay);
            overlay.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        // VISUAL REFRESH
        private void ShowTrapTriggerEffect(int gridX, int gridY)
        {
            // Create a pulsing red overlay at the trap location
            var overlay = new System.Windows.Shapes.Rectangle
            {
                Width = GridCellSize,
                Height = GridCellSize,
                Fill = new SolidColorBrush(Color.FromArgb(150, 244, 67, 54)),
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 3
            };

            Canvas.SetLeft(overlay, gridX * GridCellSize);
            Canvas.SetTop(overlay, gridY * GridCellSize);
            Canvas.SetZIndex(overlay, 999);

            RenderCanvas.Children.Add(overlay);

            // Animate and remove
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = false
            };

            animation.Completed += (s, e) =>
            {
                RenderCanvas.Children.Remove(overlay);
            };

            overlay.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        /// <summary>
        /// Refreshes all tile-interaction-related visuals by rebuilding token displays
        /// and updating the movement overlay.
        /// </summary>
        // VISUAL REFRESH
        public void RefreshTileInteractionVisuals()
        {
            RebuildTokenVisuals();
            RedrawMovementOverlay();
        }

        #endregion

        #region Spawn Points

        public void ActivateSpawnPoint(SpawnMetadata spawn)
        {
            if (_loadedTileMap == null)
            {
                AddToActionLog("Spawn", "No tile map loaded.");
                return;
            }

            if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
            {
                var spawnedTokens = _spawnService.ActivateSpawnPoint(spawn, vm.CreatureBank, _loadedTileMap, GridCellSize);

                if (spawnedTokens.Count > 0)
                {
                    AddToActionLog("Spawn", $"✅ Spawned {spawnedTokens.Count} creatures!");
                }
            }
        }

        // Add method to check automatic spawn triggers
        private void CheckSpawnTriggers(bool combatJustStarted = false)
        {
            if (_loadedTileMap == null) return;

            if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
            {
                var triggeredSpawns = _spawnService.CheckSpawnTriggers(
                    _loadedTileMap,
                    vm.Tokens,
                    vm.CurrentRound,
                    combatJustStarted);

                foreach (var spawn in triggeredSpawns)
                {
                    // Apply delay if configured
                    if (spawn.SpawnDelay > 0)
                    {
                        AddToActionLog("Spawn", $"⏳ {spawn.Name} will spawn in {spawn.SpawnDelay} rounds...");
                        // TODO: Implement delayed spawning queue
                    }
                    else
                    {
                        ActivateSpawnPoint(spawn);
                    }
                }
            }
        }

        #endregion

        #region Skill Modifier Helper

        private int GetSkillModifier(Token token, string skill)
        {
            return skill.ToUpper() switch
            {
                "PERCEPTION" => token.WisMod,
                "INVESTIGATION" => token.IntMod,
                "THIEVES' TOOLS" => token.DexMod,
                "SLEIGHT OF HAND" => token.DexMod,
                "ARCANA" => token.IntMod,
                "STR" => token.StrMod,
                "DEX" => token.DexMod,
                "CON" => token.ConMod,
                "INT" => token.IntMod,
                "WIS" => token.WisMod,
                "CHA" => token.ChaMod,
                _ => 0
            };
        }

        #endregion
    }
}
