using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.ViewModels;
using DnDBattle.Views;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DnDBattle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _autosaveTimer;
        private const string CreatureBankPath = "CreatureBank.json";

        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;

            Services.OptionsService.LoadOptions();
            UndoManager.Limit = Options.UndoStackLimit;
            UndoManager.StateChanged += UndoManager_StateChanged;

            vm.Tokens.CollectionChanged += (s, e) => AutosaveNow();

            _autosaveTimer = new DispatcherTimer();
            _autosaveTimer.Interval = TimeSpan.FromSeconds(Options.AutosaveIntervalSeconds);
            _autosaveTimer.Tick += (s, e) => AutosaveNow();
            if (Options.EnabledPeriodicAutosave)
                _autosaveTimer.Start();

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
            if (BattleGrid == null) return;
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
            var categories = await db.GetAllCategoriesAsync();
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
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Encounter JSON|*.json" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var dto = EncounterService.LoadEncounterFromFile(dlg.FileName);
                BattleGrid.LoadEncounterDto(dto);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void ToggleDrawObstacle_Checked(object sender, RoutedEventArgs e)
        {
            BattleGrid?.SetObstacleDrawMode(true);
        }

        private void ToggleDrawObstacle_Unchecked(object sender, RoutedEventArgs e)
        {
            BattleGrid?.SetObstacleDrawMode(false);
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

        private void AutosaveNow()
        {
            try
            {
                if (DataContext is MainViewModel vm)
                    AutosaveService.SaveEncounter(vm, BattleGrid);
            }
            catch { }
        }

        private void AddSampleObstacle_Click(object sender, RoutedEventArgs e)
        {
            var obs = new Obstacle
            {
                Label = "SampleWall",
                PolygonGridPoints = new System.Collections.Generic.List<System.Windows.Point>
                {
                    new System.Windows.Point(6,6),
                    new System.Windows.Point(9,6),
                    new System.Windows.Point(9,9),
                    new System.Windows.Point(6,9)
                }
            };
            BattleGrid?.AddObstacle(obs);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (DataContext is MainViewModel vm)
            {
                try
                {
                    var json = JsonSerializer.Serialize(vm.CreatureBank, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(CreatureBankPath, json);
                    Debug.WriteLine($"Saved CreatureBank to {CreatureBankPath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to save CreatureBank: {ex.Message}");
                }
            }
        }
    }
}