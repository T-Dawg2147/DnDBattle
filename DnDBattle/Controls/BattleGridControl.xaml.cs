using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
                int gridX, gridY;
                if (LockToGrid)
                {
                    gridX = (int)Math.Round(left / GridCellSize);
                    gridY = (int)Math.Round(top / GridCellSize);
                    Canvas.SetLeft(_draggingVisual, gridX * GridCellSize);
                    Canvas.SetTop(_draggingVisual, gridY * GridCellSize);
                }
                else
                {
                    // keep pixel position but update token grid coords based on fractional position
                    gridX = (int)Math.Floor(left / GridCellSize);
                    gridY = (int)Math.Floor(top / GridCellSize);
                }

                if (_draggingVisual.Tag is Token token)
                {
                    token.GridX = gridX;
                    token.GridY = gridY;

                    if (token.GridX != _dragStartGridX || token.GridY != _dragStartGridY)
                    {
                        if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
                        {
                            var act = new TokenMoveAction(vm, token, _dragStartGridX, _dragStartGridY, token.GridX, token.GridY);
                            UndoManager.Record(act, performNow: false);
                            UndoManager.Record(act, performNow: true);
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
            var worldPt = ScreenToWorld(e.GetPosition(RenderCanvas));
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

        private void RenderCanvas_MouseMove(object sender, MouseEventArgs e)
        {
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
                var fillBrush = new SolidColorBrush(Color.FromArgb(90, 200, 100, 20)); fillBrush.Freeze();
                var stroke = new Pen(Brushes.Orange, 1.8); stroke.Freeze();

                foreach (var o in _obstacles)
                {
                    if (o.PolygonGridPoints.Count < 3) continue;
                    var pg = new PathGeometry();
                    var pf = new PathFigure { StartPoint = new Point(o.PolygonGridPoints[0].X * GridCellSize, o.PolygonGridPoints[0].Y * GridCellSize), IsClosed = true };
                    for (int i = 1; i < o.PolygonGridPoints.Count; i++) pf.Segments.Add(new LineSegment(new Point(o.PolygonGridPoints[i].X * GridCellSize, o.PolygonGridPoints[i].Y * GridCellSize), true));
                    pg.Figures.Add(pf);
                    dc.DrawGeometry(fillBrush, stroke, pg);

                    if (_selectedObstacle == o)
                    {
                        var handleBrush = new SolidColorBrush(Color.FromArgb(220, 255, 200, 0)); handleBrush.Freeze();
                        foreach (var v in o.PolygonGridPoints)
                        {
                            var r = new Rect(v.X * GridCellSize - 4, v.Y * GridCellSize - 4, 8, 8);
                            dc.DrawRectangle(handleBrush, new Pen(Brushes.Black, 1), r);
                        }
                    }
                }

                if (_currentDrawVertices.Count > 0)
                {
                    var pen = new Pen(Brushes.Yellow, 2); pen.Freeze();
                    var fg = new SolidColorBrush(Color.FromArgb(170, 255, 240, 120)); fg.Freeze();
                    var pg = new PathGeometry();
                    var pf = new PathFigure { StartPoint = new Point(_currentDrawVertices[0].X * GridCellSize + GridCellSize / 2, _currentDrawVertices[0].Y * GridCellSize + GridCellSize / 2), IsClosed = false };
                    for (int i = 1; i < _currentDrawVertices.Count; i++) pf.Segments.Add(new LineSegment(new Point(_currentDrawVertices[i].X * GridCellSize + GridCellSize / 2, _currentDrawVertices[i].Y * GridCellSize + GridCellSize / 2), true));
                    pg.Figures.Add(pf);
                    dc.DrawGeometry(null, pen, pg);

                    foreach (var v in _currentDrawVertices)
                    {
                        var rect = new Rect(v.X * GridCellSize, v.Y * GridCellSize, GridCellSize, GridCellSize);
                        dc.DrawRectangle(fg, new Pen(Brushes.Black, 1), rect);
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
                    // nothing to draw
                    return;
                }

                // Convert obstacles to pixel-space polygons for raycasting
                var obstaclePixelPolys = new List<Obstacle>();
                foreach (var o in _obstacles)
                {
                    var copy = new Obstacle { Id = o.Id, Label = o.Label };
                    copy.PolygonGridPoints = o.PolygonGridPoints.Select(p => new Point(p.X * GridCellSize + GridCellSize / 2.0, p.Y * GridCellSize + GridCellSize / 2.0)).ToList();
                    obstaclePixelPolys.Add(copy);
                }

                foreach (var light in _lights)
                {
                    var centerPixel = new Point(light.CenterGrid.X * GridCellSize + GridCellSize / 2.0, light.CenterGrid.Y * GridCellSize + GridCellSize / 2.0);
                    double radiusPx = Math.Max(1.0, light.RadiusSquares * GridCellSize);

                    // compute lit geometry using multi-sample raycasts
                    var litGeom = LightingService.ComputeLitGeometry(centerPixel, radiusPx, obstaclePixelPolys, Options.MaxRaycaseAngles);

                    // Build radial gradient brush for the light (transparent center -> faint dark near edge)
                    // We draw the gradient inside the lit region so the center is the most revealed.
                    var rg = new RadialGradientBrush();
                    rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.0)); // fully transparent center (revealed)
                    byte edgeAlpha = (byte)(200 * (1.0 - light.Intensity)); // edge darkness (lower intensity => brighter)
                    rg.GradientStops.Add(new GradientStop(Color.FromArgb(edgeAlpha, 0, 0, 0), 1.0));
                    rg.RadiusX = 1; rg.RadiusY = 1;
                    rg.Center = new Point(0.5, 0.5);
                    rg.GradientOrigin = new Point(0.5, 0.5);
                    rg.Freeze();

                    // Clip to lit geometry and draw gradient rectangle covering the circle
                    dc.PushClip(litGeom);
                    dc.PushTransform(new TranslateTransform(centerPixel.X - radiusPx, centerPixel.Y - radiusPx));
                    dc.DrawRectangle(rg, null, new Rect(0, 0, radiusPx * 2, radiusPx * 2));
                    dc.Pop();
                    dc.Pop();
                }
            }

            // update blur radius in wrapper (soften edges the user can tune)
            var lightingWrapper = RenderCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(e => e.Tag as string == "Overlay" && Panel.GetZIndex(e) == 50);
            if (lightingWrapper != null)
            {
                lightingWrapper.Effect = new BlurEffect { Radius = Options.ShadowSoftnessPx, RenderingBias = RenderingBias.Quality };
            }
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