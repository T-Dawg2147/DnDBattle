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
        public ObservableCollection<Token> Tokens { get; } = new ObservableCollection<Token>();
        public ObservableCollection<Token> CreatureBank { get; } = new ObservableCollection<Token>();
        public ObservableCollection<Obstacle> Obstacles { get; } = new ObservableCollection<Obstacle>();
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

        #endregion

        public string SearchText { get; set; } = "";
        public string SelectedCategory { get; set; } = "All";

        #region Commands
        public IRelayCommand AddTokenCommand { get; }
        public IRelayCommand LoadMapCommand { get; }
        public IRelayCommand RollDiceCommand { get; }

        public IRelayCommand RollAllInitiativeCommand { get; }
        public IRelayCommand NextTurnCommand { get; }

        public IRelayCommand ToggleRulerCommand { get; }
        public IRelayCommand AddObstacleCommand { get; }
        public IRelayCommand AddLightCommand { get; }

        public IRelayCommand OpenCreatureBrowserCommand { get; }

        #endregion

        private InitiativeManager _initiativeManager;

        public ObservableCollection<Token> InitiativeOrder => 
            new ObservableCollection<Token>(Tokens.OrderByDescending(t => t.Initiative).ToList());

        public MainViewModel()
        {
            AddTokenCommand = new RelayCommand(AddToken);
            LoadMapCommand = new RelayCommand(LoadMap);
            RollDiceCommand = new RelayCommand(RollDice);

            RollAllInitiativeCommand = new RelayCommand(RollAllInitiative);
            NextTurnCommand = new RelayCommand(NextTurn);

            ToggleRulerCommand = new RelayCommand(ToggleRuler);
            AddObstacleCommand = new RelayCommand(AddSampleObstacle);
            AddLightCommand = new RelayCommand(AddSampleLight);

            OpenCreatureBrowserCommand = new RelayCommand(OpenCreatureBrowser);

            _initiativeManager = new InitiativeManager(Tokens);
        }

        private void AddToken()
        {
            var t = new Token { Name = "New Token", GridX = 0, GridY = 0, HP = 10 };
            Tokens.Add(t);
            SelectedToken = t;
            Log("System", $"Added token {t.Name}");
            OnPropertyChanged(nameof(InitiativeOrder));
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

        private void RollAllInitiative()
        {
            // Test default dice set to 1d20.
            var rolls = _initiativeManager.RollAll();
            foreach (var r in rolls)
            {
                Log("Initiative", $"{r.Token.Name} rolled {r.Roll} + {r.Modifier} => {r.Total}");
            }

            OnPropertyChanged(nameof(InitiativeOrder));
        }

        private void NextTurn()
        {
            _initiativeManager.NextTurn();
            var ct = _initiativeManager.CurrentToken;
            if (ct != null)
            {
                SelectedToken = ct;
                Log("Turn", $"Now {ct.Name}'s turn (Initiative {ct.Initiative})");
            }
        }

        private bool _rulerEnabled = false;
        public bool RulerEnabled { get => _rulerEnabled; set => SetProperty(ref _rulerEnabled, value); }

        private void ToggleRuler()
        {
            RulerEnabled = !RulerEnabled;
            Log("System", $"Ruler {(RulerEnabled ? "enabled" : "disabled")}");
        }

        private void AddSampleObstacle()
        {
            var obs = new Obstacle();
            obs.Label = "Wall";
            obs.PolygonGridPoints.Add(new System.Windows.Point(6, 4));
            obs.PolygonGridPoints.Add(new System.Windows.Point(9, 4));
            obs.PolygonGridPoints.Add(new System.Windows.Point(9, 5));
            obs.PolygonGridPoints.Add(new System.Windows.Point(6, 5));
            Obstacles.Add(obs);
            Log("System", "Added sample obstacle (wall)");
        }

        private void AddSampleLight()
        {
            var l = new LightSource { CenterGrid = new System.Windows.Point(5, 5), RadiusSquares = 6, Intensity = 1.0 };
            Lights.Add(l);
            Log("System", "Added sample light at (5,5)");
        }

        private void OpenCreatureBrowser()
        {
            var host = new Window()
            {
                Title = "Add Creature",
                Content = new CreatureBrowserWindow { DataContext = this },
                Owner = Application.Current?.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 900,
                Height = 640
            };
            host.ShowDialog();
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

        private void Log(string source, string message)
        {
            ActionLog.Insert(0, new ActionLogEntry { Source = source, Message = message, Timestamp = DateTime.Now });
        }
    }
}
