using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.Utils;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Data;
using System.Collections.Generic;
using DnDBattle.Views;

namespace DnDBattle.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public event System.Action RequestTokenVisualsRefresh;

        public ObservableCollection<Token> Tokens { get; } = new ObservableCollection<Token>();
        public ObservableCollection<Token> CreatureBank { get; } = new ObservableCollection<Token>();
        public ObservableCollection<LightSource> Lights { get; } = new ObservableCollection<LightSource>();
        public ObservableCollection<ActionLogEntry> ActionLog { get; } = new ObservableCollection<ActionLogEntry>();
        

        public ObservableCollection<string> DiceHistory { get; } = new ObservableCollection<string>();

        #region Properties

        private Token _selectedToken;
        public Token SelectedToken
        {
            get => _selectedToken;
            set => SetProperty(ref _selectedToken, value);
        }

        private Token _selectedBankItem;
        public Token SelectedBankItem
        {
            get => _selectedBankItem;
            set => SetProperty(ref _selectedBankItem, value);
        }

        private double _gridCellSize = 48;
        public double GridCellSize
        {
            get => _gridCellSize;
            set => SetProperty(ref _gridCellSize, value);
        }

        private string _diceExpression = "1d20+0";
        public string DiceExpression
        {
            get => _diceExpression;
            set => SetProperty(ref _diceExpression, value);
        }

        private ImageSource _mapImage;
        public ImageSource MapImageSource
        {
            get => _mapImage;
            set => SetProperty(ref _mapImage, value);
        }

        private Token _currentTurnToken;
        public Token CurrentTurnToken
        {
            get => _currentTurnToken;
            set => SetProperty(ref _currentTurnToken, value);
        }

        private ICollectionView _creatureBankView;
        public ICollectionView CreatureBankView
        {
            get
            {
                if (_creatureBankView == null)
                {
                    _creatureBankView = CollectionViewSource.GetDefaultView(CreatureBank);
                    _creatureBankView.Filter = new Predicate<object>(FilterCreatureBank);
                }
                return _creatureBankView;
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private string _loadingMessage;
        public string LoadingMessage
        {
            get => _loadingMessage;
            set
            {
                _loadingMessage = value;
                OnPropertyChanged(nameof(LoadingMessage));
            }
        }



        #endregion

        #region Combat State

        private bool _isInCombat;
        public bool IsInCombat
        {
            get => _isInCombat;
            set
            {
                if (SetProperty(ref _isInCombat, value))
                {
                    OnPropertyChanged(nameof(CombatButtonText));
                    OnPropertyChanged(nameof(CanStartCombat));

                    (NextTurnCommand as RelayCommand)?.NotifyCanExecuteChanged();
                    (PreviousTurnCommand as RelayCommand)?.NotifyCanExecuteChanged();
                    (ToggleCombatCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        private int _currentRound;
        public int CurrentRound
        {
            get => _currentRound;
            set
            {
                _currentRound = value;
                OnPropertyChanged(nameof(CurrentRound));
            }
        }

        private int _currentTurnIndex;
        public int CurrentTurnIndex
        {
            get => _currentTurnIndex;
            set
            {
                _currentTurnIndex = value;
                OnPropertyChanged(nameof(CurrentTurnIndex));
            }
        }

        public string CombatButtonText => IsInCombat ? "End Combat" : "Start Combat";
        public bool CanStartCombat => Tokens.Count > 0;

        private ObservableCollection<Token> _initativeOrder = new ObservableCollection<Token>();
        public ObservableCollection<Token> InitiativeOrderList
        {
            get => _initativeOrder;
            set
            {
                SetProperty(ref _initativeOrder, value);
            }
        }

        public ObservableCollection<Token> InitiativeOrder => InitiativeOrderList;

        #endregion

        private CreatureDatabaseService _dbService;
        public string SearchText { get; set; } = "";
        public string SelectedCategory { get; set; } = "All";

        #region Commands
        public IRelayCommand AddTokenCommand { get; }
        public IRelayCommand LoadMapCommand { get; }
        public IRelayCommand RollDiceCommand { get; }

        public IRelayCommand RollAllInitiativeCommand { get; }

        public IRelayCommand ToggleRulerCommand { get; }
        public IRelayCommand AddLightCommand { get; }

        public IRelayCommand OpenCreatureBrowserCommand { get; }

        #endregion

        #region Combat Commands

        public IRelayCommand StartCombatCommand { get; }
        public IRelayCommand EndCombatCommand { get; }
        public IRelayCommand ToggleCombatCommand { get; }
        public IRelayCommand NextTurnCommand { get; }
        public IRelayCommand PreviousTurnCommand { get; }
        public IRelayCommand RerollInitiativeCommand { get; }

        #endregion

        private InitiativeManager _initiativeManager;

        public MainViewModel()
        {
            _dbService = new CreatureDatabaseService();

            LoadMapCommand = new RelayCommand(LoadMap);
            RollDiceCommand = new RelayCommand(RollDice);
            ToggleRulerCommand = new RelayCommand(ToggleRuler);
            AddLightCommand = new RelayCommand(AddSampleLight);
            OpenCreatureBrowserCommand = new RelayCommand(OpenCreatureBrowser);

            // Combat Commands
            ToggleCombatCommand = new RelayCommand(ToggleCombat);
            StartCombatCommand = new RelayCommand(StartCombat);
            EndCombatCommand = new RelayCommand(EndCombat);
            NextTurnCommand = new RelayCommand(NextTurn);
            PreviousTurnCommand = new RelayCommand(PreviousTurn);
            RollAllInitiativeCommand = new RelayCommand(RollAllInitiative);

            _initiativeManager = new InitiativeManager(Tokens);

            Tokens.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanStartCombat));
                if (IsInCombat)
                {
                    RefreshInitiativeOrder();
                }
            };
        }

        public async Task LoadCreaturesFromDatabaseAsync()
        {
            IsLoading = true;
            LoadingMessage = "Connecting to database...";

            try
            {
                await Task.Run(async () =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        LoadingMessage = "Loading creatures from database";
                    });

                    var creatures = await _dbService.SearchCreaturesAsync(sortBy: "Name", limit: 10000);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        LoadingMessage = $"Adding {creatures.Count} creatures...";

                        CreatureBank.Clear();
                        foreach (var creature in creatures)
                        {
                            CreatureBank.Add(creature);
                        }
                    });
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    RefreshCreatureBankView();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading creatures: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadMap()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp";
            if (dlg.ShowDialog() == true)
            {
                MapImageSource = new BitmapImage(new System.Uri(dlg.FileName));
                Log("System", $"Loaded map {dlg.SafeFileName}");
            }
        }

        private void RollDice()
        {
            var result = DiceRoller.RollExpression(DiceExpression);
            DiceHistory.Insert(0, $"{DiceExpression} -> {result.Total} ({string.Join(", ", result.Individual)})");
            Log("Dice", $"{DiceExpression} -> {result.Total} ({string.Join(", ", result.Individual)})");
        }

        #region Combat Methods

        private void ToggleCombat()
        {
            if (IsInCombat)
                EndCombat();
            else
                StartCombat();
        }

        private void StartCombat()
        {
            if (Tokens.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Add some creatures to the battle map before starting combat!",
                    "No Creatures",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            Log("Combat", "⚔️ Combat Started!");

            foreach (var token in Tokens)
            {
                token.ResetMovementForNewTurn();
            }

            // Roll initiative for all tokens
            foreach (var token in Tokens)
            {
                var roll = DiceRoller.RollExpression("1d20");
                token.Initiative = roll.Total + token.InitiativeModifier;
                Log("Initiative", $"  {token.Name}:  {roll.Total} + {token.InitiativeModifier} = {token.Initiative}");
            }

            // Sort and set up initiative order
            RefreshInitiativeOrder();

            // Set combat state AFTER rolling
            CurrentRound = 1;
            CurrentTurnIndex = 0;
            IsInCombat = true;  // This triggers property changed notifications

            Log("Combat", $"Round {CurrentRound} begins!");

            // Set first turn
            if (InitiativeOrderList.Count > 0)
            {
                // Clear all turn markers first
                foreach (var t in Tokens)
                {
                    t.IsCurrentTurn = false;
                }

                var firstToken = InitiativeOrderList[0];
                firstToken.IsCurrentTurn = true;
                CurrentTurnToken = firstToken;
                SelectedToken = firstToken;
                Log("Turn", $"🎯 {firstToken.Name}'s turn (Initiative: {firstToken.Initiative})");
            }

            // Force UI refresh
            OnPropertyChanged(nameof(InitiativeOrderList));
            OnPropertyChanged(nameof(InitiativeOrderList));
            OnPropertyChanged(nameof(CombatButtonText));

            // Request battle grid to refresh token visuals
            RequestTokenVisualsRefresh?.Invoke();
        }

        private void EndCombat()
        {
            Log("Combat", "🏁 Combat Ended!");

            // Clear turn indicators
            foreach (var token in Tokens)
            {
                token.IsCurrentTurn = false;
            }

            CurrentTurnToken = null;
            CurrentRound = 0;
            CurrentTurnIndex = 0;
            IsInCombat = false;  // This triggers property changed

            OnPropertyChanged(nameof(InitiativeOrder));
            OnPropertyChanged(nameof(InitiativeOrderList));
            OnPropertyChanged(nameof(CombatButtonText));

            RequestTokenVisualsRefresh?.Invoke();
        }

        private void RollAllInitiative()
        {
            Log("Initiative", "🎲 Rolling initiative for all creatures...");

            foreach (var token in Tokens)
            {
                var roll = DiceRoller.RollExpression("1d20");
                token.Initiative = roll.Total + token.InitiativeModifier;

                Log("Initiatie", $"  {token.Name}:  {roll.Total} + {token.InitiativeModifier} = {token.Initiative}");
            }

            RefreshInitiativeOrder();
            
            OnPropertyChanged(nameof(InitiativeOrderList));
            OnPropertyChanged(nameof(InitiativeOrder));
        }

        private void RefreshInitiativeOrder()
        {
            var sorted = Tokens
                .OrderByDescending(t => t.Initiative)
                .ThenByDescending(t => t.InitiativeModifier)
                .ThenBy(t => t.Name)
                .ToList();

            InitiativeOrderList.Clear();
            foreach (var token in sorted)
            {
                InitiativeOrderList.Add(token);
            }

            OnPropertyChanged(nameof(InitiativeOrderList));
            OnPropertyChanged(nameof(InitiativeOrder));

            RequestTokenVisualsRefresh?.Invoke();
        }

        private void NextTurn()
        {
            if (!IsInCombat || InitiativeOrderList.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("NextTurn: Not in combat or no tokens");
                return;
            }

            // Clear current turn marker from previous token
            foreach (var t in Tokens)
            {
                t.IsCurrentTurn = false;
            }

            // Advance turn
            CurrentTurnIndex++;

            // Check if we've gone through everyone - new round! 
            if (CurrentTurnIndex >= InitiativeOrderList.Count)
            {
                CurrentTurnIndex = 0;
                CurrentRound++;
                Log("Combat", $"⚔️ Round {CurrentRound} begins!");

                // Reset movement for ALL tokens at the start of a new round
                foreach (var t in Tokens)
                {
                    t.ResetMovementForNewTurn();
                }
            }

            if (CurrentTurnToken != null)
            {
                // Trigger end-of-turn effects for previous token
                var previousToken = GetPreviousToken();
                if (previousToken != null)
                {
                    OnTokenTurnEnd(previousToken);
                }

                // Trigger start-of-turn effects for current token
                OnTokenTurnStart(CurrentTurnToken);
            }

            // Set new current turn
            if (CurrentTurnIndex >= 0 && CurrentTurnIndex < InitiativeOrderList.Count)
            {
                var token = InitiativeOrderList[CurrentTurnIndex];
                token.IsCurrentTurn = true;
                token.ResetMovementForNewTurn(); // Reset this token's movement for their turn
                CurrentTurnToken = token;
                SelectedToken = token;
                Log("Turn", $"🎯 {token.Name}'s turn ({token.SpeedSquares} squares of movement)");
            }

            CheckSpawnTriggersForCurrentRound();
            OnPropertyChanged(nameof(InitiativeOrderList));
            RequestTokenVisualsRefresh?.Invoke();
        }

        private void OnTokenTurnStart(Token token)
        {
            // Notify BattleGrid
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.BattleGrid.OnTokenTurnStart(token);
        }

        private void OnTokenTurnEnd(Token token)
        {
            // Reset movement
            token.MovementUsedThisTurn = 0;

            // Notify BattleGrid
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.BattleGrid.OnTokenTurnEnd(token);
        }

        private void CheckSpawnTriggersForCurrentRound()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.BattleGrid.OnRoundChanged(CurrentRound);
        }

        private void PreviousTurn()
        {
            if (!IsInCombat || InitiativeOrderList.Count == 0) return;

            // Clear current turn marker
            foreach (var t in Tokens)
            {
                t.IsCurrentTurn = false;
            }

            // Go back
            CurrentTurnIndex--;

            // Check if we need to go to previous round
            if (CurrentTurnIndex < 0)
            {
                if (CurrentRound > 1)
                {
                    CurrentRound--;
                    CurrentTurnIndex = InitiativeOrderList.Count - 1;
                    Log("Combat", $"⚔️ Back to Round {CurrentRound}");
                }
                else
                {
                    CurrentTurnIndex = 0;
                }
            }

            // Set new current turn
            if (CurrentTurnIndex >= 0 && CurrentTurnIndex < InitiativeOrderList.Count)
            {
                var token = InitiativeOrderList[CurrentTurnIndex];
                token.IsCurrentTurn = true;
                CurrentTurnToken = token;
                SelectedToken = token;
                Log("Turn", $"🎯 {token.Name}'s turn");
            }

            OnPropertyChanged(nameof(InitiativeOrderList));
            RequestTokenVisualsRefresh?.Invoke();
        }

        #endregion

        private bool _rulerEnabled = false;
        public bool RulerEnabled { get => _rulerEnabled; set => SetProperty(ref _rulerEnabled, value); }

        private void ToggleRuler()
        {
            RulerEnabled = !RulerEnabled;
            Log("System", $"Ruler {(RulerEnabled ? "enabled" : "disabled")}");
        }

        private void AddSampleLight()
        {
            var l = new LightSource { CenterGrid = new System.Windows.Point(5, 5), RadiusSquares = 6, Intensity = 1.0 };
            Lights.Add(l);
            Log("System", "Added sample light at (5,5)");
        }

        private void OpenCreatureBrowser()
        {
            var browser = new CreatureBrowserWindow { DataContext = this };

            // Wire up the event to add creatures to the map!
            browser.CreatureAddedToMap += OnCreatureAddedToMap;

            var host = new Window()
            {
                Title = "Add Creature",
                Content = browser,
                Owner = Application.Current?.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 1100,
                MinWidth = 1100,
                Height = 740,
                MinHeight = 740
            };
            host.ShowDialog();

            // Unsubscribe when closed to prevent memory leaks
            browser.CreatureAddedToMap -= OnCreatureAddedToMap;
        }

        private bool FilterCreatureBank(object obj)
        {
            if (obj is not Token t) return false;

            if (SelectedCategory != "All" && t.Extras.TryGetValue("Category", out var cat))
                if (cat.ToString() != SelectedCategory) return false;

            if (!string.IsNullOrEmpty(SearchText) && !t.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }

        public void RefreshCreatureBankView() => CreatureBankView.Refresh();

        public async Task RefreshCreatureBankViewAsync()
        {
            await LoadCreaturesFromDatabaseAsync();
        }

        #region Helpers

        private void Log(string source, string message)
        {
            ActionLog.Insert(0, new ActionLogEntry { Source = source, Message = message, Timestamp = DateTime.Now });
        }

        private void OnCreatureAddedToMap(Token creature)
        {
            if (creature == null) return;

            // Create a copy of the creature for the map
            var newToken = new Token
            {
                Id = Guid.NewGuid(),
                Name = creature.Name,
                Size = creature.Size,
                Type = creature.Type,
                Alignment = creature.Alignment,
                ChallengeRating = creature.ChallengeRating,
                Image = creature.Image,
                IconPath = creature.IconPath,
                HP = creature.MaxHP,
                MaxHP = creature.MaxHP,
                HitDice = creature.HitDice,
                ArmorClass = creature.ArmorClass,
                InitiativeModifier = creature.InitiativeModifier,
                IsPlayer = creature.IsPlayer,
                Speed = creature.Speed,
                SizeInSquares = creature.SizeInSquares > 0 ? creature.SizeInSquares : 1,

                // Place at a default position (will be adjusted by user)
                GridX = 5,
                GridY = 5,

                // Ability Scores
                Str = creature.Str,
                Dex = creature.Dex,
                Con = creature.Con,
                Int = creature.Int,
                Wis = creature.Wis,
                Cha = creature.Cha,

                // Extra info
                Skills = creature.Skills?.ToList() ?? new List<string>(),
                Senses = creature.Senses,
                Languages = creature.Languages,
                Immunities = creature.Immunities,
                Resistances = creature.Resistances,
                Vulnerabilities = creature.Vulnerabilities,
                Traits = creature.Traits,
                Notes = creature.Notes,

                // ACTIONS - Deep copy all action types
                Actions = creature.Actions?.Select(a => new Models.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Action>(),

                BonusActions = creature.BonusActions?.Select(a => new Models.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Action>(),

                Reactions = creature.Reactions?.Select(a => new Models.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Action>(),

                LegendaryActions = creature.LegendaryActions?.Select(a => new Models.Action
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    DamageExpression = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description,
                    Type = a.Type,
                    Cost = a.Cost
                }).ToList() ?? new List<Models.Action>(),

                Tags = creature.Tags?.ToList() ?? new List<string>()
            };

            // Add to the tokens collection
            Tokens.Add(newToken);
            SelectedToken = newToken;

            // Log the action
            Log("Map", $"➕ Added {newToken.Name} to the battle map");
        }

        #endregion
    }
}
