using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.Services.FogOfWar;
using DnDBattle.ViewModels;
using DnDBattle.Views;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

        // Dockable panel child controls
        private readonly InitiativeTrackerPanel InitiativeTracker;
        private readonly ActionLogPanel ActionLogPanel;
        private readonly SelectedTokenPanel SelectedTokenPanel;

        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;

            // Create panel child controls and assign to dockable panels
            InitiativeTracker = new InitiativeTrackerPanel();
            InitiativeTrackerDock.PanelChild = InitiativeTracker;

            ActionLogPanel = new ActionLogPanel();
            ActionLogDock.PanelChild = ActionLogPanel;

            SelectedTokenPanel = new SelectedTokenPanel();
            SelectedTokenDock.PanelChild = SelectedTokenPanel;

            Services.OptionsService.LoadOptions();
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

            SetupActionLogPanel();

            BattleGrid.TokenAddedToMap += (token) =>
            {
                InitiativeTracker.AddToken(token);
            };

            BattleGrid.TokenDoubleClicked += OnTokenDoubleClicked;

            Loaded += MainWindow_Loaded;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            this.Closing += (s, e) => Services.OptionsService.SaveOptions();
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

        private void SetupActionLogPanel()
        {
            if (DataContext is MainViewModel vm)
            {
                ActionLogPanel.SetActionLog(vm.ActionLog);
            }
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
                    Actions = edited.Actions != null ? new List<Models.Action>(edited.Actions) : new List<Models.Action>()
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
                    Actions = edited.Actions != null ? new List<Models.Action>(edited.Actions) : new List<Models.Action>()
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
            var window = new Views.DatabaseImportWindow()
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
            using var db = new Services.CreatureDatabaseService();
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
                Services.OptionsService.SaveOptions();
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
                var window = new Views.EncounterTemplateWindow(vm)
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

            var light = new LightSource { CenterGrid = new System.Windows.Point(cx, cy), RadiusSquares = 8, Intensity = 1.0 };
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

            using var db = new Services.CreatureDatabaseService();
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
                Actions = source.Actions?.Select(a => new Models.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Action>(),
                BonusActions = source.BonusActions?.Select(a => new Models.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Action>(),
                Reactions = source.Reactions?.Select(a => new Models.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Action>(),
                LegendaryActions = source.LegendaryActions?.Select(a => new Models.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Action>(),
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