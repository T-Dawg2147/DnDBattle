using System;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Services.Vision;
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
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using DnDBattle.Views.Editors;
using DnDBattle.Views.TileMap;
using Action = DnDBattle.Models.Combat.Action;

namespace DnDBattle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _autosaveTimer;

        private CombatStatisticsService _combatStatsService;
        private TurnTimerService _turnTimerService;
        private SoundEffectsService _soundService;
        private DiceHistoryService _diceHistoryService;

        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;

            Services.Persistence.OptionsService.LoadOptions();
            UndoManager.Limit = Options.UndoStackLimit;
            UndoManager.StateChanged += UndoManager_StateChanged;

            vm.Tokens.CollectionChanged += (s, e) => AutosaveNow();

            _autosaveTimer = new DispatcherTimer();
            _autosaveTimer.Interval = TimeSpan.FromSeconds(Options.AutosaveIntervalSeconds);
            _autosaveTimer.Tick += (s, e) => AutosaveNow();
            if (Options.EnabledPeriodicAutosave)
                _autosaveTimer.Start();

            // Wire up SelectedToken changes to update the panel
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.SelectedToken))
                {
                    SelectedTokenPanel.SetToken(vm.SelectedToken);
                }

                // Refresh panel when combat state changes
                if (e.PropertyName == nameof(MainViewModel.IsInCombat) ||
                    e.PropertyName == nameof(MainViewModel.CurrentTurnToken))
                {
                    if (vm.SelectedToken != null)
                    {
                        SelectedTokenPanel.UpdateDisplay();
                    }
                }
            };

            // Setup panels
            SetupInitiativeTracker();

            SetupSelectedTokenPanel();

            SetupAreaEffectToolbar();

            BattleGrid.TokenAddedToMap += (token) =>
            {
                InitiativeTracker.AddToken(token);
            };

            BattleGrid.TokenDoubleClicked += OnTokenDoubleClicked;

            Loaded += MainWindow_Loaded;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            this.Closing += (s, e) => Services.Persistence.OptionsService.SaveOptions();
            Closing += (s, e) => AutosaveNow();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                await vm.LoadCreaturesFromDatabaseAsync();
            }
        }

        #region Startup events
        private void InitializeServices()
        {
            _combatStatsService = new CombatStatisticsService();
            _turnTimerService = new TurnTimerService();
            _soundService = new SoundEffectsService();
            _diceHistoryService = new DiceHistoryService();

            // Load settings
            _turnTimerService.IsEnabled = Options.TurnTimerEnabled;
            _turnTimerService.SetTimeLimit(Options.TurnTimerSeconds);
            _soundService.IsEnabled = Options.SoundEffectsEnabled;
            _soundService.Volume = Options.SoundEffectsVolume;
        }

        private void SetupAreaEffectToolbar()
        {
            AoeToolbar.ShapeSelected += (shape) =>
            {
                BattleGrid.StartAreaEffectPlacement(shape, AoeToolbar.CurrentSize, AoeToolbar.CurrentColor);
            };

            AoeToolbar.SizeChanged += (size) =>
            {
                BattleGrid.UpdateAreaEffectSize(size);
            };

            AoeToolbar.ColorChanged += (color) =>
            {
                BattleGrid.UpdateAreaEffectColor(color);
            };

            AoeToolbar.PresetSelected += (preset) =>
            {
                BattleGrid.StartAreaEffectPlacement(preset);
            };

            AoeToolbar.CancelRequested += () =>
            {
                BattleGrid.CancelAreaEffectPlacement();
            };

            AoeToolbar.ClearAllRequested += () =>
            {
                BattleGrid.AreaEffectService.ClearAllEffects();
            };
        }

        private void SetupSelectedTokenPanel()
        {
            SelectedTokenPanel.LogAction += (message) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.ActionLog.Insert(0, new ActionLogEntry()
                    {
                        Timestamp = DateTime.Now,
                        Source = "Action",
                        Message = message
                    });
                }
            };

            SelectedTokenPanel.ActionSelected += (token, action) =>
            {
                Debug.WriteLine($"{token.Name} selected action: {action.Name}");
            };

            SelectedTokenPanel.TargetingStarted += (state) =>
            { 
                BattleGrid.EnterTargetingMode(state); 
            };

            SelectedTokenPanel.TargetingCancelled += () =>
            {
                BattleGrid.ExitTargetingMode();
            };

            SelectedTokenPanel.ActionResolved += (result) =>
            {
                // Refresh visuals after damage is applied
                BattleGrid.RebuildTokenVisuals();

                // Check for concentration save needed
                if (result.Success && result.TargetWasConcentrating && result.DamageForConcentration > 0)
                {
                    // Small delay to let the user see the damage result first
                    Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        SelectedTokenPanel.PromptConcentrationCheck(result.DamageForConcentration);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }

                // If target is dead, show message
                if (result.Target != null && result.Target.HP <= 0)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        if (result.Target.IsDead)
                        {
                            vm.ActionLog.Insert(0, new ActionLogEntry
                            {
                                Timestamp = DateTime.Now,
                                Source = "Combat",
                                Message = $"💀 {result.Target.Name} has died!"
                            });
                        }
                        else
                        {
                            vm.ActionLog.Insert(0, new ActionLogEntry
                            {
                                Timestamp = DateTime.Now,
                                Source = "Combat",
                                Message = $"💀 {result.Target.Name} has fallen unconscious!"
                            });
                        }
                    }
                }
            };

            SelectedTokenPanel.ActionResolved += (result) =>
            {
                BattleGrid.RebuildTokenVisuals();

                if (result.Target != null && result.Target.HP <= 0)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.ActionLog.Insert(0, new ActionLogEntry()
                        {
                            Timestamp = DateTime.Now,
                            Source = "Combat",
                            Message = $"💀 {result.Target.Name} has fallen!"
                        });
                    }
                }
            };

            BattleGrid.TargetSelected += (target) =>
            {
                SelectedTokenPanel.OnTargetSelected(target);
            };
        }

        private void SetupInitiativeTracker()
        {
            if (DataContext is MainViewModel vm)
            {
                InitiativeTracker.SetViewModel(vm);
            }

            InitiativeTracker.LogAction += (message) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.ActionLog.Insert(0, new ActionLogEntry
                    {
                        Timestamp = DateTime.Now,
                        Source = "Initiative",
                        Message = message
                    });
                }
            };

            InitiativeTracker.TokenSelected += (token) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectedToken = token;
                }
                SelectedTokenPanel?.SetToken(token);
            };

            InitiativeTracker.CombatStarted += () =>
            {
                // Refresh token visuals to show turn indicators
                BattleGrid?.RebuildTokenVisuals();
            };

            InitiativeTracker.CombatEnded += () =>
            {
                BattleGrid?.RebuildTokenVisuals();
            };

            InitiativeTracker.TurnChanged += () =>
            {
                BattleGrid?.RebuildTokenVisuals();
                SelectedTokenPanel?.UpdateDisplay();
            };
        }

        private void SetupFogOfWar()
        {
            // Initialize fog service in battle grid
            BattleGrid.InitializeFogOfWar(); // You may need to make this public or call in Loaded event

            FogToolbar.FogEnabledChanged += (enabled) =>
            {
                BattleGrid.SetFogEnabled(enabled);

                if (DataContext is MainViewModel vm)
                {
                    vm.ActionLog.Insert(0, new ActionLogEntry
                    {
                        Timestamp = DateTime.Now,
                        Source = "Fog",
                        Message = enabled ? "🌫️ Fog of War enabled" : "🌅 Fog of War disabled"
                    });
                }
            };

            FogToolbar.BrushModeChanged += (mode) =>
            {
                BattleGrid.SetFogBrushMode(mode);
            };

            FogToolbar.BrushSizeChanged += (size) =>
            {
                BattleGrid.SetFogBrushSize(size);
            };

            FogToolbar.PlayerViewChanged += (isPlayerView) =>
            {
                BattleGrid.SetPlayerView(isPlayerView);
            };

            FogToolbar.RevealPlayersRequested += () =>
            {
                BattleGrid.RevealAroundPlayers();

                if (DataContext is MainViewModel vm)
                {
                    vm.ActionLog.Insert(0, new ActionLogEntry
                    {
                        Timestamp = DateTime.Now,
                        Source = "Fog",
                        Message = "👥 Revealed area around player tokens"
                    });
                }
            };

            FogToolbar.RevealAllRequested += () =>
            {
                BattleGrid.FogService.RevealAll();
            };

            FogToolbar.HideAllRequested += () =>
            {
                BattleGrid.FogService.ClearAll();
            };

            FogToolbar.ShapeToolSelected += (tool) =>
            {
                BattleGrid.StartFogShapeTool(tool);
            };
        }
        #endregion

        private void OnTokenDoubleClicked(Token token)
        {
            var detailsWindow = new TokenDetailsWindow(token)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            detailsWindow.Show();
        }

        private void UndoManager_StateChanged(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            Status_Undo.Text = $"Undo: {(UndoManager.CanUndo ? "Yes" : "No")} Redo: {(UndoManager.CanRedo ? "Yes" : "No")}";
            Status_Mode.Text = $"Mode: {(Options.LiveMode ? "Live" : "Normal")}, AutoAOO: {(Options.AutoResolveAOOs ? "On" : "Off")}";
        }

        private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (SelectedTokenPanel.IsTargeting)
                {
                    SelectedTokenPanel.CancelTargeting();
                    e.Handled = true;
                    return;
                }
            }

            if (BattleGrid == null) return;

            // Pass key to battle grid
            BattleGrid.HandleKeyDown(e.Key);

            double step = BattleGrid.GridCellSize * 3;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) step *= 2;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.Z:
                        UndoManager.Undo();
                        e.Handled = true;
                        return;
                    case Key.Y:
                        UndoManager.Redo();
                        e.Handled = true;
                        return;
                    case Key.S:
                        MenuSaveEncounter_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    case Key.D:
                        if (DataContext is MainViewModel vm && vm.SelectedToken != null)
                        {
                            var src = vm.SelectedToken;
                            var clone = new Token()
                            {
                                Name = src.Name + "(copy)",
                                ArmorClass = src.ArmorClass,
                                HP = src.HP,
                                InitiativeModifier = src.InitiativeModifier,
                                Speed = src.Speed,
                                Image = src.Image,
                                IsPlayer = src.IsPlayer,
                                SizeInSquares = src.SizeInSquares
                            };
                            vm.Tokens.Add(clone);
                            UndoManager.Record(new TokenAddAction(vm, clone), performNow: false);
                            UndoManager.Record(new TokenAddAction(vm, clone), performNow: true);
                            UpdateStatus();
                        }
                        e.Handled = true;
                        return;
                    case Key.Delete:
                        if (DataContext is MainViewModel mvm && mvm.SelectedToken != null)
                        {
                            var t = mvm.SelectedToken;
                            UndoManager.Record(new TokenRemoveAction(mvm, t));
                            UpdateStatus();
                        }
                        e.Handled = true;
                        return;
                }
            }

            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    BattleGrid.PanBy(step, 0);
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.D:
                    BattleGrid.PanBy(-step, 0);
                    e.Handled = true;
                    break;
                case Key.Up:
                case Key.W:
                    BattleGrid.PanBy(0, step);
                    e.Handled = true;
                    break;
                case Key.Down:
                case Key.S:
                    BattleGrid.PanBy(0, -step);
                    e.Handled = true;
                    break;
            }   
        }

        private void OpenTileMapBuilder_Click(object sender, RoutedEventArgs e)
        {
            var editor = new Views.TileMap.TileMapEditorWindow();
            editor.Show();
        }

        private void OpenDeveloperWindow_Click(object sender, RoutedEventArgs e)
        {
            var win = new DeveloperWindow();
            win.Show();
        }

        #region Phase 4: Lighting & Vision Menu Handlers

        /// <summary>
        /// Opens the Phase 4 Lighting & Vision management window
        /// </summary>
        private void OpenPhase4Window_Click(object sender, RoutedEventArgs e)
        {
            var window = new Phase4LightingWindow(BattleGrid);
            window.Owner = this;
            window.Show();
        }

        /// <summary>
        /// Quick-add a default point light at the center of the current view
        /// </summary>
        private void Phase4_QuickAddPointLight_Click(object sender, RoutedEventArgs e)
        {
            if (!Options.EnableLighting)
            {
                MessageBox.Show("Enable the lighting system first (Phase 4 menu or Developer Settings).", "Lighting Disabled");
                return;
            }
            var light = new LightSource
            {
                CenterGrid = new System.Windows.Point(10, 10),
                BrightRadius = Options.DefaultBrightLightRadius,
                DimRadius = Options.DefaultDimLightRadius,
                Intensity = 1.0,
                LightColor = Colors.LightYellow,
                IsEnabled = true,
                Type = LightType.Point,
                Label = "Quick Point Light"
            };
            BattleGrid.AddLight(light);
        }

        /// <summary>
        /// Quick-add a directional light at the center of the current view
        /// </summary>
        private void Phase4_QuickAddDirectionalLight_Click(object sender, RoutedEventArgs e)
        {
            if (!Options.EnableLighting || !Options.EnableDirectionalLights)
            {
                MessageBox.Show("Enable both the lighting system and directional lights first.", "Feature Disabled");
                return;
            }
            var light = new LightSource
            {
                CenterGrid = new System.Windows.Point(10, 10),
                BrightRadius = Options.DefaultBrightLightRadius,
                DimRadius = Options.DefaultDimLightRadius,
                Intensity = 1.0,
                LightColor = Colors.LightBlue,
                IsEnabled = true,
                Type = LightType.Directional,
                Direction = 0,
                ConeWidth = 60,
                Label = "Quick Directional Light"
            };
            BattleGrid.AddLight(light);
        }

        /// <summary>
        /// Toggle the vision overlay showing what player tokens can see
        /// </summary>
        private void Phase4_ToggleVisionOverlay_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.ToggleVisionOverlay(MenuPhase4VisionOverlay.IsChecked);
        }

        /// <summary>
        /// Manually update fog of war based on current token vision ranges
        /// </summary>
        private void Phase4_UpdateFogFromVision_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.UpdateFogFromTokenVision();
        }

        /// <summary>
        /// Clear the shadow cache forcing a full recalculation
        /// </summary>
        private void Phase4_ClearShadowCache_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.InvalidateShadowCache();
        }

        #endregion

        #region Phase 5: Advanced Token Features Menu Handlers

        /// <summary>
        /// Opens the Phase 5 Advanced Token Features management window
        /// </summary>
        private void OpenPhase5Window_Click(object sender, RoutedEventArgs e)
        {
            var window = new Phase5TokenFeaturesWindow(BattleGrid, DataContext as ViewModels.MainViewModel);
            window.Owner = this;
            window.Show();
        }

        /// <summary>
        /// Quick-add a Paladin Aura (10 sq radius) to the selected token
        /// </summary>
        private void Phase5_AddPaladinAura_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm && vm.SelectedToken != null)
            {
                vm.SelectedToken.Auras.Add(Models.Creatures.TokenAura.PaladinAura());
                BattleGrid.RedrawAuras();
            }
            else
            {
                MessageBox.Show("Please select a token first.", "No Token Selected");
            }
        }

        /// <summary>
        /// Quick-add Spirit Guardians aura to the selected token
        /// </summary>
        private void Phase5_AddSpiritGuardians_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm && vm.SelectedToken != null)
            {
                vm.SelectedToken.Auras.Add(Models.Creatures.TokenAura.SpiritGuardians());
                BattleGrid.RedrawAuras();
            }
            else
            {
                MessageBox.Show("Please select a token first.", "No Token Selected");
            }
        }

        /// <summary>
        /// Set elevation of selected token from menu Tag value
        /// </summary>
        private void Phase5_SetElevation_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm && vm.SelectedToken != null)
            {
                if (sender is MenuItem mi && int.TryParse(mi.Tag?.ToString(), out int elev))
                {
                    vm.SelectedToken.Elevation = elev;
                    BattleGrid.InitializePhase5Visuals();
                }
            }
            else
            {
                MessageBox.Show("Please select a token first.", "No Token Selected");
            }
        }

        /// <summary>
        /// Force refresh of all Phase 5 token visuals (auras, elevation badges, facing arrows)
        /// </summary>
        private void Phase5_RefreshVisuals_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.InitializePhase5Visuals();
        }

        #endregion

        #region Phase 6: Area Effects Expansion Menu Handlers

        /// <summary>
        /// Opens the Phase 6 Area Effects Expansion management window
        /// </summary>
        private void OpenPhase6Window_Click(object sender, RoutedEventArgs e)
        {
            var window = new Phase6AreaEffectsWindow(BattleGrid);
            window.Owner = this;
            window.Show();
        }

        /// <summary>
        /// Opens the Spell Library window for browsing and placing spells
        /// </summary>
        private void Phase6_OpenSpellLibrary_Click(object sender, RoutedEventArgs e)
        {
            var spellWindow = new SpellLibraryWindow();
            spellWindow.Owner = this;

            // When a spell is selected, start placement on the battle grid
            spellWindow.SpellSelected += (AreaEffect effect) =>
            {
                BattleGrid.StartAreaEffectPlacement(effect);
            };

            spellWindow.ShowDialog();
        }

        /// <summary>
        /// Quick-place a Fireball (20ft sphere) on the battle grid
        /// </summary>
        private void Phase6_PlaceFireball_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.StartAreaEffectPlacement(AreaEffectPresets.Fireball);
        }

        /// <summary>
        /// Quick-place Darkness (15ft sphere) on the battle grid
        /// </summary>
        private void Phase6_PlaceDarkness_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.StartAreaEffectPlacement(AreaEffectPresets.Darkness);
        }

        /// <summary>
        /// Quick-place Fog Cloud (20ft sphere) on the battle grid
        /// </summary>
        private void Phase6_PlaceFogCloud_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.StartAreaEffectPlacement(AreaEffectPresets.FogCloud);
        }

        /// <summary>
        /// Advance one combat round, ticking down all effect durations and logging expired effects
        /// </summary>
        private void Phase6_AdvanceRound_Click(object sender, RoutedEventArgs e)
        {
            var durationService = new EffectDurationService(BattleGrid.AreaEffectService);
            var expired = durationService.OnRoundEnd();

            if (expired.Count > 0)
            {
                string msg = $"Round advanced. Expired: {string.Join(", ", expired)}";
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.ActionLog.Insert(0, new ActionLogEntry { Source = "Phase6", Message = msg });
                }
                MessageBox.Show(msg, "Duration Tick", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.ActionLog.Insert(0, new ActionLogEntry { Source = "Phase6", Message = "Round advanced. No effects expired." });
                }
            }
        }

        /// <summary>
        /// Clear all active area effects from the battle grid
        /// </summary>
        private void Phase6_ClearAllEffects_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.AreaEffectService.ClearAllEffects();
        }

        #endregion

        #region Phase 7: Combat Automation Menu Handlers

        /// <summary>
        /// Opens the Phase 7 Combat Automation management window for attack rolls,
        /// saving throws, spell slots, concentration, conditions, and cover.
        /// </summary>
        private void OpenPhase7Window_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                var window = new Phase7CombatWindow(BattleGrid, vm);
                window.Owner = this;
                window.Show();
            }
        }

        /// <summary>
        /// Quick attack: rolls an attack from the selected token against the first other token.
        /// Uses the selected token's first action if available, otherwise a default melee attack.
        /// </summary>
        private void Phase7_QuickAttack_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel vm || vm.SelectedToken == null)
            {
                MessageBox.Show("Select an attacker token first.", "Quick Attack", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var attacker = vm.SelectedToken;
            var defender = vm.Tokens.FirstOrDefault(t => t != attacker);
            if (defender == null)
            {
                MessageBox.Show("No target token available.", "Quick Attack", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Use the attacker's first action or create a default melee attack
            var attack = attacker.Actions?.FirstOrDefault() ?? new Models.Combat.Action
            {
                Name = "Melee Attack",
                AttackBonus = 5,
                DamageExpression = "1d8+3"
            };

            var system = new AttackRollSystem();
            var result = system.RollAttack(attacker, defender, attack);

            string msg = $"{attacker.Name} attacks {defender.Name}: " +
                         $"d20({result.D20Roll})+{result.AttackBonus}={result.TotalAttack} vs AC {result.TargetAC} → " +
                         (result.Hit ? $"HIT for {result.ActualDamage} damage" : "MISS") +
                         (result.IsCriticalHit ? " (CRIT!)" : "");

            vm.ActionLog.Insert(0, new ActionLogEntry { Source = "Phase7", Message = msg });
            MessageBox.Show(msg, "Quick Attack", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Quick save: prompts for a DC and rolls a Dexterity save for the selected token.
        /// Uses DEX as the default ability for quick saves (most common for area effects).
        /// </summary>
        private void Phase7_QuickSave_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel vm || vm.SelectedToken == null)
            {
                MessageBox.Show("Select a token first.", "Quick Save", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var token = vm.SelectedToken;
            var system = new SavingThrowSystem();
            var result = system.RollSave(token, Ability.Dexterity, 15);

            string msg = $"{token.Name} DEX Save (DC 15): " +
                         $"d20({result.D20Roll})+{result.Modifier}={result.Total} → " +
                         (result.Success ? "SUCCESS" : "FAIL");

            vm.ActionLog.Insert(0, new ActionLogEntry { Source = "Phase7", Message = msg });
            MessageBox.Show(msg, "Quick Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Performs a long rest for the selected token, restoring all spell slots to maximum
        /// </summary>
        private void Phase7_LongRest_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel vm || vm.SelectedToken == null)
            {
                MessageBox.Show("Select a token first.", "Long Rest", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var token = vm.SelectedToken;
            token.SpellSlots.LongRest();
            vm.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = $"{token.Name} completed a long rest. All spell slots restored."
            });
        }

        /// <summary>
        /// Performs a short rest for the selected token (restores Warlock pact slots)
        /// </summary>
        private void Phase7_ShortRest_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel vm || vm.SelectedToken == null)
            {
                MessageBox.Show("Select a token first.", "Short Rest", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var token = vm.SelectedToken;
            token.SpellSlots.ShortRest();
            vm.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = $"{token.Name} completed a short rest."
            });
        }

        /// <summary>
        /// Checks concentration for the selected token, prompting for damage amount.
        /// DC = max(10, damage/2). Automatically breaks concentration on failure.
        /// </summary>
        private void Phase7_CheckConcentration_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel vm || vm.SelectedToken == null)
            {
                MessageBox.Show("Select a token first.", "Concentration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var token = vm.SelectedToken;
            if (!token.IsConcentrating)
            {
                MessageBox.Show($"{token.Name} is not concentrating on any spell.", "Concentration", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Use a default damage of 10 for the quick check
            var service = new ConcentrationService();
            var result = service.CheckConcentration(token, 10);

            string msg = result.ToString();
            vm.ActionLog.Insert(0, new ActionLogEntry { Source = "Phase7", Message = msg });
            MessageBox.Show(msg, "Concentration Check", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Phase 8: Advanced Map Features Menu Handlers

        /// <summary>
        /// Opens the Phase 8 Advanced Map Features management window.
        /// </summary>
        private void OpenPhase8Window_Click(object sender, RoutedEventArgs e)
        {
            var window = new Phase8MapFeaturesWindow(BattleGrid);
            window.Owner = this;
            window.Show();
        }

        /// <summary>
        /// Sets the grid type on the current tile map from the menu Tag.
        /// </summary>
        private void Phase8_SetGridType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not string tag) return;

            var map = BattleGrid?.TileMap;
            if (map == null) return;

            map.GridType = tag switch
            {
                "HexFlatTop" => Models.Tiles.GridType.HexFlatTop,
                "HexPointyTop" => Models.Tiles.GridType.HexPointyTop,
                _ => Models.Tiles.GridType.Square
            };

            if (DataContext is MainViewModel vm)
            {
                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Phase8",
                    Message = $"Grid type set to {map.GridType}."
                });
            }
        }

        /// <summary>
        /// Toggles gridless mode on the current tile map.
        /// </summary>
        private void Phase8_ToggleGridless_Click(object sender, RoutedEventArgs e)
        {
            var map = BattleGrid?.TileMap;
            if (map == null) return;

            map.GridlessMode = MenuGridlessMode.IsChecked;
            Options.EnableGridlessMode = map.GridlessMode;

            if (DataContext is MainViewModel vm)
            {
                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Phase8",
                    Message = $"Gridless mode: {(map.GridlessMode ? "ON" : "OFF")}."
                });
            }
        }

        /// <summary>
        /// Adds a map note at the center of the current map, prompting for text.
        /// </summary>
        private void Phase8_AddMapNote_Click(object sender, RoutedEventArgs e)
        {
            var map = BattleGrid?.TileMap;
            if (map == null)
            {
                MessageBox.Show("No map loaded.", "Map Note", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var text = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter note text:", "Add Map Note", "New note");

            if (string.IsNullOrWhiteSpace(text)) return;

            var note = new Models.Tiles.MapNote
            {
                Text = text,
                GridX = map.Width / 2,
                GridY = map.Height / 2,
                FontSize = Options.MapNoteDefaultFontSize,
                IsPlayerVisible = true
            };

            map.AddNote(note);

            if (DataContext is MainViewModel vm)
            {
                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Phase8",
                    Message = $"Added note '{text}' at ({note.GridX},{note.GridY})."
                });
            }
        }

        /// <summary>
        /// Opens the Phase 8 map features window focused on the map library tab.
        /// </summary>
        private void Phase8_OpenMapLibrary_Click(object sender, RoutedEventArgs e)
        {
            var window = new Phase8MapFeaturesWindow(BattleGrid);
            window.Owner = this;
            window.Show();
        }

        #endregion

        #region Multiplayer Menu Handlers

        private void Multiplayer_HostGame_Click(object sender, RoutedEventArgs e)
        {
            var window = new HostGameWindow();
            if (DataContext is ViewModels.MainViewModel vm)
            {
                window.SetTokens(vm.Tokens);
            }
            window.Owner = this;
            window.Show();
        }

        private void Multiplayer_JoinGame_Click(object sender, RoutedEventArgs e)
        {
            var window = new JoinGameWindow();
            window.Owner = this;
            window.Show();
        }

        private void Multiplayer_VoiceChat_Click(object sender, RoutedEventArgs e)
        {
            var window = new VoiceChatWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        private void Multiplayer_CloudSave_Click(object sender, RoutedEventArgs e)
        {
            var window = new CloudSaveWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        #endregion

        #region Experimental / Undecided Features Menu Handlers

        /// <summary>
        /// Opens the Undecided/Experimental Features management window.
        /// </summary>
        private void OpenExperimentalWindow_Click(object sender, RoutedEventArgs e)
        {
            var window = new UndecidedFeaturesWindow();
            window.Owner = this;
            window.Show();
        }

        /// <summary>
        /// Quick-sets weather type from the Experimental menu Tag.
        /// Creates a WeatherService instance, applies the weather, and logs the action.
        /// </summary>
        private void Experimental_SetWeather_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not string tag) return;

            if (!Enum.TryParse<Models.Environment.WeatherType>(tag, out var weatherType))
                weatherType = Models.Environment.WeatherType.None;

            var weatherService = new WeatherService(Options.WeatherMaxParticles);
            weatherService.SetWeather(weatherType, 0.5);

            if (DataContext is MainViewModel vm)
            {
                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Experimental",
                    Message = $"Weather set to {weatherType}."
                });
            }
        }

        /// <summary>
        /// Quick-sets time of day from the Experimental menu Tag.
        /// </summary>
        private void Experimental_SetTimeOfDay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not string tag) return;

            if (!Enum.TryParse<Models.Environment.TimeOfDay>(tag, out var tod))
                tod = Models.Environment.TimeOfDay.Day;

            var weatherService = new WeatherService(Options.WeatherMaxParticles);
            weatherService.SetTimeOfDay(tod);

            if (DataContext is MainViewModel vm)
            {
                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Experimental",
                    Message = $"Time of day set to {tod}."
                });
            }
        }

        /// <summary>
        /// Quick dice roll from the Experimental menu. Rolls the dice type specified
        /// in the Tag, skips animation, and shows the result in a MessageBox.
        /// </summary>
        private void Experimental_RollDice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item || item.Tag is not string tag) return;

            if (!Enum.TryParse<Models.Combat.DiceType>(tag, out var diceType))
                diceType = Models.Combat.DiceType.D20;

            var diceService = new DicePhysicsService();
            diceService.Roll(diceType, 1);
            diceService.SkipAnimation();

            int total = diceService.GetTotal();
            MessageBox.Show($"🎲 {tag} result: {total}", "Quick Dice Roll",
                MessageBoxButton.OK, MessageBoxImage.Information);

            if (DataContext is MainViewModel vm)
            {
                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Experimental",
                    Message = $"Quick roll {tag}: {total}."
                });
            }
        }

        /// <summary>
        /// Generates a procedural dungeon map with default settings and logs the result.
        /// </summary>
        private void Experimental_GenerateMap_Click(object sender, RoutedEventArgs e)
        {
            var service = new ProceduralMapService();
            var config = new Models.Environment.ProceduralMapConfig
            {
                Type = Models.Environment.MapGenerationType.Dungeon,
                Width = 50,
                Height = 50,
                TargetRoomCount = 15
            };

            var result = service.Generate(config);
            MessageBox.Show(
                $"Generated Dungeon map ({result.Width}×{result.Height}) with {result.Rooms.Count} rooms.",
                "Procedural Map Generated", MessageBoxButton.OK, MessageBoxImage.Information);

            if (DataContext is MainViewModel vm)
            {
                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Experimental",
                    Message = $"Generated procedural dungeon ({result.Width}×{result.Height}, {result.Rooms.Count} rooms)."
                });
            }
        }

        /// <summary>
        /// Adds a distance measurement from (0,0) to (5,5) as a quick demo.
        /// </summary>
        private void Experimental_AddMeasurement_Click(object sender, RoutedEventArgs e)
        {
            var service = new MeasurementService();
            service.AddDistanceMeasurement("Quick Measurement", 0, 0, 5, 5);

            MessageBox.Show("Added distance measurement from (0,0) to (5,5).",
                "Measurement Added", MessageBoxButton.OK, MessageBoxImage.Information);

            if (DataContext is MainViewModel vm)
            {
                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Experimental",
                    Message = "Added quick distance measurement (0,0)→(5,5)."
                });
            }
        }

        /// <summary>
        /// Opens the Experimental Features window focused on accessibility settings.
        /// </summary>
        private void Experimental_AccessibilitySettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new UndecidedFeaturesWindow();
            window.Owner = this;
            window.Show();
        }

        #endregion

        private void ToggleLeftSidebar_Click(object seder, RoutedEventArgs e)
        {
            if (LeftSidebarColumn.Width.Value > 0)
            {
                LeftSidebarColumn.Width = new GridLength(0);
            }
            else
            {
                LeftSidebarColumn.Width = new GridLength(280);
            }
        }

        private void ToggleRightSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (RightSidebarColumn.Width.Value > 0)
            {
                RightSidebarColumn.Width = new GridLength(0);
            }
            else
            {
                RightSidebarColumn.Width = new GridLength(300);
            }
        }

        private void SpawnEditedToken_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is MainViewModel vm)) return;

            var edited = vm.SelectedToken;
            if (edited == null) return;

            var placed = new Token()
            {
                Name = edited.Name,
                ArmorClass = edited.ArmorClass,
                HP = edited.HP,
                InitiativeModifier = edited.InitiativeModifier,
                Speed = edited.Speed,
                Image = edited.Image,
                IsPlayer = edited.IsPlayer,
                SizeInSquares = edited.SizeInSquares
            };

            var centerScreen = new System.Windows.Point(BattleGrid.ActualWidth / 2.0, BattleGrid.ActualHeight / 2.0);
            var world = BattleGrid.ScreenToWorldPublic(centerScreen);
            int gx = (int)Math.Floor(world.X / BattleGrid.GridCellSize);
            int gy = (int)Math.Floor(world.Y / BattleGrid.GridCellSize);
            placed.GridX = gx; placed.GridY = gy;

            vm.Tokens.Add(placed);
            UndoManager.Record(new TokenAddAction(vm, placed), performNow: true);
            UpdateStatus();
        }

        private void SaveEditedToBank_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is MainViewModel vm)) return;
            var edited = vm.SelectedToken;
            if (edited == null) return;

            if (vm.SelectedBankItem != null)
            {
                var idx = vm.CreatureBank.IndexOf(vm.SelectedBankItem);
                var proto = new Token()
                {
                    Name = edited.Name,
                    ArmorClass = edited.ArmorClass,
                    HP = edited.HP,
                    InitiativeModifier = edited.InitiativeModifier,
                    Speed = edited.Speed,
                    Image = edited.Image,
                    IsPlayer = edited.IsPlayer,
                    SizeInSquares = edited.SizeInSquares,
                    Actions = edited.Actions != null ? new List<Models.Combat.Action>(edited.Actions) : new List<Models.Combat.Action>()
                };
                if (idx >= 0) vm.CreatureBank[idx] = proto;
                else vm.CreatureBank.Add(proto);
            }
            else
            {
                var proto = new Token()
                {
                    Name = edited.Name,
                    ArmorClass = edited.ArmorClass,
                    HP = edited.HP,
                    InitiativeModifier = edited.InitiativeModifier,
                    Speed = edited.Speed,
                    Image = edited.Image,
                    IsPlayer = edited.IsPlayer,
                    SizeInSquares = edited.SizeInSquares,
                    Actions = edited.Actions != null ? new List<Models.Combat.Action>(edited.Actions) : new List<Models.Combat.Action>()
                };
                vm.CreatureBank.Add(proto);
            }
            UpdateStatus();
        }

        private void MenuImportSrdPack_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select SRD Pack Directory",
                Filter = "JSON file|*.json|All files|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                var files = dlg.FileNames;
                if (files.Length == 0)
                {
                    System.Windows.MessageBox.Show("No JSON files found in the selected folder.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var file in files)
                {
                    var categoryName = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        var creatures = JsonSerializer.Deserialize<List<Token>>(File.ReadAllText(file));
                        foreach (var creature in creatures)
                        {
                            creature.Extras["Category"] = categoryName;
                            if (!(DataContext is MainViewModel vm)) continue;
                            vm.CreatureBank.Add(creature);
                        }

                        System.Windows.MessageBox.Show($"Loaded {creatures.Count} creatures from '{categoryName}'", "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to load '{categoryName}': {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void MenuImportDatabase_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.Encounters.DatabaseImportWindow()
            {
                Owner = this
            };
            window.ShowDialog();

            ReloadCreatureBankFromDatabase();
        }

        private async void MenuReloadDatabase_Click(object sender, RoutedEventArgs e)
        {
            await ReloadCreatureBankFromDatabase();
            MessageBox.Show($"Creature bank reloaded from database.", "Reload Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void MenuDatabaseStats_Click(object sender, RoutedEventArgs e)
        {
            using var db = new Services.Creatures.CreatureDatabaseService();
            var count = await db.GetCreatureCountAsync();
            var categories = await db.GetCategoriesAsync();
            var types = await db.GetAllTypesAsync();
            var tags = await db.GetAllTagsAsync();

            var stats = $"Database Statistics:\n\n" +
                        $"Total Creatures: {count}" +
                        $"Categories: {categories.Count - 1}\n" +
                        $"Types: {types.Count - 1}" +
                        $"Tags: {tags.Count}";

            MessageBox.Show(stats, "Database Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e) =>
            Close();
        
        private void MenuImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON file|*.json|All files|*.*" };
            if (dlg.ShowDialog() != true) return;

            List<Token> imported;
            try
            {
                imported = ImportService.ImportTokensFromJsonFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to import file: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (DataContext is MainViewModel vm)
            {
                foreach (var t in imported)
                {
                    t.GridX = 0; t.GridY = 0;
                    vm.CreatureBank.Add(t);
                }
            }
        }

        private void MenuOptions_Click(object sender, RoutedEventArgs e)
        {
            var win = new OptionsWindow { Owner = this };
            if (win.ShowDialog() == true)
            {
                BattleGrid.GridCellSize = Options.DefaultGridCellSize;
                BattleGrid.SetGridMaxSize(Options.GridMaxWidth, Options.GridMaxHeight);
                BattleGrid.UpdateShadowSoftness();
                Services.Persistence.OptionsService.SaveOptions();
            }
        }

        private void MenuGridSettings_Click(object sender, RoutedEventArgs e)
        {
            // Will make its own dialog eventually, for now sends all info and user to same window.
            MenuOptions_Click(sender, e);
        }

        private void MenuToggleLiveMode_Click(object sender, RoutedEventArgs e)
        {
            Options.LiveMode = !Options.LiveMode;

            if (Options.LiveMode) Options.AutoResolveAOOs = false;
        }

        private async void CommitMove_Click(object sender, RoutedEventArgs e)
        {
            if (BattleGrid != null)
                await BattleGrid.CommitPreviewedPathAsync();
        }

        private void MenuExportBank_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is MainViewModel vm)) return;

            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "Creature Bank JSON|*.json",
                FileName = "creature_bank.json"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                TokenExportService.ExportTokensToJson(dlg.FileName, vm.CreatureBank);
                System.Windows.MessageBox.Show("Creature bank exported.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export creature bank: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Undo_Click(object sender, RoutedEventArgs e) => UndoManager.Undo();

        private void Redo_Click(object sender, RoutedEventArgs e) => UndoManager.Redo();

        private void MenuSaveEncounter_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Encounter JSON|*.json" };
            if (dlg.ShowDialog() != true) return;
            var dto = BattleGrid.GetEncounterDto();
            try
            {
                EncounterService.SaveEncounterToFile(dto, dlg.FileName);
                System.Windows.MessageBox.Show("Saved encounter.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuLoadEncounter_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Encounter Files (*.encounter)|*.encounter|Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Load Encounter or Tile Map"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string extension = System.IO.Path.GetExtension(dialog.FileName).ToLower();

                    if (extension == ".json")
                    {
                        // Load as tile map
                        var mapService = new Services.TileService.TileMapService();
                        var tileMap = mapService.LoadMapAsync(dialog.FileName).Result;

                        if (tileMap != null)
                        {
                            BattleGrid.LoadTileMap(tileMap);
                            MessageBox.Show(
                                $"Loaded tile map: {tileMap.Name}\nSize: {tileMap.Width}×{tileMap.Height}",
                                "Tile Map Loaded",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                    else if (extension == ".encounter")
                    {
                        // Load as encounter (old format)
                        string json = System.IO.File.ReadAllText(dialog.FileName);
                        var dto = System.Text.Json.JsonSerializer.Deserialize<EncounterDto>(json);

                        if (dto != null)
                        {
                            BattleGrid.LoadEncounterDto(dto);
                            MessageBox.Show(
                                $"Encounter loaded successfully!\n\nTokens: {dto.Tokens.Count}\nWalls: {dto.Walls.Count}",
                                "Encounter Loaded",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error loading file:\n\n{ex.Message}",
                        "Load Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void MenuEncounterTemplates_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var window = new Views.Encounters.EncounterTemplateWindow(vm)
                {
                    Owner = this
                };
                window.ShowDialog();
            }
        }

        private void ToggleMeasure_Click(object sender, RoutedEventArgs e)
        {
            bool newState = !BattleGrid.IsMeasureMode;
            BattleGrid.SetMeasureMode(newState);

            if (sender is Button btn)
            {
                btn.Background = newState
                    ? new SolidColorBrush(Color.FromRgb(14, 99, 156))
                    : new SolidColorBrush(Color.FromRgb(45, 45, 48));
            }

            Status_Mode.Text = newState ? "Mode: Measure" : "Mode: Normal";
        }

        private void DrawSolidWall_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.SetWallDrawMode(true, WallType.Solid);
        }

        private void DrawRoom_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.SetRoomDrawMode(true, WallType.Solid);
            Status_Mode.Text = "Mode: Draw Room";
        }

        private void DrawDoor_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.SetWallDrawMode(true, WallType.Door);
        }

        private void DrawRoomWithDoors_Click(object sender, RoutedEventArgs e)
        {
            // First wall will be a door, rest solid
            BattleGrid.SetRoomDrawMode(true, WallType.Solid);
            Status_Mode.Text = "Mode: Draw Room";
        }

        private void DrawWindow_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.SetWallDrawMode(true, WallType.Window);
        }

        private void DrawHalfWall_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.SetWallDrawMode(true, WallType.Halfwall);
        }

        private void StopWallDraw_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.SetWallDrawMode(false);
            BattleGrid.SetRoomDrawMode(false);
            Status_Mode.Text = "Mode: Normal";
        }

        private void ClearAllWalls_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Delete all wall from the map?",
                "Clear Walls",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                BattleGrid.WallService.Clear();
            }
        }

        private void AddLight_Click(object sender, RoutedEventArgs e)
        {
            if (BattleGrid == null) return;

            Token target = (DataContext as MainViewModel)?.SelectedToken;
            double cx, cy;
            if (target != null)
            {
                cx = target.GridX; cy = target.GridY;
            }
            else
            {
                // center of viewport -> convert using public wrapper
                var centerScreen = new System.Windows.Point(BattleGrid.ActualWidth / 2.0, BattleGrid.ActualHeight / 2.0);
                var world = BattleGrid.ScreenToWorldPublic(centerScreen);
                cx = Math.Floor(world.X / BattleGrid.GridCellSize);
                cy = Math.Floor(world.Y / BattleGrid.GridCellSize);
            }

            var light = new LightSource { CenterGrid = new System.Windows.Point(cx, cy), BrightRadius = 4, DimRadius = 8, Intensity = 1.0 };
            BattleGrid.AddLight(light);
        }

        // Add these methods to your MainWindow class

        /// <summary>
        /// Opens the tile map editor window
        /// </summary>
        private void OpenTileMapEditor_Click(object sender, RoutedEventArgs e)
        {
            var editorWindow = new Views.TileMap.TileMapEditorWindow();
            editorWindow.Owner = this;
            editorWindow.Show();
        }

        /// <summary>
        /// Load a tile map into the battle grid
        /// </summary>
        private async void LoadTileMap_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Tile Map Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Load Tile Map",
                InitialDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "DnDBattle", "Maps")
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Show loading indicator
                    /*BusyIndicator.Visibility = Visibility.Visible; // Add this to your XAML
                    BusyIndicator.BusyContent = "Loading tile map...";*/

                    // Load on background thread
                    var mapService = new Services.TileService.TileMapService();
                    var tileMap = await mapService.LoadMapAsync(dialog.FileName);

                    if (tileMap != null)
                    {
                        // Back on UI thread - now load into grid
                        BattleGrid.LoadTileMap(tileMap);

                        MessageBox.Show(
                            $"Loaded tile map: {tileMap.Name}\nSize: {tileMap.Width}×{tileMap.Height}\nTiles: {tileMap.PlacedTiles.Count}",
                            "Tile Map Loaded",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Failed to load tile map.",
                            "Load Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error loading tile map:\n\n{ex.Message}",
                        "Load Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    //BusyIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Clear the currently loaded tile map
        /// </summary>
        private void ClearTileMap_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Clear the current tile map?",
                "Clear Tile Map",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                BattleGrid.LoadTileMap(null);
                MessageBox.Show(
                    "Tile map cleared.",
                    "Cleared",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async Task ReloadCreatureBankFromDatabase()
        {
            if (!(DataContext is MainViewModel vm)) return;

            using var db = new Services.Creatures.CreatureDatabaseService();
            var creatures = await db.SearchCreaturesAsync(sortBy: "Name");

            vm.CreatureBank.Clear();
            foreach (var creature in creatures)
            {
                vm.CreatureBank.Add(creature);
            }
        }

        private void BtnCombatStats_Click(object sender, RoutedEventArgs e)
        {
            var panel = new CombatStatisticsPanel();
            panel.SetStatsService(_combatStatsService);

            var host = new Window
            {
                Title = "Combat Statistics",
                Content = panel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 400,
                Height = 500
            };

            host.Show();
        }

        // Add method to open dice history window
        private void BtnDiceHistory_Click(object sender, RoutedEventArgs e)
        {
            var panel = new DiceHistoryPanel();
            panel.SetHistoryService(_diceHistoryService);

            var host = new Window
            {
                Title = "Dice Roll History",
                Content = panel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 450,
                Height = 550
            };

            host.Show();
        }

        public void UpdatePendingSpawnsDisplay(int count)
        {
            TxtPendingSpawns.Text = count.ToString();

            if (count > 0)
            {
                TxtPendingSpawns.Foreground = (Brush)Application.Current.Resources["Brush_Warning"];
            }
            else
            {
                TxtPendingSpawns.Foreground = (Brush)Application.Current.Resources["Brush_Text_Secondary"];
            }
        }

        #region Fog of war handlers

        private void EnableFog_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = MenuFogEnabled.IsChecked;
            var mode = MenuFogExploration.IsChecked ? FogMode.Exploration : FogMode.Dynamic;
            BattleGrid.SetFogOfWar(enabled, mode);
        }

        private void FogExploration_Click(object sender, RoutedEventArgs e)
        {
            MenuFogExploration.IsChecked = true;
            MenuFogDynamic.IsChecked = false;

            if (MenuFogEnabled.IsChecked)
            {
                BattleGrid.SetFogOfWar(true, FogMode.Exploration);
            }
        }

        private void FogDynamic_Click(object sender, RoutedEventArgs e)
        {
            MenuFogExploration.IsChecked = false;
            MenuFogDynamic.IsChecked = true;

            if (MenuFogEnabled.IsChecked)
            {
                BattleGrid.SetFogOfWar(true, FogMode.Dynamic);
            }
        }

        private void RevealAllFog_Click(object sender, RoutedEventArgs e)
        {
            BattleGrid.RevealAllFog();
        }

        private void ResetFog_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Reset all fog of war? This will hide everything again.",
                "Reset Fog",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                BattleGrid.ResetFog();
            }
        }

        #endregion

        #region Encounter Builder

        private void BtnEncounterBuilder_Click(object sender, RoutedEventArgs e)
        {
            var builder = new EncounterBuilderWindow();

            // Wire up deploy event
            builder.DeployRequested += OnEncounterDeploy;

            var host = new Window()
            {
                Title = "Encounter Builder",
                Content = builder,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 900,
                Height = 650,
                MinWidth = 800,
                MinHeight = 500
            };

            host.ShowDialog();
        }

        private void OnEncounterDeploy(List<Token> creatures)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                int gridX = 2;
                int gridY = 2;
                int spacing = 2;
                int creaturesPerRow = 5;
                int index = 0;

                foreach (var creature in creatures)
                {
                    // Create a copy of the creature
                    var newToken = CreateTokenFromCreature(creature);

                    // Position in a grid pattern
                    newToken.GridX = gridX + (index % creaturesPerRow) * spacing;
                    newToken.GridY = gridY + (index / creaturesPerRow) * spacing;

                    vm.Tokens.Add(newToken);
                    index++;
                }

                vm.ActionLog.Insert(0, new ActionLogEntry
                {
                    Timestamp = DateTime.Now,
                    Source = "Encounter",
                    Message = $"⚔️ Deployed {creatures.Count} creatures from encounter builder"
                });
            }
        }

        // If you don't already have this method, add it:
        private Token CreateTokenFromCreature(Token source)
        {
            return new Token
            {
                Id = Guid.NewGuid(),
                Name = source.Name,
                Size = source.Size,
                Type = source.Type,
                Alignment = source.Alignment,
                ChallengeRating = source.ChallengeRating,
                Image = source.Image,
                IconPath = source.IconPath,
                HP = source.MaxHP,
                MaxHP = source.MaxHP,
                HitDice = source.HitDice,
                ArmorClass = source.ArmorClass,
                InitiativeModifier = source.InitiativeModifier,
                IsPlayer = source.IsPlayer,
                Speed = source.Speed,
                SizeInSquares = source.SizeInSquares > 0 ? source.SizeInSquares : 1,
                Str = source.Str,
                Dex = source.Dex,
                Con = source.Con,
                Int = source.Int,
                Wis = source.Wis,
                Cha = source.Cha,
                Skills = source.Skills?.ToList() ?? new List<string>(),
                Senses = source.Senses,
                Languages = source.Languages,
                Immunities = source.Immunities,
                Resistances = source.Resistances,
                Vulnerabilities = source.Vulnerabilities,
                Traits = source.Traits,
                Notes = source.Notes,
                Actions = source.Actions?.Select(a => new Models.Combat.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Combat.Action>(),
                BonusActions = source.BonusActions?.Select(a => new Models.Combat.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Combat.Action>(),
                Reactions = source.Reactions?.Select(a => new Models.Combat.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Combat.Action>(),
                LegendaryActions = source.LegendaryActions?.Select(a => new Models.Combat.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Combat.Action>(),
                Tags = source.Tags?.ToList() ?? new List<string>()
            };
        }

        #endregion

        #region Helpers

        private void AutosaveNow()
        {
            try
            {
                if (DataContext is MainViewModel vm)
                    AutosaveService.SaveEncounter(vm, BattleGrid);
            }
            catch { }
        }

        #endregion
    }
}