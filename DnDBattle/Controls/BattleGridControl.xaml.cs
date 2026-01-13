using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace DnDBattle.Controls
{
    public partial class BattleGridControl : UserControl
    {
        public event Action<Token> TokenDoubleClicked;

        public event Action<Token> RequestDeleteToken;
        public event Action<Token> RequestDuplicateToken;
        public event Action<Token> RequestEditToken;
        public event Action<Point> RequestAddTokenAtPosition;

        // Dependency properties (including LockToGrid)
        public static readonly DependencyProperty GridCellSizeProperty =
            DependencyProperty.Register(nameof(GridCellSize), typeof(double), typeof(BattleGridControl), new PropertyMetadata(48.0, OnGridPropertyChanged));

        public static readonly DependencyProperty TokensProperty =
            DependencyProperty.Register(nameof(Tokens), typeof(System.Collections.ObjectModel.ObservableCollection<Token>), typeof(BattleGridControl), new PropertyMetadata(null, OnTokensChanged));

        public static readonly DependencyProperty MapImageSourceProperty =
            DependencyProperty.Register(nameof(MapImageSource), typeof(ImageSource), typeof(BattleGridControl), new PropertyMetadata(null, OnMapImageChanged));

        public static readonly DependencyProperty SelectedTokenProperty =
            DependencyProperty.Register(nameof(SelectedToken), typeof(Token), typeof(BattleGridControl), new PropertyMetadata(null, OnSelectedTokenChanged));

        public static readonly DependencyProperty LockToGridProperty =
            DependencyProperty.Register(nameof(LockToGrid), typeof(bool), typeof(BattleGridControl), new PropertyMetadata(Options.DefaultLockToGrid));

        public double GridCellSize { get => (double)GetValue(GridCellSizeProperty); set => SetValue(GridCellSizeProperty, value); }
        public System.Collections.ObjectModel.ObservableCollection<Token> Tokens { get => (System.Collections.ObjectModel.ObservableCollection<Token>)GetValue(TokensProperty); set => SetValue(TokensProperty, value); }
        public ImageSource MapImageSource { get => (ImageSource)GetValue(MapImageSourceProperty); set => SetValue(MapImageSourceProperty, value); }
        public Token SelectedToken { get => (Token)GetValue(SelectedTokenProperty); set => SetValue(SelectedTokenProperty, value); }
        public bool LockToGrid { get => (bool)GetValue(LockToGridProperty); set => SetValue(LockToGridProperty, value); }

        // internal state
        private readonly SpatialIndex _spatialIndex = new SpatialIndex(1);
        private readonly List<Obstacle> _obstacles = new List<Obstacle>();
        private readonly List<LightSource> _lights = new List<LightSource>();

        private readonly DrawingVisual _movementVisual = new DrawingVisual();
        private readonly DrawingVisual _pathVisual = new DrawingVisual();
        private readonly DrawingVisual _lightingVisual = new DrawingVisual();
        private readonly DrawingVisual _obstacleDrawVisual = new DrawingVisual();

        private readonly WallService _wallService = new WallService();
        private readonly DrawingVisual _wallVisual = new DrawingVisual();

        private TranslateTransform _pan = new TranslateTransform();
        private ScaleTransform _zoom = new ScaleTransform(1, 1);
        private TransformGroup _transformGroup = new TransformGroup();

        private bool _isPanning = false;
        private Point _lastPanPoint;

        // obstacle draw/edit state
        private bool _drawObstacleMode = false;
        private readonly List<Point> _currentDrawVertices = new List<Point>(); // grid coords
        private Obstacle _selectedObstacle = null;
        private int _selectedVertexIndex = -1;
        private bool _isDraggingVertex = false;
        private Point _vertexDragOriginalPos;

        // Wall Drawing State
        private bool _wallDrawMode = false;
        private Point? _wallDrawStart = null;
        private Point _wallDrawPreview;
        private WallType _currentWallType = WallType.Solid;
        private Wall _selectedWall = null;
        private bool _isDraggingWallEndpoint = false;
        private bool _draggingWallIsStart = false;

        public WallService WallService => _wallService;

        // undo / redo stacks for obstacle operations
        private readonly Stack<Models.ObstacleAction> _undoStack = new Stack<Models.ObstacleAction>();
        private readonly Stack<Models.ObstacleAction> _redoStack = new Stack<Models.ObstacleAction>();

        // token drag
        private bool _isDraggingToken = false;
        private FrameworkElement _draggingVisual;
        private Point _dragOrigin;

        // path preview state
        private List<(int x, int y)> _lastPreviewPath = null;
        private HashSet<int> _lastAooIndices = null;

        // visuals sizing
        private int _gridWidth = 200, _gridHeight = 200;
        private int _dragStartGridX = 0;
        private int _dragStartGridY = 0;

        // drop events
        private DateTime _lastDropTime = DateTime.MinValue;
        private string _lastDropPrototypeId = null;
        private readonly TimeSpan _duplicateDropThreshold = TimeSpan.FromMilliseconds(300);

        public BattleGridControl()
        {
            InitializeComponent();

            GridCellSize = Options.DefaultGridCellSize;
            SetGridMaxSize(Options.GridMaxWidth, Options.GridMaxHeight);
            UpdateShadowSoftness();

            _transformGroup.Children.Add(_zoom);
            _transformGroup.Children.Add(_pan);
            RenderCanvas.RenderTransform = _transformGroup;

            // overlays
            AddVisualOverlay(_lightingVisual, 50, makeBlur: true);
            AddVisualOverlay(_movementVisual, 60);
            AddVisualOverlay(_pathVisual, 65);
            AddVisualOverlay(_obstacleDrawVisual, 75);
            AddVisualOverlay(_wallVisual, 70);

            _wallService.WallsChanged += () => RedrawWalls();
            Loaded += BattleGridControl_Loaded;

            // Allow dropping token prototypes from the Creature Bank
            RenderCanvas.AllowDrop = true;
            RenderCanvas.Drop += RenderCanvas_Drop;
        }

        private void BattleGridControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("BattleGridControl loaded. RenderCanvas is " + (RenderCanvas == null ? "NULL" : "OK"));
            UpdateGridVisual();
            RebuildTokenVisuals();
            RedrawLighting();

            if (DataContext is MainViewModel vm)
            {
                vm.RequestTokenVisualsRefresh += () =>
                {
                    Dispatcher.Invoke(() => RebuildTokenVisuals());
                };
            }
        }

        private void AddVisualOverlay(DrawingVisual visual, int zIndex, bool makeBlur = false)
        {
            var wrapper = new FrameworkElementForVisual(visual) { IsHitTestVisible = false, Tag = "Overlay" };
            if (makeBlur)
                wrapper.Effect = new BlurEffect { Radius = Options.ShadowSoftnessPx, RenderingBias = RenderingBias.Quality };
            RenderCanvas.Children.Add(wrapper);
            Canvas.SetZIndex(RenderCanvas.Children[^1], zIndex);
        }

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

            var rollInitItem = new MenuItem { Header = "🎲 Roll Initiative" };
            rollInitItem.Click += (s, e) =>
            {
                var roll = Utils.DiceRoller.RollExpression("1d20");
                token.Initiative = roll.Total + token.InitiativeModifier;
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

        private void InitializeMapContextMenu()
        {
            var mapMenu = new ContextMenu();

            var addCreatureItem = new MenuItem { Header = "➕ Add Creature Here..." };
            addCreatureItem.Click += (s, e) =>
            {
                var position = Mouse.GetPosition(RenderCanvas);
                RequestAddTokenAtPosition?.Invoke(position);
            };
            mapMenu.Items.Add(addCreatureItem);

            var addLightItem = new MenuItem { Header = "💡 Add Light Here..." };
            addLightItem.Click += (s, e) =>
            {
                var position = Mouse.GetPosition(RenderCanvas);
                // Add light at position method?
            };
            mapMenu.Items.Add(addLightItem);

            RenderCanvas.ContextMenu = mapMenu;
        }

        private class FrameworkElementForVisual : FrameworkElement
        {
            private readonly DrawingVisual _visual;
            public FrameworkElementForVisual(DrawingVisual visual) => _visual = visual;
            protected override int VisualChildrenCount => 1;
            protected override Visual GetVisualChild(int index) => _visual;
        }

        #region Dependency callbacks
        private static void OnGridPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (BattleGridControl)d;
            ctrl.UpdateGridVisual();
            ctrl.LayoutTokens();
            ctrl.RedrawLighting();
            ctrl.RedrawMovementOverlay();
            ctrl.RedrawPathVisual();
            ctrl.RedrawObstacleDrawVisual();
        }

        private static void OnMapImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (BattleGridControl)d;
            ctrl.MapImage.Source = (ImageSource)e.NewValue;
            ctrl.MapImage.SetValue(Canvas.ZIndexProperty, -100);
            ctrl.UpdateGridVisual();
        }

        private static void OnTokensChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (BattleGridControl)d;
            if (e.OldValue is INotifyCollectionChanged oldColl) oldColl.CollectionChanged -= ctrl.Tokens_CollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newColl) newColl.CollectionChanged += ctrl.Tokens_CollectionChanged;
            ctrl.RebuildTokenVisuals();
        }

        private static void OnSelectedTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (BattleGridControl)d;
            ctrl.RedrawMovementOverlay();
            ctrl.ClearPathVisual();
        }
        #endregion

        #region Token drag handlers
        private void Token_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _draggingVisual = sender as FrameworkElement;

            if (_draggingVisual != null && _draggingVisual.Tag is Token t)
            {
                _dragStartGridX = t.GridX;
                _dragStartGridY = t.GridY;
            }
            _isDraggingToken = true;
            _dragOrigin = e.GetPosition(RenderCanvas);
            _draggingVisual.CaptureMouse();
            e.Handled = true;
        }

        private void Token_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingToken && _draggingVisual != null)
            {
                var pos = e.GetPosition(RenderCanvas);
                var dx = pos.X - _dragOrigin.X;
                var dy = pos.Y - _dragOrigin.Y;
                var left = Canvas.GetLeft(_draggingVisual) + dx;
                var top = Canvas.GetTop(_draggingVisual) + dy;
                Canvas.SetLeft(_draggingVisual, left);
                Canvas.SetTop(_draggingVisual, top);
                _dragOrigin = pos;
            }
        }

        private void Token_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingToken && _draggingVisual != null)
            {
                _draggingVisual.ReleaseMouseCapture();
                var left = Canvas.GetLeft(_draggingVisual);
                var top = Canvas.GetTop(_draggingVisual);
                int newGridX, newGridY;

                if (LockToGrid)
                {
                    newGridX = (int)Math.Round(left / GridCellSize);
                    newGridY = (int)Math.Round(top / GridCellSize);
                }
                else
                {
                    newGridX = (int)Math.Floor(left / GridCellSize);
                    newGridY = (int)Math.Floor(top / GridCellSize);
                }

                if (_draggingVisual.Tag is Token token)
                {
                    int oldX = _dragStartGridX;
                    int oldY = _dragStartGridY;

                    // Calculate Manhattan distance moved
                    int distanceMoved = Math.Abs(newGridX - oldX) + Math.Abs(newGridY - oldY);

                    // Check if we're in combat and it's this token's turn
                    bool isInCombat = false;
                    bool isTokensTurn = false;
                    MainViewModel vm = null;

                    if (Application.Current?.MainWindow?.DataContext is MainViewModel mainVm)
                    {
                        vm = mainVm;
                        isInCombat = vm.IsInCombat;
                        isTokensTurn = token.IsCurrentTurn;
                    }

                    // Enforce movement limits during combat on the token's turn
                    if (isInCombat && isTokensTurn && distanceMoved > 0)
                    {
                        if (distanceMoved > token.MovementRemainingThisTurn)
                        {
                            // Can't move that far - snap back! 
                            Canvas.SetLeft(_draggingVisual, oldX * GridCellSize);
                            Canvas.SetTop(_draggingVisual, oldY * GridCellSize);

                            MessageBox.Show(
                                $"{token.Name} can only move {token.MovementRemainingThisTurn} more squares this turn!\n\n" +
                                $"Speed: {token.SpeedSquares} squares\n" +
                                $"Already moved: {token.MovementUsedThisTurn} squares\n" +
                                $"Remaining: {token.MovementRemainingThisTurn} squares\n" +
                                $"Attempted: {distanceMoved} squares",
                                "Movement Limit Exceeded",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            _isDraggingToken = false;
                            _draggingVisual = null;
                            e.Handled = true;
                            return;
                        }

                        // Use the movement
                        token.MovementUsedThisTurn += distanceMoved;

                        // Log the movement
                        AddToActionLog("Movement", $"{token.Name} moved {distanceMoved} squares ({token.MovementRemainingThisTurn} remaining)");
                    }

                    // Snap to grid if enabled
                    if (LockToGrid)
                    {
                        Canvas.SetLeft(_draggingVisual, newGridX * GridCellSize);
                        Canvas.SetTop(_draggingVisual, newGridY * GridCellSize);
                    }

                    // Update token position
                    token.GridX = newGridX;
                    token.GridY = newGridY;

                    // Record undo action if position changed
                    if (newGridX != oldX || newGridY != oldY)
                    {
                        if (vm != null)
                        {
                            var act = new TokenMoveAction(vm, token, oldX, oldY, newGridX, newGridY);
                            UndoManager.Record(act, performNow: false);
                        }
                    }
                }

                _isDraggingToken = false;
                _draggingVisual = null;
                e.Handled = true;

                RedrawMovementOverlay();
                ClearPathVisual();
            }
        }

        private void Tokens_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RebuildTokenVisuals();
        #endregion



        private void RebuildTokenVisuals()
        {
            var existing = RenderCanvas.Children.OfType<FrameworkElement>().Where(c => c.Tag is Token).ToList();
            foreach (var c in existing) RenderCanvas.Children.Remove(c);

            if (Tokens == null) return;

            foreach (var token in Tokens)
            {
                var container = new Grid()
                {
                    Width = GridCellSize * token.SizeInSquares + 8,
                    Height = GridCellSize * token.SizeInSquares + 8,
                    Tag = token
                };

                if (token.IsCurrentTurn)
                {
                    var glowBorder = new Border()
                    {
                        Width = GridCellSize * token.SizeInSquares + 4,
                        Height = GridCellSize * token.SizeInSquares + 4,
                        CornerRadius = new CornerRadius(4),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        BorderThickness = new Thickness(3),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect()
                        {
                            Color = Color.FromRgb(76, 175, 80),
                            BlurRadius = 15,
                            ShadowDepth = 0,
                            Opacity = 0.8
                        }
                    };
                    container.Children.Add(glowBorder);
                }

                var img = new Image
                {
                    Width = GridCellSize * token.SizeInSquares,
                    Height = GridCellSize * token.SizeInSquares,
                    Stretch = Stretch.UniformToFill,
                    Source = token.Image ?? LoadDefaultTokenImage(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = CreateTokenTooltip(token)
                };

                img.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        if (img.Tag is Token token)
                        {
                            TokenDoubleClicked?.Invoke(token);
                        }
                        e.Handled = true;
                    }
                    else
                    {
                        Token_MouseLeftButtonDown(s, e);
                    }
                };

                img.ContextMenu = CreateTokenContextMenu(token);

                ToolTipService.SetInitialShowDelay(img, 100);
                ToolTipService.SetShowDuration(img, 30000);
                ToolTipService.SetBetweenShowDelay(img, 0);

                container.Children.Add(img);

                container.MouseLeftButtonDown += Token_MouseLeftButtonDown;
                container.MouseMove += Token_MouseMove;
                container.MouseLeftButtonUp += Token_MouseLeftButtonUp;

                RenderCanvas.Children.Add(container);
                Canvas.SetZIndex(container, token.IsCurrentTurn ? 150 : 100);
            }

            LayoutTokens();
            UpdateGridVisual();
            RedrawLighting();
        }

        private ToolTip CreateTokenTooltip(Token token)
        {
            var tooltip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0),
                HasDropShadow = true
            };

            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                MinWidth = 180
            };

            var stack = new StackPanel();

            // Name (bold, larger)
            stack.Children.Add(new TextBlock
            {
                Text = token.Name ?? "Unknown",
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 2)
            });

            // Type and Size
            var subtitleText = "";
            if (!string.IsNullOrEmpty(token.Size)) subtitleText += token.Size;
            if (!string.IsNullOrEmpty(token.Type))
            {
                if (subtitleText.Length > 0) subtitleText += " ";
                subtitleText += token.Type;
            }
            if (!string.IsNullOrEmpty(subtitleText))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = subtitleText,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // HP Bar
            var hpPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            var hpHeader = new Grid();
            hpHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hpHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            hpHeader.Children.Add(new TextBlock
            {
                Text = "Hit Points",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130))
            });

            var hpText = new TextBlock
            {
                Text = $"{token.HP} / {token.MaxHP}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Color the HP text based on health percentage
            double hpPercent = token.MaxHP > 0 ? (double)Math.Max(0, token.HP) / token.MaxHP : 0;
            hpText.Foreground = hpPercent > 0.5 ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                                hpPercent > 0.25 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) :
                                new SolidColorBrush(Color.FromRgb(244, 67, 54));

            Grid.SetColumn(hpText, 1);
            hpHeader.Children.Add(hpText);
            hpPanel.Children.Add(hpHeader);

            // HP Bar visual
            var hpBarBg = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                CornerRadius = new CornerRadius(3),
                Height = 8,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var hpBarFill = new Border
            {
                Background = hpPercent > 0.5 ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                             hpPercent > 0.25 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) :
                             new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                CornerRadius = new CornerRadius(3),
                Height = 8,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(0, 156 * hpPercent) // 156 = minWidth - padding
            };

            var hpBarGrid = new Grid { Height = 8, Margin = new Thickness(0, 4, 0, 0) };
            hpBarGrid.Children.Add(hpBarBg);
            hpBarGrid.Children.Add(hpBarFill);
            hpPanel.Children.Add(hpBarGrid);

            stack.Children.Add(hpPanel);

            // Stats row (AC, CR, Speed)
            var statsGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // AC
            var acPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            acPanel.Children.Add(new TextBlock
            {
                Text = "AC",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            acPanel.Children.Add(new TextBlock
            {
                Text = token.ArmorClass.ToString(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(acPanel, 0);
            statsGrid.Children.Add(acPanel);

            // CR
            var crPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            crPanel.Children.Add(new TextBlock
            {
                Text = "CR",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            crPanel.Children.Add(new TextBlock
            {
                Text = token.ChallengeRating ?? "—",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(crPanel, 1);
            statsGrid.Children.Add(crPanel);

            // Initiative (if rolled)
            var initPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            initPanel.Children.Add(new TextBlock
            {
                Text = "Init",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            initPanel.Children.Add(new TextBlock
            {
                Text = token.Initiative > 0 ? token.Initiative.ToString() : "—",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(186, 104, 200)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(initPanel, 2);
            statsGrid.Children.Add(initPanel);

            stack.Children.Add(statsGrid);

            // Speed (smaller, below stats)
            if (!string.IsNullOrEmpty(token.Speed))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Speed: {token.Speed}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }

            bool showMovement = false;
            if (Application.Current?.MainWindow?.DataContext is MainViewModel vm && vm.IsInCombat)
            {
                showMovement = true;
            }

            if (showMovement)
            {
                var movementPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

                var movementHeader = new Grid();
                movementHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                movementHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                movementHeader.Children.Add(new TextBlock
                {
                    Text = "Movement",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130))
                });

                var movementText = new TextBlock
                {
                    Text = $"{token.MovementRemainingThisTurn} / {token.SpeedSquares}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Foreground = token.CanMoveThisTurn
                        ? new SolidColorBrush(Color.FromRgb(100, 181, 246))
                        : new SolidColorBrush(Color.FromRgb(244, 67, 54))
                };
                Grid.SetColumn(movementText, 1);
                movementHeader.Children.Add(movementText);
                movementPanel.Children.Add(movementHeader);

                // Movement bar
                double movePercent = token.SpeedSquares > 0
                    ? (double)token.MovementRemainingThisTurn / token.SpeedSquares
                    : 0;

                var moveBarGrid = new Grid { Height = 6, Margin = new Thickness(0, 4, 0, 0) };
                moveBarGrid.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    CornerRadius = new CornerRadius(3)
                });
                moveBarGrid.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                    CornerRadius = new CornerRadius(3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = Math.Max(0, 156 * movePercent)
                });
                movementPanel.Children.Add(moveBarGrid);

                stack.Children.Add(movementPanel);
            }

            // Conditions
            if (token.Conditions != Models.Condition.None)
            {
                var conditionsBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(50, 40, 30)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 8, 0, 0)
                };

                conditionsBorder.Child = new TextBlock
                {
                    Text = $"⚠ {token. ConditionsDisplay}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    TextWrapping = TextWrapping. Wrap
                };

                stack.Children. Add(conditionsBorder);
            }

            // Tags (if any)
            if (token.Tags != null && token.Tags.Count > 0)
            {
                var tagsPanel = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
                foreach (var tag in token.Tags.Take(4)) // Limit to 4 tags
                {
                    tagsPanel.Children.Add(new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(40, 80, 35)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 4, 0),
                        Child = new TextBlock
                        {
                            Text = tag,
                            FontSize = 9,
                            Foreground = Brushes.White
                        }
                    });
                }
                if (token.Tags.Count > 4)
                {
                    tagsPanel.Children.Add(new TextBlock
                    {
                        Text = $"+{token.Tags.Count - 4} more",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
                stack.Children.Add(tagsPanel);
            }

            mainBorder.Child = stack;
            tooltip.Content = mainBorder;
            return tooltip;
        }

        private ImageSource LoadDefaultTokenImage()
        {
            try 
            { 
                return new BitmapImage(new Uri("pack://application:,,,/Resources/default-token.png", UriKind.Absolute)); 
            } 
            catch { return null; }
        }

        private void LayoutTokens()
        {
            var tokenVisuals = RenderCanvas.Children.OfType<FrameworkElement>().Where(c => c.Tag is Token);
            foreach (var vis in tokenVisuals)
            {
                var token = (Token)vis.Tag;
                Canvas.SetLeft(vis, token.GridX * GridCellSize);
                Canvas.SetTop(vis, token.GridY * GridCellSize);
                vis.Width = GridCellSize * token.SizeInSquares;
                vis.Height = GridCellSize * token.SizeInSquares;
            }
        }

        private void UpdateGridVisual()
        {
            var screenRect = new Rect(0, 0, ActualWidth, ActualHeight);
            var inv = _transformGroup.Inverse;
            Rect worldRect;
            if (inv != null)
            {
                var tl = inv.Transform(new Point(screenRect.Left, screenRect.Top));
                var br = inv.Transform(new Point(screenRect.Right, screenRect.Bottom));
                worldRect = new Rect(tl, br);
            }
            else worldRect = new Rect(0, 0, ActualWidth, ActualHeight);

            GridHost.DrawGridViewport(GridCellSize, worldRect);
        }

        #region Options getters

        public void SetGridMaxSize(int maxWidth, int maxHeight)
        {
            _gridWidth = Math.Max(1, maxWidth);
            _gridHeight = Math.Max(1, maxHeight);
            UpdateGridVisual();
            RedrawMovementOverlay();
            RedrawLighting();
        }

        public void UpdateShadowSoftness()
        {
            var wrapper = RenderCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(w => Panel.GetZIndex(w) == 50);
            if (wrapper != null) wrapper.Effect = new BlurEffect { Radius = Options.ShadowSoftnessPx, RenderingBias = RenderingBias.Quality };
        }

        private bool _isMiddlePanning = false;
        private Point _middlePanLast;

        private void RenderCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isMiddlePanning = true;
                _middlePanLast = e.GetPosition(this);
                Cursor = Cursors.Hand;
                RenderCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void RenderCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && _isMiddlePanning)
            {
                _isMiddlePanning = true;
                Cursor = Cursors.Arrow;
                RenderCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void RenderCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMiddlePanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                var pt = e.GetPosition(this);
                var dx = pt.X - _middlePanLast.X;
                var dy = pt.Y - _middlePanLast.Y;
                _pan.X += dx;
                _pan.Y += dy;
                _middlePanLast = pt;

                UpdateGridVisual();
                RedrawLighting();
                RedrawMovementOverlay();
                RedrawPathVisual();
                RedrawObstacleDrawVisual();

                e.Handled = true;
                return;
            }
        }

        #endregion

        #region Pan / Zoom / Mouse
        private void RenderCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = (e.Delta > 0) ? 1.12 : 1.0 / 1.12;
            var pos = e.GetPosition(RenderCanvas);
            double absX = pos.X * _zoom.ScaleX + _pan.X;
            double absY = pos.Y * _zoom.ScaleY + _pan.Y;

            _zoom.ScaleX *= zoomFactor;
            _zoom.ScaleY *= zoomFactor;

            _pan.X = absX - pos.X * _zoom.ScaleX;
            _pan.Y = absY - pos.Y * _zoom.ScaleY;

            UpdateGridVisual(); RedrawLighting(); RedrawMovementOverlay(); RedrawPathVisual(); RedrawObstacleDrawVisual();
        }

        private void RenderCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var worldPt = ScreenToWorld(e.GetPosition(RenderCanvas));
            var gridPoint = new Point(
                worldPt.X / GridCellSize,
                worldPt.Y / GridCellSize);

            // Wall drawing mode
            if (_wallDrawMode)
            {
                // Snap to grid intersections if LockToGrid
                if (LockToGrid)
                {
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                }

                HandleWallDrawClick(gridPoint, e);
                e.Handled = true;
                return;
            }

            // Check for wall selection/interaction
            if (!_drawObstacleMode)
            {
                HandleWallSelection(gridPoint);
                if (_selectedWall != null)
                {
                    e.Handled = true;
                    return;
                }
            }

            Debug.WriteLine("RenderCanvas clicked; draw mode=" + _drawObstacleMode);
            if (_drawObstacleMode)
            {
                Debug.WriteLine($"[DrawMode] Canvas click at {e.GetPosition(RenderCanvas)}");
                var world = ScreenToWorld(e.GetPosition(RenderCanvas));
                var gx = (int)Math.Floor(world.X / GridCellSize);
                var gy = (int)Math.Floor(world.Y / GridCellSize);
                _currentDrawVertices.Add(new Point(gx, gy));
                RedrawObstacleDrawVisual();
                if (e.ClickCount >= 2 && _currentDrawVertices.Count >= 3)
                    FinishCurrentObstacle();
                e.Handled = true;
                return;
            }

            // obstacle hit test: select vertex/obstacle if present
            var clickedGrid = new Point(worldPt.X / GridCellSize, worldPt.Y / GridCellSize);
            var found = HitTestObstacle(clickedGrid, out int vertexIdx);
            if (found != null)
            {
                _selectedObstacle = found;
                _selectedVertexIndex = vertexIdx;
                if (vertexIdx >= 0)
                {
                    _isDraggingVertex = true;
                    _vertexDragOriginalPos = _selectedObstacle.PolygonGridPoints[vertexIdx];
                }
                RedrawObstacleDrawVisual();
                return;
            }

            if (e.MiddleButton == MouseButtonState.Pressed || (Keyboard.IsKeyDown(Key.Space) && e.LeftButton == MouseButtonState.Pressed))
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(this);
                Cursor = Cursors.Hand;
                RenderCanvas.CaptureMouse();
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (SelectedToken != null)
                {
                    var screenPt = e.GetPosition(RenderCanvas);
                    var world = ScreenToWorld(screenPt);
                    var targetCell = (x: (int)Math.Floor(world.X / GridCellSize), y: (int)Math.Floor(world.Y / GridCellSize));
                    ComputeAndDrawPathPreview(targetCell);
                }
            }
        }

        private void RenderCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingWallEndpoint)
            {
                _isDraggingWallEndpoint = false;
                AddToActionLog("Wall", $"Moved wall endpoint");
                return;
            }

            if (_isDraggingVertex && _selectedObstacle != null && _selectedVertexIndex >= 0)
            {
                var newPos = _selectedObstacle.PolygonGridPoints[_selectedVertexIndex];
                _undoStack.Push(new Models.ObstacleAction { ActionType = Models.ObstacleActionType.MoveVertex, Obstacle = _selectedObstacle, VertexIndex = _selectedVertexIndex, OldPosition = _vertexDragOriginalPos, NewPosition = newPos });
                _redoStack.Clear();
                _isDraggingVertex = false;
                _selectedVertexIndex = -1;
                RedrawObstacleDrawVisual();
                return;
            }

            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Arrow;
                RenderCanvas.ReleaseMouseCapture();
            }
        }

        private void RenderCanvas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var worldPt = ScreenToWorld(e.GetPosition(RenderCanvas));
            var gridPoint = new Point(
                worldPt.X / GridCellSize,
                worldPt.Y / GridCellSize);

            var hitWall = _wallService.HitTest(gridPoint, 0.5);
            if (hitWall != null && hitWall.WallType == WallType.Door)
            {
                hitWall.IsOpen = !hitWall.IsOpen;
                AddToActionLog("Door", $"{hitWall.Label} is now {(hitWall.IsOpen ? "OPEN" : "CLOSED")}");
                RedrawWalls();
                RedrawLighting();
                e.Handled = true;
            }
        }

        private void RenderCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var worldPt = ScreenToWorld(e.GetPosition(RenderCanvas));
            var gridPoint = new Point(
                worldPt.X / GridCellSize,
                worldPt.Y / GridCellSize);

            // Wall drawing preview
            if (_wallDrawMode && _wallDrawStart.HasValue)
            {
                if (LockToGrid)
                {
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                }
                _wallDrawPreview = gridPoint;
                RedrawWalls();
            }

            // Wall endpoint dragging
            if (_isDraggingWallEndpoint && _selectedWall != null)
            {
                if (LockToGrid)
                {
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                }

                if (_draggingWallIsStart)
                    _selectedWall.StartPoint = gridPoint;
                else
                    _selectedWall.EndPoint = gridPoint;

                RedrawWalls();
                RedrawLighting();
                return;
            }

            if (_isDraggingVertex && _selectedObstacle != null && _selectedVertexIndex >= 0 && e.LeftButton == MouseButtonState.Pressed)
            {
                var world = ScreenToWorld(e.GetPosition(RenderCanvas));
                var gx = world.X / GridCellSize;
                var gy = world.Y / GridCellSize;
                double nx = gx, ny = gy;
                if (LockToGrid) { nx = Math.Round(gx); ny = Math.Round(gy); }
                _selectedObstacle.PolygonGridPoints[_selectedVertexIndex] = new Point(nx, ny);
                _spatialIndex.Clear(); foreach (var o in _obstacles) _spatialIndex.IndexObstacle(o);
                RedrawObstacleDrawVisual(); RedrawMovementOverlay(); RedrawLighting();
                return;
            }

            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                var pt = e.GetPosition(this);
                var dx = pt.X - _lastPanPoint.X;
                var dy = pt.Y - _lastPanPoint.Y;
                _pan.X += dx; _pan.Y += dy; _lastPanPoint = pt;
                UpdateGridVisual(); RedrawLighting(); RedrawMovementOverlay(); RedrawPathVisual(); RedrawObstacleDrawVisual();
            }
        }

        private void RenderCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_wallDrawMode)
            {
                // Cancel wall drawing
                _wallDrawStart = null;
                RedrawWalls();
                e.Handled = true;
                return;
            }

            if (_selectedWall != null)
            {
                // Delete selected wall
                var result = MessageBox.Show(
                    $"Delete wall '{_selectedWall.Label}'?",
                    "Delete Wall",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _wallService.RemoveWall(_selectedWall);
                    AddToActionLog("Wall", $"Deleted {_selectedWall.Label}");
                    _selectedWall = null;
                    RedrawWalls();
                    RedrawLighting();
                }
                e.Handled = true;
                return;
            }

            if (_drawObstacleMode) { _currentDrawVertices.Clear(); RedrawObstacleDrawVisual(); return; }
            if (_selectedObstacle != null)
            {
                var removed = _selectedObstacle;
                _obstacles.Remove(removed);
                _spatialIndex.Clear(); foreach (var o in _obstacles) _spatialIndex.IndexObstacle(o);
                _undoStack.Push(new Models.ObstacleAction { ActionType = Models.ObstacleActionType.Remove, Obstacle = removed });
                _redoStack.Clear();
                _selectedObstacle = null; _selectedVertexIndex = -1;
                RedrawObstacleDrawVisual(); RedrawMovementOverlay(); RedrawLighting();
            }
        }

        private void RenderCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var imageFile = files.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(imageFile)) { MapImage.Source = new BitmapImage(new Uri(imageFile)); MapImage.SetValue(Canvas.ZIndexProperty, -100); e.Handled = true; return; }
                }

                if (e.Data.GetDataPresent("DnDBattle.Token"))
                {
                    var proto = e.Data.GetData("DnDBattle.Token") as Token;
                    if (proto != null)
                    {
                        var protoId = proto.Id.ToString();
                        var now = DateTime.UtcNow;
                        if (_lastDropPrototypeId == protoId && (now - _lastDropTime) < _duplicateDropThreshold) { e.Handled = true; return; }
                        _lastDropPrototypeId = protoId; _lastDropTime = now;

                        var dropPt = e.GetPosition(RenderCanvas);
                        var world = ScreenToWorld(dropPt);
                        int gx = (int)Math.Floor(world.X / GridCellSize);
                        int gy = (int)Math.Floor(world.Y / GridCellSize);
                        var newToken = new Token { Name = proto.Name, Image = proto.Image, HP = proto.HP, ArmorClass = proto.ArmorClass, InitiativeModifier = proto.InitiativeModifier, IsPlayer = proto.IsPlayer, Speed = proto.Speed, GridX = gx, GridY = gy, SizeInSquares = proto.SizeInSquares };
                        Tokens?.Add(newToken); SelectedToken = newToken;
                        RedrawMovementOverlay(); RedrawPathVisual();
                        e.Handled = true; return;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Drop handler error: {ex}"); }
        }

        public void PanBy(double dx, double dy)
        {
            _pan.X = dx;
            _pan.Y = dy;
            UpdateGridVisual();
            RedrawLighting();
            RedrawMovementOverlay();
            RedrawPathVisual();
            RedrawObstacleDrawVisual();
        }
        #endregion

        #region Obstacle draw/edit helpers
        // Hit-test: returns obstacle clicked and vertex index if clicked near a vertex (-1 if clicked on polygon but not a vertex)
        private Obstacle HitTestObstacle(Point gridPoint, out int vertexIndex)
        { 
            vertexIndex = -1; 
            const double vertexHitRadius = 0.4; 
            foreach (var o in _obstacles) 
            { 
                for (int i = 0; i < o.PolygonGridPoints.Count; i++) 
                { 
                    var v = o.PolygonGridPoints[i];
                    if (Math.Abs(v.X - gridPoint.X) <= vertexHitRadius && Math.Abs(v.Y - gridPoint.Y) <= vertexHitRadius) 
                    { 
                        vertexIndex = i; 
                        return o; 
                    } 
                } 
                if (LOSService.PointInPolygonInternal(new Point(gridPoint.X + 0.5, gridPoint.Y + 0.5), o.PolygonGridPoints)) 
                { 
                    vertexIndex = -1;
                    return o; 
                } 
            } 
            return null; 
        }

        // Start/stop draw mode
        public void SetObstacleDrawMode(bool enabled)
        {
            _drawObstacleMode = enabled;
            _currentDrawVertices.Clear();
            RedrawObstacleDrawVisual();
            SetDrawingCursor(enabled);
        }

        public void SetDrawingCursor(bool drawing)
        {
            if (RenderCanvas == null) return;
            RenderCanvas.Cursor = drawing ? Cursors.Cross : Cursors.Arrow;
        }

        private void RedrawObstacleDrawVisual()
        {
            using (var dc = _obstacleDrawVisual.RenderOpen())
            {
                // More visible obstacle fill and stroke
                var fillBrush = new SolidColorBrush(Color.FromArgb(150, 139, 69, 19)); // Brown, semi-transparent
                var strokeBrush = new SolidColorBrush(Color.FromArgb(255, 255, 140, 0)); // Orange
                fillBrush.Freeze();

                var stroke = new Pen(strokeBrush, 3.0); // Thicker stroke
                stroke.Freeze();

                // Draw existing obstacles
                foreach (var o in _obstacles)
                {
                    if (o.PolygonGridPoints.Count < 3) continue;

                    var pg = new PathGeometry();
                    var startPoint = new Point(
                        o.PolygonGridPoints[0].X * GridCellSize + GridCellSize / 2,
                        o.PolygonGridPoints[0].Y * GridCellSize + GridCellSize / 2);
                    var pf = new PathFigure { StartPoint = startPoint, IsClosed = true, IsFilled = true };

                    for (int i = 1; i < o.PolygonGridPoints.Count; i++)
                    {
                        var point = new Point(
                            o.PolygonGridPoints[i].X * GridCellSize + GridCellSize / 2,
                            o.PolygonGridPoints[i].Y * GridCellSize + GridCellSize / 2);
                        pf.Segments.Add(new LineSegment(point, true));
                    }
                    pg.Figures.Add(pf);
                    dc.DrawGeometry(fillBrush, stroke, pg);

                    // Draw vertex handles if this obstacle is selected
                    if (_selectedObstacle == o)
                    {
                        var handleBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)); // Gold
                        handleBrush.Freeze();
                        var handlePen = new Pen(Brushes.Black, 2);
                        handlePen.Freeze();

                        foreach (var v in o.PolygonGridPoints)
                        {
                            var handleRect = new Rect(
                                v.X * GridCellSize + GridCellSize / 2 - 6,
                                v.Y * GridCellSize + GridCellSize / 2 - 6,
                                12, 12);
                            dc.DrawRectangle(handleBrush, handlePen, handleRect);
                        }
                    }
                }

                // Draw current drawing vertices (when in draw mode)
                if (_currentDrawVertices.Count > 0)
                {
                    var drawingPen = new Pen(Brushes.Yellow, 3);
                    drawingPen.Freeze();
                    var drawingFill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0)); // Yellow tint
                    drawingFill.Freeze();

                    // Draw lines connecting vertices
                    if (_currentDrawVertices.Count >= 2)
                    {
                        var pg = new PathGeometry();
                        var startPt = new Point(
                            _currentDrawVertices[0].X * GridCellSize + GridCellSize / 2,
                            _currentDrawVertices[0].Y * GridCellSize + GridCellSize / 2);
                        var pf = new PathFigure { StartPoint = startPt, IsClosed = false };

                        for (int i = 1; i < _currentDrawVertices.Count; i++)
                        {
                            var pt = new Point(
                                _currentDrawVertices[i].X * GridCellSize + GridCellSize / 2,
                                _currentDrawVertices[i].Y * GridCellSize + GridCellSize / 2);
                            pf.Segments.Add(new LineSegment(pt, true));
                        }
                        pg.Figures.Add(pf);
                        dc.DrawGeometry(null, drawingPen, pg);
                    }

                    // Draw vertex markers
                    foreach (var v in _currentDrawVertices)
                    {
                        var rect = new Rect(
                            v.X * GridCellSize + GridCellSize / 4,
                            v.Y * GridCellSize + GridCellSize / 4,
                            GridCellSize / 2,
                            GridCellSize / 2);
                        dc.DrawRectangle(drawingFill, new Pen(Brushes.Yellow, 2), rect);
                    }

                    // Show hint text
                    if (_currentDrawVertices.Count >= 1)
                    {
                        var lastPt = _currentDrawVertices[^1];
                        var textPoint = new Point(
                            lastPt.X * GridCellSize + GridCellSize,
                            lastPt.Y * GridCellSize);

                        var formattedText = new FormattedText(
                            _currentDrawVertices.Count < 3
                                ? $"Click to add points ({_currentDrawVertices.Count}/3 min)\nDouble-click to finish"
                                : "Double-click to finish\nRight-click to cancel",
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            12,
                            Brushes.White,
                            1.0);

                        dc.DrawText(formattedText, textPoint);
                    }
                }
            }
        }

        private void FinishCurrentObstacle()
        {
            if (_currentDrawVertices.Count < 3) return;
            var obs = new Obstacle { Label = $"Wall-{_obstacles.Count + 1}", PolygonGridPoints = _currentDrawVertices.Select(p => new Point(p.X, p.Y)).ToList() };
            _obstacles.Add(obs); _spatialIndex.IndexObstacle(obs);
            Services.UndoManager.Record(new Models.ObstacleAddAction(obs, this));
            _currentDrawVertices.Clear(); _drawObstacleMode = false;
            RedrawObstacleDrawVisual(); RedrawMovementOverlay(); RedrawLighting();
        }
        #endregion

        #region Wall Drawing

        public void SetWallDrawMode(bool enabled, WallType wallType = WallType.Solid)
        {
            _wallDrawMode = enabled;
            _currentWallType = wallType;
            _wallDrawStart = null;
            _selectedWall = null;

            RenderCanvas.Cursor = enabled ? Cursors.Cross : Cursors.Arrow;
            RedrawWalls();
        }

        public void AddWall(Wall wall) =>
            _wallService.AddWall(wall);

        public void RemoveWall(Wall wall) =>
            _wallService.RemoveWall(wall);

        public void RedrawWalls()
        {
            using (var dc = _wallVisual.RenderOpen())
            {
                var solidPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 139, 90, 43)), 6);
                var doorPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 101, 67, 33)), 6);
                var doorOpenPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 101, 67, 33)), 4);
                var windowPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 135, 206, 235)), 4);
                var halfWallPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 169, 169, 169)), 4);
                var selectedPen = new Pen(Brushes.Yellow, 3);

                solidPen.DashStyle = DashStyles.Solid;
                doorPen.DashStyle = DashStyles.Solid;
                doorOpenPen.DashStyle = DashStyles.Dash;
                windowPen.DashStyle = DashStyles.DashDot;
                halfWallPen.DashStyle = DashStyles.Dot;

                solidPen.Freeze();
                doorPen.Freeze();
                doorOpenPen.Freeze();
                windowPen.Freeze();
                halfWallPen.Freeze();
                selectedPen.Freeze();                

                foreach (var wall in _wallService.Walls)
                {
                    var startPx = new Point(
                        wall.StartPoint.X * GridCellSize + GridCellSize / 2,
                        wall.StartPoint.Y * GridCellSize + GridCellSize / 2);
                    var endPx = new Point(
                        wall.EndPoint.X * GridCellSize + GridCellSize / 2,
                        wall.EndPoint.Y * GridCellSize + GridCellSize / 2);

                    Pen wallPen = wall.WallType switch
                    {
                        WallType.Solid => solidPen,
                        WallType.Door => wall.IsOpen ? doorOpenPen : doorPen,
                        WallType.Window => windowPen,
                        WallType.Halfwall => halfWallPen,
                        _ => solidPen
                    };

                    var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), wallPen.Thickness);
                    shadowPen.Freeze();
                    dc.DrawLine(shadowPen,
                        new Point(startPx.X + 2, startPx.Y + 2),
                        new Point(endPx.X + 2, endPx.Y + 2));

                    dc.DrawLine(wallPen, startPx, endPx);

                    if (wall == _selectedWall)
                    {
                        dc.DrawLine(selectedPen, startPx, endPx);

                        var handleBrush = Brushes.Yellow;
                        dc.DrawEllipse(handleBrush, new Pen(Brushes.Black, 2), startPx, 8, 8);
                        dc.DrawEllipse(handleBrush, new Pen(Brushes.Black, 2), endPx, 8, 8);
                    }

                    if (wall.WallType == WallType.Door)
                    {
                        var midPoint = new Point((startPx.X + endPx.X) / 2, (startPx.Y + endPx.Y) / 2);
                        var doorBrush = wall.IsOpen
                            ? new SolidColorBrush(Color.FromArgb(200, 76, 175, 80)) // Green for open
                            : new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)); // Red for closed
                        doorBrush.Freeze();

                        dc.DrawEllipse(doorBrush, new Pen(Brushes.White, 2), midPoint, 10, 10);

                        var iconText = wall.IsOpen ? "🚪" : "🔒";
                        var formattedText = new FormattedText(
                            iconText,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI Emoji"),
                            14,
                            Brushes.White,
                            1.0);
                        dc.DrawText(formattedText, new Point(midPoint.X - 7, midPoint.Y - 10));
                    }
                }

                if (_wallDrawMode && _wallDrawStart.HasValue)
                {
                    var previewPen = new Pen(Brushes.LimeGreen, 4);
                    previewPen.DashStyle = DashStyles.Dash;
                    previewPen.Freeze();

                    var startPx = new Point(
                        _wallDrawStart.Value.X * GridCellSize + GridCellSize / 2,
                        _wallDrawStart.Value.Y * GridCellSize + GridCellSize / 2);
                    var endPx = new Point(
                        _wallDrawPreview.X * GridCellSize + GridCellSize / 2,
                        _wallDrawPreview.Y * GridCellSize + GridCellSize / 2);

                    dc.DrawLine(previewPen, startPx, endPx);

                    dc.DrawEllipse(Brushes.LimeGreen, null, startPx, 6, 6);
                    dc.DrawEllipse(Brushes.LimeGreen, null, endPx, 6, 6);

                    var length = Math.Sqrt(
                        Math.Pow(_wallDrawPreview.X - _wallDrawStart.Value.X, 2) +
                        Math.Pow(_wallDrawPreview.Y - _wallDrawStart.Value.Y, 2));

                    var midPoint = new Point((startPx.X + endPx.X) / 2, (startPx.Y + endPx.Y) / 2 - 20);
                    var lengthText = new FormattedText(
                        $"{length:F1} squares",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        12,
                        Brushes.White,
                        1.0);

                    var textBounds = new Rect(midPoint.X - 5, midPoint.Y - 2, lengthText.Width + 10, lengthText.Height + 4);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)), null, textBounds);
                    dc.DrawText(lengthText, midPoint);
                }

                if (_wallDrawMode)
                {
                    var indicatorText = new FormattedText(
                        $"Wall Mode: {_currentWallType}\nClick to start, click again to place\nRight-click to cancel",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        12,
                        Brushes.White,
                        1.0);

                    var bgRect = new Rect(10, 10, indicatorText.Width + 20, indicatorText.Height + 10);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)), null, bgRect);
                    dc.DrawText(indicatorText, new Point(20, 15));
                }
            }
        }

        private void HandleWallDrawClick(Point gridPoint, MouseButtonEventArgs e)
        {
            if (!_wallDrawMode) return;

            if (e.ChangedButton == MouseButton.Right)
            {
                _wallDrawStart = null;
                RedrawWalls();
                return;
            }

            if (_wallDrawStart == null)
            {
                _wallDrawStart = gridPoint;
                _wallDrawPreview = gridPoint;
            }
            else
            {
                if (_wallDrawStart.Value != gridPoint)
                {
                    var wall = new Wall()
                    {
                        StartPoint = _wallDrawStart.Value,
                        EndPoint = gridPoint,
                        WallType = _currentWallType,
                        Label = $"{_currentWallType} Wall"
                    };

                    _wallService.AddWall(wall);
                    AddToActionLog("Wall", $"Added {_currentWallType} wall from ({wall.StartPoint.X:F0},{wall.StartPoint.Y:F0}) to ({wall.EndPoint.X:F0},{wall.EndPoint.Y:F0})");
                }

                _wallDrawStart = null;
            }

            RedrawWalls();
            RedrawLighting();
        }

        private void HandleWallSelection(Point gridPoint)
        {
            var endPointWall = _wallService.HitTestEndPoint(gridPoint, out bool isStart, 0.5);
            if (endPointWall != null)
            {
                _selectedWall = endPointWall;
                _isDraggingWallEndpoint = true;
                _draggingWallIsStart = isStart;
                RedrawWalls();
                return;
            }

            var hitWall = _wallService.HitTest(gridPoint, 0.5);
            if (hitWall != null)
            {
                _selectedWall = hitWall;

                if (hitWall.WallType == WallType.Door)
                {
                    hitWall.IsOpen = !hitWall.IsOpen;
                }

                RedrawWalls();
                return;
            }

            _selectedWall = null;
            RedrawWalls();
        }

        #endregion

        #region Movement overlay
        public void RedrawMovementOverlay()
        {
            using (var dc = _movementVisual.RenderOpen())
            {
                if (SelectedToken == null) return;
                int startX = SelectedToken.GridX;
                int startY = SelectedToken.GridY;
                int maxSquares = SelectedToken.SpeedSquares;

                Func<int, int, bool> isBlocked = (gx, gy) =>
                {
                    var obs = _spatialIndex.QueryObstaclesInCell(gx, gy);
                    foreach (var o in obs)
                    {
                        var center = new System.Windows.Point(gx + 0.5, gy + 0.5);
                        if (LOSService.PointInPolygonInternal(center, o.PolygonGridPoints)) return true;
                    }
                    return false;
                };

                var reachable = MovementService.GetReachableSquares(startX, startY, maxSquares, _gridWidth, _gridHeight, isBlocked);

                var brush = new SolidColorBrush(Color.FromArgb(120, 30, 144, 255));
                brush.Freeze();
                foreach (var cell in reachable)
                {
                    var rect = new Rect(cell.x * GridCellSize, cell.y * GridCellSize, GridCellSize, GridCellSize);
                    dc.DrawRectangle(brush, null, rect);
                }
            }
        }
        #endregion

        #region Path preview (unchanged)
        private void ComputeAndDrawPathPreview((int x, int y) targetCell)
        {
            if (SelectedToken == null) return;

            var start = (SelectedToken.GridX, SelectedToken.GridY);
            var goal = targetCell;

            Func<int, int, bool> isWalkable = (gx, gy) =>
            {
                var center = new Point(gx + 0.5, gy + 0.5);
                var obs = _spatialIndex.QueryObstaclesInCell(gx, gy);
                foreach (var o in obs)
                {
                    if (LOSService.PointInPolygonInternal(center, o.PolygonGridPoints)) return false;
                }
                return true;
            };

            var path = MovementService.FindPathAStar(start, goal, _gridWidth, _gridHeight, isWalkable);
            _lastPreviewPath = path;

            var enemies = new List<(int x, int y)>();
            if (Tokens != null)
            {
                foreach (var t in Tokens)
                {
                    if (t.Id == SelectedToken.Id) continue;
                    if (t.IsPlayer == SelectedToken.IsPlayer) continue;
                    enemies.Add((t.GridX, t.GridY));
                }
            }

            _lastAooIndices = MovementService.ComputeAOOIndices(path, enemies);
            RedrawPathVisual();
        }

        private void RedrawPathVisual()
        {
            using (var dc = _pathVisual.RenderOpen())
            {
                if (_lastPreviewPath == null || _lastPreviewPath.Count == 0) return;

                var pen = new Pen(Brushes.LightBlue, 2); pen.Freeze();
                var stepBrush = new SolidColorBrush(Color.FromArgb(200, 100, 180, 255)); stepBrush.Freeze();
                var aooBrush = new SolidColorBrush(Color.FromArgb(220, 220, 50, 50)); aooBrush.Freeze();

                for (int i = 0; i < _lastPreviewPath.Count; i++)
                {
                    var s = _lastPreviewPath[i];
                    var rect = new Rect(s.x * GridCellSize, s.y * GridCellSize, GridCellSize, GridCellSize);
                    dc.DrawRectangle(stepBrush, null, rect);

                    if (_lastAooIndices != null && _lastAooIndices.Contains(i))
                    {
                        var cx = rect.Left + rect.Width / 2;
                        var cy = rect.Top + rect.Height / 2;
                        dc.DrawEllipse(null, new Pen(aooBrush, 3), new Point(cx, cy), rect.Width * 0.35, rect.Height * 0.35);
                    }
                }

                var pts = _lastPreviewPath.Select(p => new Point(p.x * GridCellSize + GridCellSize / 2.0, p.y * GridCellSize + GridCellSize / 2.0)).ToArray();
                if (pts.Length >= 2)
                {
                    var pg = new PathGeometry();
                    var pf = new PathFigure { StartPoint = pts[0], IsClosed = false };
                    for (int i = 1; i < pts.Length; i++) pf.Segments.Add(new LineSegment(pts[i], true));
                    pg.Figures.Add(pf);
                    dc.DrawGeometry(null, pen, pg);
                }
            }
        }

        private void ClearPathVisual()
        {
            _lastPreviewPath = null;
            _lastAooIndices = null;
            using (var dc = _pathVisual.RenderOpen()) { }
        }
        #endregion

        #region Lighting (unchanged aside from obstacle positions)
        public void AddLight(LightSource light)
        {
            _lights.Add(light);
            _spatialIndex.IndexLight(light);
            RedrawLighting();
        }

        public void AddObstacle(Obstacle obs)
        {
            _obstacles.Add(obs);
            _spatialIndex.IndexObstacle(obs);
            RedrawMovementOverlay();
            RedrawLighting();
        }

        private void RedrawLighting()
        {
            using (var dc = _lightingVisual.RenderOpen())
            {
                if (_lights == null || _lights.Count == 0)
                {
                    return;
                }

                foreach (var light in _lights)
                {
                    var centerGrid = light.CenterGrid;
                    var centerPixel = new Point(
                        centerGrid.X * GridCellSize + GridCellSize / 2.0,
                        centerGrid.Y * GridCellSize + GridCellSize / 2.0);
                    double radiusPx = Math.Max(GridCellSize, light.RadiusSquares * GridCellSize);

                    // Compute lit polygon using wall service
                    var litPolygonGrid = _wallService.ComputeLitPolygon(centerGrid, light.RadiusSquares, 180);

                    if (litPolygonGrid.Count < 3)
                    {
                        // No walls blocking - draw full circle
                        var rg = CreateLightGradient(light.Intensity);
                        dc.DrawEllipse(rg, null, centerPixel, radiusPx, radiusPx);
                    }
                    else
                    {
                        // Create polygon geometry from lit area
                        var litGeometry = new PathGeometry();
                        var figure = new PathFigure
                        {
                            StartPoint = new Point(
                                litPolygonGrid[0].X * GridCellSize + GridCellSize / 2,
                                litPolygonGrid[0].Y * GridCellSize + GridCellSize / 2),
                            IsClosed = true,
                            IsFilled = true
                        };

                        for (int i = 1; i < litPolygonGrid.Count; i++)
                        {
                            figure.Segments.Add(new LineSegment(
                                new Point(
                                    litPolygonGrid[i].X * GridCellSize + GridCellSize / 2,
                                    litPolygonGrid[i].Y * GridCellSize + GridCellSize / 2),
                                true));
                        }

                        litGeometry.Figures.Add(figure);

                        // Draw with radial gradient clipped to lit area
                        var rg = CreateLightGradient(light.Intensity);

                        dc.PushClip(litGeometry);
                        dc.PushTransform(new TranslateTransform(centerPixel.X - radiusPx, centerPixel.Y - radiusPx));
                        dc.DrawRectangle(rg, null, new Rect(0, 0, radiusPx * 2, radiusPx * 2));
                        dc.Pop();
                        dc.Pop();
                    }

                    // Draw light source indicator
                    var centerBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 150));
                    centerBrush.Freeze();
                    dc.DrawEllipse(centerBrush, new Pen(Brushes.Orange, 2), centerPixel, 8, 8);
                }
            }
        }

        private RadialGradientBrush CreateLightGradient(double intensity)
        {
            var rg = new RadialGradientBrush();

            byte centerAlpha = (byte)(200 * intensity);
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(centerAlpha, 255, 255, 200), 0.0));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(centerAlpha * 0.5), 255, 220, 150), 0.5));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 150, 50), 1.0));

            rg.RadiusX = 1;
            rg.RadiusY = 1;
            rg.Center = new Point(0.5, 0.5);
            rg.GradientOrigin = new Point(0.5, 0.5);
            rg.Freeze();

            return rg;

        }

        #endregion

        #region Undo / Redo API for obstacles

        public void RemoveObstaclePublic(Obstacle obs)
        {
            if (obs == null) return;
            _obstacles.Remove(obs);
            _spatialIndex.Clear();
            foreach (var o in _obstacles) _spatialIndex.IndexObstacle(o);
            RedrawObstacleDrawVisual(); RedrawMovementOverlay(); RedrawLighting();
        }

        public void MoveVertexPublic(Obstacle obs, int index, Point pos)
        {
            if (obs == null) return;
            if (index < 0 || index >= obs.PolygonGridPoints.Count) return;
            obs.PolygonGridPoints[index] = pos;
            _spatialIndex.Clear();
            foreach (var o in _obstacles) _spatialIndex.IndexObstacle(o);
            RedrawObstacleDrawVisual(); RedrawMovementOverlay(); RedrawLighting();
        }

        public void RemoveLightPublic(LightSource light)
        {
            if (light == null) return;
            _lights.Remove(light);
            RedrawLighting();
        }
        #endregion

        #region Public API: commit previewed path with animation
        public async Task CommitPreviewedPathAsync()
        {
            if (_lastPreviewPath == null || _lastPreviewPath.Count == 0 || SelectedToken == null) return;

            var tokenVis = RenderCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(c => c.Tag is Token t && t.Id == SelectedToken.Id);
            if (tokenVis == null) return;
            if (_isDraggingToken) return;

            double secondsPerSquare = 1.0 / Math.Max(0.001, Options.PathSpeedSquaresPerSecond);

            int prevX = SelectedToken.GridX;
            int prevY = SelectedToken.GridY;

            for (int i = 0; i < _lastPreviewPath.Count; i++)
            {
                var step = _lastPreviewPath[i];
                double targetLeft = step.x * GridCellSize;
                double targetTop = step.y * GridCellSize;

                await AnimateTokenTo(tokenVis, targetLeft, targetTop, secondsPerSquare);

                // Resolve AOOs between prev cell and this step
                await ResolveAOOsForStep((prevX, prevY), (step.x, step.y), SelectedToken);

                // Update logical position after AOOs resolved
                SelectedToken.GridX = step.x;
                SelectedToken.GridY = step.y;

                prevX = step.x;
                prevY = step.y;
            }

            ClearPathVisual();
            RedrawMovementOverlay();
        }

        private Task AnimateTokenTo(FrameworkElement tokenVis, double targetLeft, double targetTop, double seconds)
        {
            var tcs = new TaskCompletionSource<object>();

            var leftAnim = new DoubleAnimation()
            {
                To = targetLeft,
                Duration = TimeSpan.FromSeconds(seconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            var topAnim = new DoubleAnimation()
            {
                To = targetTop,
                Duration = TimeSpan.FromSeconds(seconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            leftAnim.FillBehavior = FillBehavior.Stop;
            topAnim.FillBehavior = FillBehavior.Stop;

            int completedCount = 0;
            void OnCompleted(object s, EventArgs e)
            {
                completedCount++;
                if (completedCount >= 2)
                {
                    Canvas.SetLeft(tokenVis, targetLeft);
                    Canvas.SetTop(tokenVis, targetTop);
                    leftAnim.Completed -= OnCompleted;
                    topAnim.Completed -= OnCompleted;
                    tcs.SetResult(null);
                }
            }

            leftAnim.Completed += OnCompleted;
            topAnim.Completed += OnCompleted;

            tokenVis.BeginAnimation(Canvas.LeftProperty, leftAnim);
            tokenVis.BeginAnimation(Canvas.TopProperty, topAnim);

            return tcs.Task;
        }

        private async Task ResolveAOOsForStep((int x, int y) prevCell, (int x, int y) curCell, Token defender)
        {
            if (defender == null || Tokens == null) return;

            // Find enemies (opposite team) that were adjacent to prevCell and are NOT adjacent to curCell
            var enemies = Tokens.Where(t => t.Id != defender.Id && t.IsPlayer != defender.IsPlayer).ToList();
            var provokingEnemies = new List<Token>();

            foreach (var eToken in enemies)
            {
                bool prevAdj = AreCellsAdjacent(prevCell, (eToken.GridX, eToken.GridY));
                bool curAdj = AreCellsAdjacent(curCell, (eToken.GridX, eToken.GridY));
                if (prevAdj && !curAdj)
                    provokingEnemies.Add(eToken);
            }

            if (provokingEnemies.Count == 0) return;

            foreach (var attacker in provokingEnemies)
            {
                (string Name, int AttackBonus, string DamageExpression, string Range)? action = PickAttackAction(attacker);
                int attackBonus = action?.AttackBonus ?? attacker.InitiativeModifier;

                // roll d20
                var rollRes = DnDBattle.Utils.DiceRoller.RollExpression("d20");
                int d20 = rollRes.Individual.Count > 0 ? rollRes.Individual[0] : rollRes.Total;
                int attackTotal = d20 + attackBonus;

                bool isCritical = (d20 == 20);
                bool hit = isCritical || (attackTotal >= defender.ArmorClass);

                int damage = 0;
                string damageDetails = string.Empty;
                if (hit)
                {
                    string dmgExpr = action?.DamageExpression ?? "1d4";
                    // roll damage; on crit, roll dice portion twice (we simulate by two rolls and add modifier once)
                    var dmgRoll = DnDBattle.Utils.DiceRoller.RollExpression(dmgExpr);
                    damage = dmgRoll.Total;
                    if (isCritical)
                    {
                        // roll dice part again and add (simple approach)
                        var extra = DnDBattle.Utils.DiceRoller.RollExpression(dmgExpr);
                        damage += extra.Total;
                        damageDetails = $"{dmgRoll.Total} + {extra.Total} (critical)";
                    }
                    else damageDetails = $"{dmgRoll.Total}";
                }

                // Apply damage to defender (on UI thread)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    defender.HP = Math.Max(0, defender.HP - damage);
                });

                // Log result into MainViewModel.ActionLog if available
                var msg = $"{attacker.Name} makes an opportunity attack on {defender.Name}: d20={d20} + {attackBonus} => {attackTotal}. ";
                msg += hit ? $"Hit for {damage} ({damageDetails}). {defender.Name} HP now {defender.HP}." : "Missed.";
                AddToActionLog("AOO", msg);

                // small delay to stagger logs/animation feel
                await Task.Delay(250);
                // If defender dies, break
                if (defender.HP <= 0)
                {
                    AddToActionLog("AOO", $"{defender.Name} has been reduced to 0 HP.");
                    break;
                }
            }
        }

        private bool AreCellsAdjacent((int x, int y) a, (int x, int y) b)
        {
            int dx = Math.Abs(a.x  - b.x), dy = Math.Abs(a.y - b.y);
            return (dx + dy) == 1;
        }

        private (string name, int AttackBonus, string DamageExpression, string Range) PickAttackAction(Token attacker)
        {
            try
            {
                var prop = attacker.GetType().GetProperty("Action");
                if (prop != null)
                {
                    var actions = prop.GetValue(attacker) as System.Collections.IEnumerable;
                    if (actions != null)
                    {
                        foreach (var a in actions)
                        {
                            var rangeProp = a.GetType().GetProperty("Range");
                            var nameProp = a.GetType().GetProperty("Name");
                            var atkProp = a.GetType().GetProperty("AttackBonus");
                            var dmgProp = a.GetType().GetProperty("Damage");
                            var range = rangeProp?.GetValue(a)?.ToString() ?? "";
                            if (range.ToLower().Contains("melee") || string.IsNullOrWhiteSpace(range))
                            {
                                var nm = nameProp?.GetValue(a)?.ToString() ?? "Attack";
                                int atk = 0;
                                int.TryParse(atkProp?.GetValue(a)?.ToString() ?? "0", out atk);
                                var dmg = dmgProp?.GetValue(a)?.ToString() ?? "1d4";
                                return (nm, atk, dmg, range);
                            }
                        }

                    }
                }
            }
            catch { }
            return ("Melee Attack", attacker.InitiativeModifier, "1d4", "Melee");
        }

        private void AddToActionLog(string source, string message)
        {
            try
            {
                var mw = Application.Current?.MainWindow;
                if (mw != null && mw.DataContext is DnDBattle.ViewModels.MainViewModel vm)
                {
                    var entry = new DnDBattle.Models.ActionLogEntry { Source = source, Message = message, Timestamp = DateTime.Now };
                    vm.ActionLog.Insert(0, entry);
                }
            }
            catch
            {
                Debug.WriteLine("Failed to write action log entry: " + message);
            }
        }
        #endregion

        #region Save/Load encounter DTO helpers
        // Create encounter DTO to save (used by AutosaveService and EncounterService)
        public EncounterDto GetEncounterDto()
        {
            var dto = new EncounterDto();
            if (MapImage?.Source is BitmapImage bi && bi.UriSource != null)
                dto.MapImagePath = bi.UriSource.LocalPath;
            else dto.MapImagePath = null;

            if (Tokens != null)
            {
                foreach (var t in Tokens)
                {
                    dto.Tokens.Add(new TokenDto
                    {
                        Id = t.Id.ToString(),
                        Name = t.Name,
                        GridX = t.GridX,
                        GridY = t.GridY,
                        MaxHP = t.HP,
                        AC = t.ArmorClass,
                        Initiative = t.Initiative,
                        InitiativeMod = t.InitiativeModifier,
                        Speed = t.Speed,
                        IsPlayer = t.IsPlayer,
                        IconPath = (t.Image is BitmapImage b && b.UriSource != null) ? b.UriSource.LocalPath : null
                    });
                }
            }

            dto.Obstacles = _obstacles.Select(o => new ObstacleDto
            {
                Label = o.Label,
                Polygon = o.PolygonGridPoints.Select(p => new PointDto { X = p.X, Y = p.Y }).ToList()
            }).ToList();

            dto.Lights = _lights.Select(l => new LightDto
            {
                X = l.CenterGrid.X,
                Y = l.CenterGrid.Y,
                RadiusSquares = l.RadiusSquares,
                Intensity = l.Intensity
            }).ToList();

            return dto;
        }

        // Load from EncounterDto (replaces current tokens/obstacles/lights)
        public void LoadEncounterDto(EncounterDto dto)
        {
            // Map image
            if (!string.IsNullOrEmpty(dto.MapImagePath) && System.IO.File.Exists(dto.MapImagePath))
            {
                MapImage.Source = new BitmapImage(new Uri(dto.MapImagePath));
                MapImage.SetValue(Canvas.ZIndexProperty, -100);
            }

            // tokens: clear and add
            Tokens?.Clear();
            foreach (var td in dto.Tokens)
            {
                var token = new Token
                {
                    Name = td.Name,
                    HP = td.MaxHP,
                    ArmorClass = td.AC,
                    Initiative = td.Initiative,
                    InitiativeModifier = td.InitiativeMod,
                    Speed = td.Speed,
                    IsPlayer = td.IsPlayer,
                    GridX = td.GridX,
                    GridY = td.GridY
                };
                if (!string.IsNullOrEmpty(td.IconPath) && System.IO.File.Exists(td.IconPath))
                {
                    token.Image = new BitmapImage(new Uri(td.IconPath));
                }
                Tokens?.Add(token);
            }

            // obstacles
            _obstacles.Clear();
            foreach (var od in dto.Obstacles)
            {
                var o = new Obstacle { Label = od.Label };
                o.PolygonGridPoints = od.Polygon.Select(p => new Point(p.X, p.Y)).ToList();
                _obstacles.Add(o);
                _spatialIndex.IndexObstacle(o);
            }

            // lights
            _lights.Clear();
            foreach (var ld in dto.Lights)
            {
                _lights.Add(new LightSource { CenterGrid = new Point(ld.X, ld.Y), RadiusSquares = ld.RadiusSquares, Intensity = ld.Intensity });
            }

            RebuildTokenVisuals();
            RedrawObstacleDrawVisual();
            RedrawMovementOverlay();
            RedrawLighting();
        }
        #endregion

        #region Drag/drop support (Creature Bank prototypes and map images)
        public Point ScreenToWorldPublic(Point screenPoint)
        {
            var inv = _transformGroup.Inverse;
            if (inv != null) return inv.Transform(screenPoint);
            return screenPoint;
        }

        
        #endregion

        #region Utilities
        private Point ScreenToWorld(Point screenPt)
        {
            var inv = _transformGroup.Inverse;
            if (inv != null) return inv.Transform(screenPt);
            return screenPt;
        }

        #endregion
    }
}