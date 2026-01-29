using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.Utils;
using DnDBattle.ViewModels;
using DnDBattle.Views;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DnDBattle.Controls
{
    public partial class BattleGridControl : UserControl
    {
        public event Action<Token> TokenDoubleClicked;
        public event Action<Token> RequestDeleteToken;
        public event Action<Token> RequestDuplicateToken;
        public event Action<Token> RequestEditToken;
        public event Action<Point> RequestAddTokenAtPosition;
        public event Action<Token> TokenAddedToMap;
        public event Action<Token> TargetSelected;

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

        public static readonly DependencyProperty ShowGridProperty =
            DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(BattleGridControl),
                new PropertyMetadata(true, OnGridVisualPropertyChanged));

        public static readonly DependencyProperty ShowCoordinatesProperty =
            DependencyProperty.Register(nameof(ShowCoordinates), typeof(bool), typeof(BattleGridControl),
                new PropertyMetadata(true, OnGridVisualPropertyChanged));

        public double GridCellSize { get => (double)GetValue(GridCellSizeProperty); set => SetValue(GridCellSizeProperty, value); }
        public System.Collections.ObjectModel.ObservableCollection<Token> Tokens { get => (System.Collections.ObjectModel.ObservableCollection<Token>)GetValue(TokensProperty); set => SetValue(TokensProperty, value); }
        public ImageSource MapImageSource { get => (ImageSource)GetValue(MapImageSourceProperty); set => SetValue(MapImageSourceProperty, value); }
        public Token SelectedToken { get => (Token)GetValue(SelectedTokenProperty); set => SetValue(SelectedTokenProperty, value); }
        public bool LockToGrid { get => (bool)GetValue(LockToGridProperty); set => SetValue(LockToGridProperty, value); }
        public bool ShowGrid { get => (bool)GetValue(ShowGridProperty); set => SetValue(ShowGridProperty, value); }
        public bool ShowCoordinates { get => (bool)GetValue(ShowCoordinatesProperty); set => SetValue(ShowCoordinatesProperty, value); }
        public bool ShowCoordinatesRulers { get => _showCoordinateRulers; set { _showCoordinateRulers = value; DrawCoordinateRulers(); } }
        public AreaEffectService AreaEffectService => _areaEffectService;
        public FogOfWarService FogService => _fogService;

        #region States
        // internal state
        private readonly SpatialIndex _spatialIndex = new SpatialIndex(1);
        private readonly List<LightSource> _lights = new List<LightSource>();

        private readonly DrawingVisual _movementVisual = new DrawingVisual();
        private readonly DrawingVisual _pathVisual = new DrawingVisual();
        private readonly DrawingVisual _lightingVisual = new DrawingVisual();

        private readonly WallService _wallService = new WallService();
        private readonly DrawingVisual _wallVisual = new DrawingVisual();

        private TranslateTransform _pan = new TranslateTransform();
        private ScaleTransform _zoom = new ScaleTransform(1, 1);
        private TransformGroup _transformGroup = new TransformGroup();
        private DispatcherTimer _refreshThrottleTimer;
        private bool _refreshPending;
        private const double RefreshThrottleMs = 16;

        private bool _isPanning = false;
        private Point _lastPanPoint;

        #region Grid fields
        // Grid State
        private int _gridMinX = 0;
        private int _gridMinY = 0;
        private int _gridMaxX = 100;
        private int _gridMaxY = 100;

        // Cache for movement overlay to avoid recalculating during pan/zoom
        private HashSet<(int x, int y)> _cachedReachableSquares;
        private Guid? _cachedReachableTokenId;
        private int _cachedReachableStartX;
        private int _cachedReachableStartY;
        private int _cachedReachableMaxSquares;
        private int _cachedReachableWallVersion;

        // Track wall changes for cache invalidation
        private int _wallVersion = 0;
        #endregion

        #region Token Fields
        // Token Visual Lookup
        private readonly Dictionary<Guid, Grid> _tokenVisualLookup = new();

        //Hp Bar
        private static readonly SolidColorBrush HPBarBackgroundBrush;
        private static readonly SolidColorBrush HPBarGreenBrush;
        private static readonly SolidColorBrush HPBarYellowBrush;
        private static readonly SolidColorBrush HPBarRedBrush;

        #endregion

        #region Cached Resources

        // Wall type pens - created once, reused forever
        private static readonly Pen WallSolidPen;
        private static readonly Pen WallDoorPen;
        private static readonly Pen WallDoorOpenPen;
        private static readonly Pen WallWindowPen;
        private static readonly Pen WallHalfwallPen;
        private static readonly Pen WallSelectedPen;

        // Shadow pens for each wall thickness
        private static readonly Pen WallShadowPen6;  // For solid/door walls (thickness 6)
        private static readonly Pen WallShadowPen4;  // For window/halfwall/open door (thickness 4)

        // Handle drawing
        private static readonly Pen WallHandlePen;

        // Origin point marker
        private static readonly Pen AreaEffectOriginPen;

        // Label background
        private static readonly SolidColorBrush AreaEffectLabelBackgroundBrush;

        // Preview pen dash style (shared)
        private static readonly DashStyle PreviewDashStyle = DashStyles.Dash;

        // Simple cache for effect brushes by color (limited size)
        private readonly Dictionary<Color, (SolidColorBrush fill, Pen outline, Pen previewOutline)> _effectBrushCache = new();
        private const int MaxEffectBrushCacheSize = 20;

        // Movement overlay
        private static readonly SolidColorBrush MovementOverlayBrush;
        private static readonly Pen MovementOverlayBorderPen;

        // Path preview
        private static readonly Pen PathPreviewLinePen;
        private static readonly SolidColorBrush PathStepBrush;
        private static readonly SolidColorBrush PathAOOBrush;
        private static readonly Pen PathAOOPen;

        // Measurement line
        private static readonly Pen MeasureLinePen;
        private static readonly Pen MeasureShadowPen;
        private static readonly Pen MeasureEndpointPen;

        // Measurement label
        private static readonly SolidColorBrush MeasureLabelBackgroundBrush;
        private static readonly Pen MeasureLabelBorderPen;

        #endregion

        // AOE State
        private readonly AreaEffectService _areaEffectService = new AreaEffectService();
        private readonly DrawingVisual _areaEffectVisual = new DrawingVisual();
        private AreaEffect _previewEffect;
        private bool _isPlacingAreaEffect;
        private AreaEffectShape? _currentAoeShape;
        private int _currentAoeSize = 20;
        private Color _currentAoeColor = Color.FromArgb(120, 255, 69, 0);

        // Wall Drawing State
        private bool _wallDrawMode = false;
        private Point? _wallDrawStart = null;
        private Point _wallDrawPreview;
        private WallType _currentWallType = WallType.Solid;
        private Wall _selectedWall = null;
        private bool _isDraggingWallEndpoint = false;
        private bool _draggingWallIsStart = false;

        private bool _roomDrawMode = false;
        private List<Point> _roomVertices = new List<Point>();

        #region Lighting
        // Dirty Light Caching
        private bool _lightingDirty = true;
        private int _lightingWallVersion = -1;
        private int _lightingLightCount = -1;

        // Cached Light Gradient Brushes
        private static readonly object _lightGradientCacheLock = new();

        // Cached brush for light center indicator
        private static readonly SolidColorBrush LightCenterBrush;
        private static readonly Pen LightCenterPen;

        // Light source indicator
        private static readonly Pen LightIndicatorPen;

        // Cache for light gradients by intensity (most lights use standard intensities)
        private readonly Dictionary<double, RadialGradientBrush> _lightGradientCache = new();
        private const int MaxLightGradientCacheSize = 10;

        #endregion

        #region Ruler
        // Measurement Tool State
        private bool _measureMode = false;
        private Point? _measureStart = null;
        private Point _measureEnd;
        private readonly DrawingVisual _measureVisual = new DrawingVisual();
        private bool _showCoordinateRulers = true;

        // Ruler brushes
        private static readonly SolidColorBrush RulerBackgroundBrush;
        private static readonly SolidColorBrush RulerTextBrush;
        private static readonly SolidColorBrush RulerLineBrush;
        private static readonly FontFamily RulerFontFamily;

        // Reusable background rectangles (updated instead of recreated)
        private System.Windows.Shapes.Rectangle _topRulerBackground;
        private System.Windows.Shapes.Rectangle _leftRulerBackground;

        // Coordinates Ruler Pooling fields
        private readonly List<TextBlock> _colLabelPool = new();
        private readonly List<TextBlock> _rowLabelPool = new();
        private readonly List<System.Windows.Shapes.Line> _colTickPool = new();
        private readonly List<System.Windows.Shapes.Line> _rowTickPool = new();
        private System.Windows.Shapes.Rectangle _topRulerBg;
        private System.Windows.Shapes.Rectangle _leftRulerBg;
        private bool _rulerPoolsInitialized;

        // Cached brushes for rulers
        private static readonly SolidColorBrush RulerBgBrush = new(Color.FromRgb(37, 37, 38));

        #endregion

        public WallService WallService => _wallService;        

        // token drag
        private bool _isDraggingToken = false;
        private FrameworkElement _draggingVisual;
        private Point _dragOrigin;

        // Path preview state
        private List<(int x, int y)> _lastPreviewPath = null;
        private HashSet<int> _lastAooIndices = null;
        private DateTime _lastPathCalculation = DateTime.MinValue;
        private readonly TimeSpan _pathCalculationDebounce = TimeSpan.FromMilliseconds(50);

        // Path caching for performance
        private (int x, int y)? _cachedPathStart = null;
        private (int x, int y)? _cachedPathGoal = null;
        private int _cachedPathWallVersion = -1;

        // visuals sizing
        private int _gridWidth = 200, _gridHeight = 200;
        private int _dragStartGridX = 0;
        private int _dragStartGridY = 0;

        // drop events
        private DateTime _lastDropTime = DateTime.MinValue;
        private string _lastDropPrototypeId = null;
        private readonly TimeSpan _duplicateDropThreshold = TimeSpan.FromMilliseconds(300);

        // targeting fields
        private bool _isInTargetingMode = false;
        private TargetingState _currentTargetingState;
        private Dictionary<Token, Border> _targetHighlights = new Dictionary<Token, Border>();

        // Fog of war fields
        private FogOfWarService _fogService;
        private Canvas _fogCanvas;
        private bool _isFogBrushActive = false;
        private FogShapeTool _currentFogShapeTool = FogShapeTool.None;
        private Point? _fogShapeStartPoint;

        // Cached Image field
        private static ImageSource _cachedDefaultTokenImage;
        private static readonly object _defaultImageLock = new object();
        private static readonly Dictionary<string, ImageSource> _tokenImageCache = new Dictionary<string, ImageSource>();
        private static readonly object _tokenImageLock = new object();
        #endregion

        static BattleGridControl()
        {
            HPBarBackgroundBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            HPBarGreenBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            HPBarYellowBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            HPBarRedBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));

            HPBarBackgroundBrush.Freeze();
            HPBarGreenBrush.Freeze();
            HPBarYellowBrush.Freeze();
            HPBarRedBrush.Freeze();

            // Light center indicator
            LightCenterBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 150));
            LightCenterBrush.Freeze();

            LightCenterPen = new Pen(Brushes.Orange, 2);
            LightCenterPen.Freeze();

            // Lighting resources
            LightIndicatorPen = new Pen(Brushes.Orange, 2);
            LightIndicatorPen.Freeze();

            // Initialize wall pens
            var solidBrush = new SolidColorBrush(Color.FromArgb(255, 139, 90, 43));
            solidBrush.Freeze();
            WallSolidPen = new Pen(solidBrush, 6) { DashStyle = DashStyles.Solid };
            WallSolidPen.Freeze();

            var doorBrush = new SolidColorBrush(Color.FromArgb(255, 101, 67, 33));
            doorBrush.Freeze();
            WallDoorPen = new Pen(doorBrush, 6) { DashStyle = DashStyles.Solid };
            WallDoorPen.Freeze();

            var doorOpenBrush = new SolidColorBrush(Color.FromArgb(150, 101, 67, 33));
            doorOpenBrush.Freeze();
            WallDoorOpenPen = new Pen(doorOpenBrush, 4) { DashStyle = DashStyles.Dash };
            WallDoorOpenPen.Freeze();

            var windowBrush = new SolidColorBrush(Color.FromArgb(200, 135, 206, 235));
            windowBrush.Freeze();
            WallWindowPen = new Pen(windowBrush, 4) { DashStyle = DashStyles.DashDot };
            WallWindowPen.Freeze();

            var halfwallBrush = new SolidColorBrush(Color.FromArgb(180, 169, 169, 169));
            halfwallBrush.Freeze();
            WallHalfwallPen = new Pen(halfwallBrush, 4) { DashStyle = DashStyles.Dot };
            WallHalfwallPen.Freeze();

            WallSelectedPen = new Pen(Brushes.Yellow, 3);
            WallSelectedPen.Freeze();

            // Shadow pens
            var shadowBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
            shadowBrush.Freeze();
            WallShadowPen6 = new Pen(shadowBrush, 6);
            WallShadowPen6.Freeze();
            WallShadowPen4 = new Pen(shadowBrush, 4);
            WallShadowPen4.Freeze();

            // Handle pen
            WallHandlePen = new Pen(Brushes.Black, 2);
            WallHandlePen.Freeze();

            // Area effect resources
            AreaEffectOriginPen = new Pen(Brushes.Black, 1);
            AreaEffectOriginPen.Freeze();

            AreaEffectLabelBackgroundBrush = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
            AreaEffectLabelBackgroundBrush.Freeze();

            // Movement overlay resources
            MovementOverlayBrush = new SolidColorBrush(Color.FromArgb(80, 30, 144, 255));
            MovementOverlayBrush.Freeze();

            var movementBorderBrush = new SolidColorBrush(Color.FromArgb(150, 30, 144, 255));
            movementBorderBrush.Freeze();
            MovementOverlayBorderPen = new Pen(movementBorderBrush, 1);
            MovementOverlayBorderPen.Freeze();

            // Path preview resources
            PathPreviewLinePen = new Pen(Brushes.LightBlue, 2);
            PathPreviewLinePen.Freeze();

            PathStepBrush = new SolidColorBrush(Color.FromArgb(200, 100, 180, 255));
            PathStepBrush.Freeze();

            PathAOOBrush = new SolidColorBrush(Color.FromArgb(220, 220, 50, 50));
            PathAOOBrush.Freeze();

            PathAOOPen = new Pen(PathAOOBrush, 3);
            PathAOOPen.Freeze();

            // Measurement resources
            MeasureLinePen = new Pen(Brushes.Yellow, 3) { DashStyle = DashStyles.Dash };
            MeasureLinePen.Freeze();

            var measureShadowBrush = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
            measureShadowBrush.Freeze();
            MeasureShadowPen = new Pen(measureShadowBrush, 5);
            MeasureShadowPen.Freeze();

            MeasureEndpointPen = new Pen(Brushes.Black, 2);
            MeasureEndpointPen.Freeze();

            MeasureLabelBackgroundBrush = new SolidColorBrush(Color.FromArgb(220, 30, 30, 30));
            MeasureLabelBackgroundBrush.Freeze();

            MeasureLabelBorderPen = new Pen(Brushes.Yellow, 2);
            MeasureLabelBorderPen.Freeze();

            // Ruler resources
            RulerBackgroundBrush = new SolidColorBrush(Color.FromRgb(37, 37, 38));
            RulerBackgroundBrush.Freeze();

            RulerTextBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            RulerTextBrush.Freeze();

            RulerLineBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            RulerLineBrush.Freeze();

            RulerFontFamily = new FontFamily("Segoe UI");
        }

        public BattleGridControl()
        {
            InitializeComponent();

            _refreshThrottleTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(RefreshThrottleMs)
            };
            _refreshThrottleTimer.Tick += (s, e) =>
            {
                _refreshThrottleTimer.Stop();
                if (_refreshPending)
                {
                    _refreshPending = false;
                    RefreshAllVisualsImmediate();
                }
            };

            GridCellSize = Options.DefaultGridCellSize;
            SetGridMaxSize(Options.GridMaxWidth, Options.GridMaxHeight);
            UpdateShadowSoftness();

            _transformGroup.Children.Add(_zoom);
            _transformGroup.Children.Add(_pan);
            RenderCanvas.RenderTransform = _transformGroup;

            AddVisualOverlay(_lightingVisual, 50, makeBlur: true);
            AddVisualOverlay(_movementVisual, 60);
            AddVisualOverlay(_pathVisual, 65);
            AddVisualOverlay(_wallVisual, 70);
            AddVisualOverlay(_measureVisual, 80);
            AddVisualOverlay(_areaEffectVisual, 85);

            RulerBgBrush.Freeze();
            RulerTextBrush.Freeze();
            RulerLineBrush.Freeze();

            _wallService.WallsChanged += () =>
            {
                _wallVersion++;
                _lightingDirty = true;    // Mark lighting as needing recalculation
                RedrawWalls();
            };

            Loaded += BattleGridControl_Loaded;
            KeyDown += BattleGridControl_KeyDown;

            MouseDown += (s, e) => Focus();

            SizeChanged += (s, e) =>
            {
                RefreshAllVisualsImmediate();
            };

            RenderCanvas.AllowDrop = true;
        }

        private void BattleGridControl_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== BattleGridControl_Loaded ===");
            System.Diagnostics.Debug.WriteLine($"RenderCanvas: {(RenderCanvas == null ? "NULL" : "OK")}");
            System.Diagnostics.Debug.WriteLine($"Tokens: {(Tokens == null ? "NULL" : $"Count={Tokens.Count}")}");
            System.Diagnostics.Debug.WriteLine($"DataContext: {DataContext?.GetType().Name ?? "NULL"}");

            UpdateGridVisual();
            RebuildTokenVisuals();
            RedrawLighting();
            RedrawWalls();

            if (DataContext is MainViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel found, wiring refresh event");
                vm.RequestTokenVisualsRefresh += () =>
                {
                    Dispatcher.Invoke(() => RebuildTokenVisuals());
                };
            }

            Focusable = true;
            MouseDown += (s, e) => Focus();
        }

        public void InitializeFogOfWar()
        {
            _fogService = new FogOfWarService();
            _fogService.FogChanged += OnFogChanged;
            _fogService.LogMessage += (msg) => Debug.WriteLine(msg);

            int gridWidth = (int)(ActualWidth / GridCellSize) + 10;
            int gridHeight = (int)(ActualHeight / GridCellSize) + 10;
            _fogService.Initialize(Math.Max(50, gridWidth), Math.Max(50, gridHeight), GridCellSize);
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
            ctrl.GridHost.InvalidateGrid();
            ctrl.UpdateGridVisual();
            ctrl.LayoutTokens();
            ctrl.RedrawLighting();
            ctrl.RedrawMovementOverlay();
            ctrl.RedrawPathVisual();
            ctrl.RedrawWalls();
        }

        private static void OnGridVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (BattleGridControl)d;
            ctrl.GridHost.InvalidateGrid();
            ctrl.UpdateGridVisual();
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
            ctrl.InvalidateReachableCache();
            ctrl.InvalidatePathCache();
            ctrl.ClearPathVisual();
            ctrl.RedrawMovementOverlay();
        }
        #endregion

        #region Handler Events
        private void Token_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var visual = sender as FrameworkElement;
            if (visual?.Tag is Token clickedToken)
            {
                // Check if we're in targeting mode
                if (_isInTargetingMode)
                {
                    // Fire the target selected event
                    TargetSelected?.Invoke(clickedToken);
                    e.Handled = true;
                    return;
                }

                // Normal selection/drag behavior
                _draggingVisual = visual;
                _dragStartGridX = clickedToken.GridX;
                _dragStartGridY = clickedToken.GridY;

                // Set the selected token
                SelectedToken = clickedToken;

                _isDraggingToken = true;
                _dragOrigin = e.GetPosition(RenderCanvas);
                _draggingVisual?.CaptureMouse();
                e.Handled = true;
            }
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

        private void BattleGridControl_KeyDown(object sender, KeyEventArgs e)
        {
            // Calculate pan amount based on current zoom level
            double basePanAmount = GridCellSize * 2; // Pan by 2 grid cells

            // Hold Shift for faster panning (5 cells)
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                basePanAmount = GridCellSize * 5;
            }

            double panAmount = basePanAmount * _zoom.ScaleX;
            bool handled = true;

            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    _pan.X += panAmount;
                    break;
                case Key.Right:
                case Key.D:
                    _pan.X -= panAmount;
                    break;
                case Key.Up:
                case Key.W:
                    _pan.Y += panAmount;
                    break;
                case Key.Down:
                case Key.S:
                    _pan.Y -= panAmount;
                    break;
                case Key.Home:
                    // Reset view to origin (cell A1 at top-left)
                    _pan.X = 0;
                    _pan.Y = 0;
                    _zoom.ScaleX = 1;
                    _zoom.ScaleY = 1;
                    break;
                case Key.Add:
                case Key.OemPlus:
                    ZoomAtCenter(1.2);
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    ZoomAtCenter(1.0 / 1.2);
                    break;
                case Key.Delete:
                    // Delete selected token
                    if (SelectedToken != null)
                    {
                        RequestDeleteToken?.Invoke(SelectedToken);
                    }
                    break;
                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                ClampPanToBoundaries();
                RefreshAllVisualsImmediate();
                e.Handled = true;
            }
        }

        public void HandleKeyDown(Key key)
        {
            if (key == Key.Escape && _isInTargetingMode)
            {
                ExitTargetingMode();
            }
        }

        private void ZoomAtCenter(double zoomFactor)
        {
            var centerX = ActualWidth / 2;
            var centerY = ActualHeight / 2;

            double absX = centerX * _zoom.ScaleX + _pan.X;
            double absY = centerY * _zoom.ScaleY + _pan.Y;

            _zoom.ScaleX *= zoomFactor;
            _zoom.ScaleY *= zoomFactor;

            // Clamp zoom
            _zoom.ScaleX = Math.Max(0.1, Math.Min(5.0, _zoom.ScaleX));
            _zoom.ScaleY = Math.Max(0.1, Math.Min(5.0, _zoom.ScaleY));

            _pan.X = absX - centerX * _zoom.ScaleX;
            _pan.Y = absY - centerY * _zoom.ScaleY;
        }

        // Add these event handlers that redirect to the RenderCanvas logic

        private void GridBackground_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();

            var pos = e.GetPosition(RenderCanvas);
            var worldPt = ScreenToWorld(pos);
            var gridPoint = new Point(worldPt.X / GridCellSize, worldPt.Y / GridCellSize);

            // Area Effect Placement
            if (_isPlacingAreaEffect && _previewEffect != null)
            {
                if (_previewEffect.Shape == AreaEffectShape.Cone || _previewEffect.Shape == AreaEffectShape.Line)
                {
                    // First click sets origin, wait for second click for direction
                    if (_previewEffect.Origin == default)
                    {
                        _previewEffect.Origin = gridPoint;
                        RedrawAreaEffects();
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        // Second click places the effect
                        _previewEffect.DirectionAngle = CalculateAngle(_previewEffect.Origin, gridPoint);
                        PlaceAreaEffect();
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    // Single click placement for spheres, cubes
                    _previewEffect.Origin = gridPoint;
                    PlaceAreaEffect();
                    e.Handled = true;
                    return;
                }
            }
            // Measurement mode
            if (_measureMode)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));

                _measureStart = gridPoint;
                _measureEnd = gridPoint;
                RedrawMeasureVisual();
                e.Handled = true;
                return;
            }

            // Wall drawing mode
            if (_wallDrawMode)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));

                HandleWallDrawClick(gridPoint, e);
                e.Handled = true;
                return;
            }

            // Room drawing mode
            if (_roomDrawMode)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                HandleRoomDrawClick(gridPoint, e);
                e.Handled = true;
                return;
            }

            // Check for wall selection/interaction
            HandleWallSelection(gridPoint);
            if (_selectedWall != null)
            {
                e.Handled = true;
                return;
            }

            // Ctrl+Click for path preview
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (SelectedToken != null)
                {
                    var targetCell = (x: (int)Math.Floor(gridPoint.X), y: (int)Math.Floor(gridPoint.Y));
                    ComputeAndDrawPathPreview(targetCell);
                }
                e.Handled = true;
                return;
            }

            // Start panning (on empty space)
            _isPanning = true;
            _lastPanPoint = e.GetPosition(GridBackground);
            Cursor = Cursors.Hand;
            GridBackground.CaptureMouse();
            e.Handled = true;
        }

        private void GridBackground_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingWallEndpoint)
            {
                _isDraggingWallEndpoint = false;
                AddToActionLog("Wall", "Moved wall endpoint");
                return;
            }

            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Arrow;
                GridBackground.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void GridBackground_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(RenderCanvas);
            var worldPt = ScreenToWorld(pos);
            var gridPoint = new Point(worldPt.X / GridCellSize, worldPt.Y / GridCellSize);

            // Update status bar with current cell
            UpdateCurrentCellDisplay(gridPoint);

            // AOE preview
            if (_isPlacingAreaEffect && _previewEffect != null)
            {
                if (_previewEffect.Shape == AreaEffectShape.Cone || _previewEffect.Shape == AreaEffectShape.Line)
                {
                    if (_previewEffect.Origin != default)
                    {
                        // Update direction
                        _previewEffect.DirectionAngle = CalculateAngle(_previewEffect.Origin, gridPoint);
                    }
                    else
                    {
                        // Update origin position
                        _previewEffect.Origin = gridPoint;
                    }
                }
                else
                {
                    // Update position for spheres, cubes
                    _previewEffect.Origin = gridPoint;
                }

                RedrawAreaEffects();
            }

            // Measurement preview
            if (_measureMode && _measureStart.HasValue)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                _measureEnd = gridPoint;
                RedrawMeasureVisual();
            }

            // Wall drawing preview
            if (_wallDrawMode && _wallDrawStart.HasValue)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                _wallDrawPreview = gridPoint;
                RedrawWalls();
            }

            // Wall endpoint dragging
            if (_isDraggingWallEndpoint && _selectedWall != null)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));

                if (_draggingWallIsStart)
                    _selectedWall.StartPoint = gridPoint;
                else
                    _selectedWall.EndPoint = gridPoint;

                RedrawWalls();
                RedrawLighting();
                return;
            }

            // Panning
            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                var pt = e.GetPosition(GridBackground);
                var dx = pt.X - _lastPanPoint.X;
                var dy = pt.Y - _lastPanPoint.Y;
                _pan.X += dx;
                _pan.Y += dy;
                _lastPanPoint = pt;

                ClampPanToBoundaries();
                RefreshAllVisualsImmediate();
                e.Handled = true;
            }

            // Middle mouse panning
            if (_isMiddlePanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                var pt = e.GetPosition(GridBackground);
                var dx = pt.X - _middlePanLast.X;
                var dy = pt.Y - _middlePanLast.Y;
                _pan.X += dx;
                _pan.Y += dy;
                _middlePanLast = pt;

                ClampPanToBoundaries();
                RefreshAllVisualsImmediate();
                e.Handled = true;
            }
        }

        private void GridBackground_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = (e.Delta > 0) ? 1.15 : 1.0 / 1.15;
            var pos = e.GetPosition(RenderCanvas);

            // Calculate zoom centered on mouse position
            double absX = pos.X * _zoom.ScaleX + _pan.X;
            double absY = pos.Y * _zoom.ScaleY + _pan.Y;

            _zoom.ScaleX *= zoomFactor;
            _zoom.ScaleY *= zoomFactor;

            // Clamp zoom level
            _zoom.ScaleX = Math.Max(0.2, Math.Min(4.0, _zoom.ScaleX));
            _zoom.ScaleY = Math.Max(0.2, Math.Min(4.0, _zoom.ScaleY));

            _pan.X = absX - pos.X * _zoom.ScaleX;
            _pan.Y = absY - pos.Y * _zoom.ScaleY;

            ClampPanToBoundaries();
            RefreshAllVisualsImmediate();
            e.Handled = true;
        }

        private void GridBackground_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(RenderCanvas);
            var worldPt = ScreenToWorld(pos);
            var gridPoint = new Point(worldPt.X / GridCellSize, worldPt.Y / GridCellSize);

            // Cancel AOE placement
            if (_isPlacingAreaEffect)
            {
                CancelAreaEffectPlacement();
                e.Handled = true;
                return;
            }

            // Cancel wall drawing
            if (_wallDrawMode)
            {
                _wallDrawStart = null;
                RedrawWalls();
                e.Handled = true;
                return;
            }

            // Cancel room drawing
            if (_roomDrawMode)
            {
                _roomVertices.Clear();
                _roomDrawMode = false;
                Cursor = Cursors.Arrow;
                RedrawWalls();
                e.Handled = true;
                return;
            }

            // Delete selected wall
            if (_selectedWall != null)
            {
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

            // Cancel measurement
            if (_measureMode && _measureStart.HasValue)
            {
                _measureStart = null;
                _measureEnd = new Point();
                RedrawMeasureVisual();
                e.Handled = true;
                return;
            }
        }

        private void GridBackground_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isMiddlePanning = true;
                _middlePanLast = e.GetPosition(GridBackground);
                Cursor = Cursors.Hand;
                GridBackground.CaptureMouse();
                e.Handled = true;
            }
        }

        private void GridBackground_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && _isMiddlePanning)
            {
                _isMiddlePanning = false;
                Cursor = Cursors.Arrow;
                GridBackground.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Requests a visual refresh. Throttled to ~60 FPS to prevent lag during pan/zoom.
        /// </summary>
        private void RefreshAllVisuals()
        {
            if (_refreshThrottleTimer.IsEnabled)
            {
                // Timer is running, just mark that we need a refresh
                _refreshPending = true;
                return;
            }

            // No timer running: do immediate refresh and start the throttle window
            RefreshAllVisualsImmediate();
            _refreshThrottleTimer.Start();
        }

        /// <summary>
        /// Helper to refresh all visual elements after pan/zoom
        /// </summary>
        private void RefreshAllVisualsImmediate()
        {
            UpdateGridVisual();
            LayoutTokens();
            RedrawMovementOverlay();
            RedrawPathVisual();
            RedrawWalls();
            DrawCoordinateRulers();
        }

        /// <summary>
        /// Updates the status bar with current cell coordinates
        /// </summary>
        private void UpdateCurrentCellDisplay(Point gridPoint)
        {
            int cellX = (int)Math.Floor(gridPoint.X);
            int cellY = (int)Math.Floor(gridPoint.Y);

            // Only show valid cells (non-negative)
            if (cellX >= _gridMinX && cellY >= _gridMinY && cellX < _gridMaxX && cellY < _gridMaxY)
            {
                string colLabel = GetColumnLabel(cellX);
                CurrentCellText = $"{colLabel}{cellY + 1}"; // 1-based row
            }
            else
            {
                CurrentCellText = "-";
            }
        }

        // Add this property for the status bar
        private string _currentCellText = "-";
        public string CurrentCellText
        {
            get => _currentCellText;
            set
            {
                if (_currentCellText != value)
                {
                    _currentCellText = value;
                    CurrentCellChanged?.Invoke(value);
                }
            }
        }

        public event Action<string> CurrentCellChanged;
        #endregion

        public void RebuildTokenVisuals()
        {
            System.Diagnostics.Debug.WriteLine($"=== RebuildTokenVisuals called ===");

            // Clear the lookup dictionary
            _tokenVisualLookup.Clear();

            // Remove existing token visuals
            var toRemove = new List<UIElement>(RenderCanvas.Children.Count);
            foreach (UIElement child in RenderCanvas.Children)
            {
                if (child is FrameworkElement fe && fe.Tag is Token)
                {
                    toRemove.Add(child);
                }
            }

            foreach (var child in toRemove)
            {
                RenderCanvas.Children.Remove(child);
            }

            if (Tokens == null) return;

            foreach (var token in Tokens)
            {
                try
                {
                    // Subscribe to HP changes to update the visual
                    token.PropertyChanged -= Token_PropertyChanged;
                    token.PropertyChanged += Token_PropertyChanged;

                    var container = new Grid()
                    {
                        Width = GridCellSize * token.SizeInSquares + 8,
                        Height = GridCellSize * token.SizeInSquares + 8,
                        Tag = token,
                        Background = Brushes.Transparent
                    };

                    // ... rest of token visual creation code unchanged ...

                    RenderCanvas.Children.Add(container);
                    Canvas.SetZIndex(container, token.IsCurrentTurn ? 150 : 100);

                    // ✅ Register in lookup dictionary for O(1) access later
                    _tokenVisualLookup[token.Id] = container;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR creating token visual for {token.Name}: {ex.Message}");
                }
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

                var conditionsPanel = new WrapPanel();
                foreach (var condition in token.Conditions.GetActiveConditions())
                {
                    conditionsPanel.Children.Add(new TextBlock
                    {
                        Text = $"{ConditionExtensions.GetConditionIcon(condition)} ",
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                var condText = new TextBlock
                {
                    Text = token.ConditionsDisplay,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var condStack = new StackPanel { Orientation = Orientation.Horizontal };
                condStack.Children.Add(conditionsPanel);
                condStack.Children.Add(condText);

                conditionsBorder.Child = condStack;
                stack.Children.Add(conditionsBorder);
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
            // Return cached image if we already have it
            if (_cachedDefaultTokenImage != null)
                return _cachedDefaultTokenImage;

            // Thread-safe initialization
            lock (_defaultImageLock)
            {
                // Double-check after acquiring lock
                if (_cachedDefaultTokenImage != null)
                    return _cachedDefaultTokenImage;

                _cachedDefaultTokenImage = CreateFallbackTokenImage();
                System.Diagnostics.Debug.WriteLine("Generated default token image (cached)");

                return _cachedDefaultTokenImage;
            }
        }

        private static ImageSource CreateFallbackTokenImage()
        {
            // Create a simple circle as the default token image
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Draw a circle with gradient - nice blue theme
                var gradientBrush = new RadialGradientBrush(
                    Color.FromRgb(100, 149, 237),  // Cornflower blue center
                    Color.FromRgb(65, 105, 225));   // Royal blue edge
                gradientBrush.Freeze();

                var pen = new Pen(Brushes.White, 2);
                pen.Freeze();

                dc.DrawEllipse(gradientBrush, pen, new Point(24, 24), 22, 22);

                // Add a question mark
                var text = new FormattedText(
                    "?",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    20,
                    Brushes.White,
                    1.0);
                dc.DrawText(text, new Point(24 - text.Width / 2, 24 - text.Height / 2));
            }

            var rtb = new RenderTargetBitmap(48, 48, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();

            return rtb;
        }

        private static ImageSource GetOrCreateTokenImage(Token token)
        {
            // Determine cache key based on creature type (or use "default" if none)
            string cacheKey = string.IsNullOrEmpty(token?.Type) ? "default" : token.Type.ToLowerInvariant().Trim();

            // Check cache first
            lock (_tokenImageLock)
            {
                if (_tokenImageCache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            // Generate new image for this type
            var image = CreateTokenImage(token);

            // Cache it
            lock (_tokenImageLock)
            {
                _tokenImageCache[cacheKey] = image;
            }

            System.Diagnostics.Debug.WriteLine($"Generated token image for type: {cacheKey}");
            return image;
        }

        private static ImageSource CreateTokenImage(Token token)
        {
            // Get colors based on creature type
            var (centerColor, edgeColor, textColor) = GetCreatureTypeColors(token?.Type);

            // Get display character (first letter of name, or ? if none)
            string displayChar = "?";
            if (!string.IsNullOrEmpty(token?.Name))
            {
                displayChar = token.Name.Substring(0, 1).ToUpperInvariant();
            }

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Draw circle with type-based gradient
                var gradientBrush = new RadialGradientBrush(centerColor, edgeColor);
                gradientBrush.Freeze();

                var pen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 2);
                pen.Freeze();

                dc.DrawEllipse(gradientBrush, pen, new Point(24, 24), 22, 22);

                // Draw initial/character
                var textBrush = new SolidColorBrush(textColor);
                textBrush.Freeze();

                var text = new FormattedText(
                    displayChar,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    18,
                    textBrush,
                    1.0);
                dc.DrawText(text, new Point(24 - text.Width / 2, 24 - text.Height / 2));
            }

            var rtb = new RenderTargetBitmap(48, 48, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();

            return rtb;
        }

        private static (Color center, Color edge, Color text) GetCreatureTypeColors(string creatureType)
        {
            if (string.IsNullOrEmpty(creatureType))
                return (Color.FromRgb(100, 149, 237), Color.FromRgb(65, 105, 225), Colors.White); // Default blue

            // Normalize the type string
            string type = creatureType.ToLowerInvariant().Trim();

            // Return colors based on D&D creature types
            return type switch
            {
                // Classic monster types with thematic colors
                "aberration" => (Color.FromRgb(138, 43, 226), Color.FromRgb(75, 0, 130), Colors.White),      // Purple (eldritch)
                "beast" => (Color.FromRgb(139, 90, 43), Color.FromRgb(101, 67, 33), Colors.White),           // Brown (natural)
                "celestial" => (Color.FromRgb(255, 215, 0), Color.FromRgb(218, 165, 32), Colors.Black),      // Gold (divine)
                "construct" => (Color.FromRgb(169, 169, 169), Color.FromRgb(105, 105, 105), Colors.White),   // Gray (metal)
                "dragon" => (Color.FromRgb(220, 20, 60), Color.FromRgb(139, 0, 0), Colors.White),            // Red (fire)
                "elemental" => (Color.FromRgb(255, 140, 0), Color.FromRgb(255, 69, 0), Colors.White),        // Orange (energy)
                "fey" => (Color.FromRgb(50, 205, 50), Color.FromRgb(34, 139, 34), Colors.White),             // Green (nature magic)
                "fiend" => (Color.FromRgb(178, 34, 34), Color.FromRgb(85, 0, 0), Colors.White),              // Dark red (infernal)
                "giant" => (Color.FromRgb(160, 82, 45), Color.FromRgb(101, 55, 27), Colors.White),           // Sienna (earth)
                "humanoid" => (Color.FromRgb(100, 149, 237), Color.FromRgb(65, 105, 225), Colors.White),     // Blue (default)
                "monstrosity" => (Color.FromRgb(128, 0, 0), Color.FromRgb(64, 0, 0), Colors.White),          // Maroon (dangerous)
                "ooze" => (Color.FromRgb(124, 252, 0), Color.FromRgb(50, 205, 50), Colors.Black),            // Lime green (acidic)
                "plant" => (Color.FromRgb(34, 139, 34), Color.FromRgb(0, 100, 0), Colors.White),             // Forest green
                "undead" => (Color.FromRgb(72, 61, 139), Color.FromRgb(25, 25, 112), Colors.LightGray),      // Dark slate (death)
                "swarm" => (Color.FromRgb(85, 85, 85), Color.FromRgb(45, 45, 45), Colors.White),             // Dark gray (many)

                // Default for unknown types
                _ => (Color.FromRgb(100, 149, 237), Color.FromRgb(65, 105, 225), Colors.White)              // Blue
            };
        }

        private void LayoutTokens()
        {
            foreach (var kvp in _tokenVisualLookup)
            {
                var container = kvp.Value;
                if (container.Tag is Token token)
                {
                    Canvas.SetLeft(container, token.GridX * GridCellSize);
                    Canvas.SetTop(container, token.GridY * GridCellSize);
                    container.Width = GridCellSize * token.SizeInSquares + 8;
                    container.Height = GridCellSize * token.SizeInSquares + 8;
                }
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

            // Update the grid drawing (you can set showCoordinates to false now since we use rulers)
            GridHost.DrawGridViewport(GridCellSize, worldRect, 16, showCoordinates: false, showGrid: ShowGrid);

            // Update the coordinate rulers
            DrawCoordinateRulers();
        }

        #region Options getters

        public void SetGridMaxSize(int maxWidth, int maxHeight)
        {
            _gridWidth = Math.Max(1, maxWidth);
            _gridHeight = Math.Max(1, maxHeight);
            UpdateGridVisual();
            RedrawMovementOverlay();
            MarkLightingDirty();
            RedrawLighting();
        }

        public void UpdateShadowSoftness()
        {
            var wrapper = RenderCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(w => Panel.GetZIndex(w) == 50);
            if (wrapper != null) wrapper.Effect = new BlurEffect { Radius = Options.ShadowSoftnessPx, RenderingBias = RenderingBias.Quality };
        }

        private bool _isMiddlePanning = false;
        private Point _middlePanLast;

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
                RedrawMovementOverlay();
                RedrawPathVisual();

                e.Handled = true;
                return;
            }
        }

        #endregion

        #region Pan / Zoom / Mouse
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

        private void RenderCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var imageFile = files.FirstOrDefault(f =>
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(imageFile))
                    {
                        MapImage.Source = new BitmapImage(new Uri(imageFile));
                        MapImage.SetValue(Canvas.ZIndexProperty, -100);
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Data.GetDataPresent("DnDBattle.Token"))
                {
                    var proto = e.Data.GetData("DnDBattle.Token") as Token;
                    if (proto != null)
                    {
                        var protoId = proto.Id.ToString();
                        var now = DateTime.UtcNow;
                        if (_lastDropPrototypeId == protoId && (now - _lastDropTime) < _duplicateDropThreshold)
                        {
                            e.Handled = true;
                            return;
                        }
                        _lastDropPrototypeId = protoId;
                        _lastDropTime = now;

                        var dropPt = e.GetPosition(RenderCanvas);
                        var world = ScreenToWorld(dropPt);
                        int gx = (int)Math.Floor(world.X / GridCellSize);
                        int gy = (int)Math.Floor(world.Y / GridCellSize);

                        // Create a FULL copy of the token with ALL properties
                        var newToken = new Token
                        {
                            Id = Guid.NewGuid(),
                            Name = proto.Name,
                            Size = proto.Size,
                            Type = proto.Type,
                            Alignment = proto.Alignment,
                            ChallengeRating = proto.ChallengeRating,
                            Image = proto.Image,
                            IconPath = proto.IconPath,
                            HP = proto.MaxHP,
                            MaxHP = proto.MaxHP,
                            HitDice = proto.HitDice,
                            ArmorClass = proto.ArmorClass,
                            InitiativeModifier = proto.InitiativeModifier,
                            IsPlayer = proto.IsPlayer,
                            Speed = proto.Speed,
                            GridX = gx,
                            GridY = gy,
                            SizeInSquares = proto.SizeInSquares > 0 ? proto.SizeInSquares : 1,

                            // Ability Scores
                            Str = proto.Str,
                            Dex = proto.Dex,
                            Con = proto.Con,
                            Int = proto.Int,
                            Wis = proto.Wis,
                            Cha = proto.Cha,

                            // Extra info
                            Skills = proto.Skills?.ToList() ?? new List<string>(),
                            Senses = proto.Senses,
                            Languages = proto.Languages,
                            Immunities = proto.Immunities,
                            Resistances = proto.Resistances,
                            Vulnerabilities = proto.Vulnerabilities,
                            Traits = proto.Traits,
                            Notes = proto.Notes,

                            // ACTIONS - This was missing!
                            Actions = proto.Actions?.Select(a => new Models.Action
                            {
                                Name = a.Name,
                                AttackBonus = a.AttackBonus,
                                DamageExpression = a.DamageExpression,
                                Range = a.Range,
                                Description = a.Description
                            }).ToList() ?? new List<Models.Action>(),

                            BonusActions = proto.BonusActions?.Select(a => new Models.Action
                            {
                                Name = a.Name,
                                AttackBonus = a.AttackBonus,
                                DamageExpression = a.DamageExpression,
                                Range = a.Range,
                                Description = a.Description
                            }).ToList() ?? new List<Models.Action>(),

                            Reactions = proto.Reactions?.Select(a => new Models.Action
                            {
                                Name = a.Name,
                                AttackBonus = a.AttackBonus,
                                DamageExpression = a.DamageExpression,
                                Range = a.Range,
                                Description = a.Description
                            }).ToList() ?? new List<Models.Action>(),

                            LegendaryActions = proto.LegendaryActions?.Select(a => new Models.Action
                            {
                                Name = a.Name,
                                AttackBonus = a.AttackBonus,
                                DamageExpression = a.DamageExpression,
                                Range = a.Range,
                                Description = a.Description
                            }).ToList() ?? new List<Models.Action>(),

                            Tags = proto.Tags?.ToList() ?? new List<string>()
                        };

                        Tokens?.Add(newToken);
                        SelectedToken = newToken;
                        TokenAddedToMap?.Invoke(newToken);
                        RedrawMovementOverlay();
                        RedrawPathVisual();
                        e.Handled = true;
                        return;
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
        }
        #endregion

        #region Battle Targeting

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
            RenderCanvas.Cursor = Cursors.Cross;

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
            RenderCanvas.Cursor = Cursors.Arrow;

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

        #region Tokens
        private void Token_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Token token)
            {
                // HP changes - just update the HP bar (most common case)
                if (e.PropertyName == nameof(Token.HP) ||
                    e.PropertyName == nameof(Token.MaxHP) ||
                    e.PropertyName == nameof(Token.TempHP))
                {
                    UpdateTokenHPBar(token);

                    // Also do full rebuild if HP dropped to 0 (for death visual effects)
                    if (token.HP <= 0)
                    {
                        RebuildTokenVisuals();
                    }
                    return;
                }

                // Position changes - just re-layout (very cheap)
                if (e.PropertyName == nameof(Token.GridX) ||
                    e.PropertyName == nameof(Token.GridY))
                {
                    LayoutTokens();
                    InvalidateReachableCache(); // If it's the selected token
                    return;
                }

                // Properties that can be updated on a single token without full rebuild
                if (e.PropertyName == nameof(Token.Conditions) ||
                    e.PropertyName == nameof(Token.IsCurrentTurn) ||
                    e.PropertyName == nameof(Token.IsConcentrating))
                {
                    // Update just this one token instead of rebuilding everything!
                    UpdateSingleTokenVisual(token);
                    return;
                }

                // Image changes - need full rebuild for this token
                // (could be optimized further, but less common)
                if (e.PropertyName == nameof(Token.Image) ||
                    e.PropertyName == nameof(Token.DisplayImage))
                {
                    RebuildTokenVisuals();
                    return;
                }
            }
        }

        private void UpdateSingleTokenVisual(Token token)
        {
            var grid = GetTokenVisual(token.Id);
            if (grid == null) return;

            // Update HP bar
            var hpBar = FindHPBar(grid);
            if (hpBar != null)
            {
                double hpPercent = token.MaxHP > 0 ? (double)Math.Max(0, token.HP) / token.MaxHP : 0;
                double barWidth = GridCellSize * token.SizeInSquares - 8;

                hpBar.Width = Math.Max(0, barWidth * hpPercent);

                var newBrush = GetHPBarBrush(hpPercent);
                if (hpBar.Background != newBrush)
                {
                    hpBar.Background = newBrush;
                }
            }

            // Update condition badges
            WrapPanel oldBadges = null;
            foreach (UIElement child in grid.Children)
            {
                if (child is WrapPanel wp)
                {
                    oldBadges = wp;
                    break;
                }
            }

            if (oldBadges != null)
                grid.Children.Remove(oldBadges);

            var newBadges = CreateConditionBadges(token);
            if (newBadges != null)
                grid.Children.Add(newBadges);

            // Update current turn glow
            UpdateTokenGlow(grid, token);

            // Update Z-index for current turn
            Canvas.SetZIndex(grid, token.IsCurrentTurn ? 150 : 100);
        }

        /// <summary>
        /// Updates the glow effect for the current turn indicator
        /// </summary>
        private void UpdateTokenGlow(Grid container, Token token)
        {
            // Find existing glow border (it's a Border with green glow effect)
            Border existingGlow = null;
            foreach (UIElement child in container.Children)
            {
                if (child is Border b && b.BorderBrush is SolidColorBrush brush &&
                    brush.Color == Color.FromRgb(76, 175, 80))
                {
                    existingGlow = b;
                    break;
                }
            }

            if (token.IsCurrentTurn)
            {
                // Add glow if not present
                if (existingGlow == null)
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
                    // Insert at index 0 so it's behind other elements
                    container.Children.Insert(0, glowBorder);
                }
            }
            else
            {
                // Remove glow if present
                if (existingGlow != null)
                {
                    container.Children.Remove(existingGlow);
                }
            }
        }

        private Border FindHPBar(Grid container)
        {
            // Find the HP bar container (Grid at the bottom with the fill border)
            foreach (UIElement child in container.Children)
            {
                if (child is Grid hpGrid && hpGrid.VerticalAlignment == VerticalAlignment.Bottom)
                {
                    // Return the fill border (second Border child)
                    int borderCount = 0;
                    foreach (UIElement hpChild in hpGrid.Children)
                    {
                        if (hpChild is Border b)
                        {
                            borderCount++;
                            if (borderCount == 2)
                                return b;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Updates just the HP bar for a specific token without rebuilding all visuals.
        /// Uses O(1) dictionary lookup instead of linear search.
        /// </summary>
        private void UpdateTokenHPBar(Token token)
        {
            if (token == null) return;

            // ✅ O(1) lookup instead of linear search
            var container = GetTokenVisual(token.Id);
            if (container == null) return;

            // Find the HP bar grid within the container
            Grid hpBarContainer = null;
            foreach (UIElement child in container.Children)
            {
                if (child is Grid g && g.VerticalAlignment == VerticalAlignment.Bottom && g.Height == 4)
                {
                    hpBarContainer = g;
                    break;
                }
            }

            if (hpBarContainer == null) return;

            // Calculate new HP percentage
            double hpPercent = token.MaxHP > 0 ? (double)Math.Max(0, token.HP) / token.MaxHP : 0;

            // Find the fill border (second Border child)
            Border fillBorder = null;
            int borderCount = 0;
            foreach (UIElement child in hpBarContainer.Children)
            {
                if (child is Border b)
                {
                    borderCount++;
                    if (borderCount == 2)
                    {
                        fillBorder = b;
                        break;
                    }
                }
            }

            if (fillBorder != null)
            {
                double barWidth = hpBarContainer.Width;
                fillBorder.Width = Math.Max(0, barWidth * hpPercent);

                var newBrush = GetHPBarBrush(hpPercent);
                if (fillBorder.Background != newBrush)
                {
                    fillBorder.Background = newBrush;
                }
            }
        }
        #endregion

        #region Fog Of War

        private void OnFogChanged()
        {
            RedrawFog();
        }

        /// <summary>
        /// Redraws the fog overlay
        /// </summary>
        public void RedrawFog()
        {
            if (_fogService == null) return;

            if (!_fogService.IsEnabled)
            {
                // Remove fog layer if it exists
                RemoveFogLayer();
                return;
            }

            // Create new fog visual
            RenderFogLayer();
        }

        private void RenderFogLayer()
        {
            // Find or create the fog canvas
            if (_fogCanvas == null)
            {
                _fogCanvas = RenderCanvas.Children.OfType<Canvas>()
                    .FirstOrDefault(c => c.Tag as string == "FogLayer");
            }

            if (_fogCanvas == null)
            {
                _fogCanvas = new Canvas
                {
                    Tag = "FogLayer",
                    IsHitTestVisible = false
                };
                RenderCanvas.Children.Add(_fogCanvas);
            }

            // Clear existing fog rectangles
            _fogCanvas.Children.Clear();

            // Set Z-index high so fog is on top
            Canvas.SetZIndex(_fogCanvas, 1000);

            bool isPlayerView = _fogService.ShowPlayerView;
            double opacity = isPlayerView ? _fogService.PlayerFogOpacity : _fogService.DmFogOpacity;
            var fogBrush = new SolidColorBrush(_fogService.FogColor);

            // Draw fog for each hidden cell
            for (int x = 0; x < _fogService.GridWidth; x++)
            {
                for (int y = 0; y < _fogService.GridHeight; y++)
                {
                    if (!_fogService.IsCellRevealed(x, y))
                    {
                        var rect = new System.Windows.Shapes.Rectangle
                        {
                            Width = GridCellSize,
                            Height = GridCellSize,
                            Fill = fogBrush,
                            Opacity = opacity,
                            IsHitTestVisible = false
                        };

                        Canvas.SetLeft(rect, x * GridCellSize);
                        Canvas.SetTop(rect, y * GridCellSize);
                        _fogCanvas.Children.Add(rect);
                    }
                }
            }
        }

        private void RemoveFogLayer()
        {
            if (_fogCanvas != null)
            {
                _fogCanvas.Children.Clear();

                if (RenderCanvas.Children.Contains(_fogCanvas))
                {
                    RenderCanvas.Children.Remove(_fogCanvas);
                }

                _fogCanvas = null;
            }

            // Also remove any stray fog canvases
            var fogCanvases = RenderCanvas.Children.OfType<Canvas>()
                .Where(c => c.Tag as string == "FogLayer")
                .ToList();

            foreach (var canvas in fogCanvases)
            {
                RenderCanvas.Children.Remove(canvas);
            }
        }


        /// <summary>
        /// Sets fog enabled state
        /// </summary>
        public void SetFogEnabled(bool enabled)
        {
            _fogService.IsEnabled = enabled;
            RedrawFog();
        }

        /// <summary>
        /// Sets player view mode
        /// </summary>
        public void SetPlayerView(bool isPlayerView)
        {
            _fogService.ShowPlayerView = isPlayerView;
            RedrawFog();

            // Also update token visibility in player view
            if (isPlayerView)
            {
                UpdateTokenVisibilityForPlayerView();
            }
            else
            {
                ShowAllTokens();
            }
        }

        private void UpdateTokenVisibilityForPlayerView()
        {
            // Hide tokens that are in fog
            foreach (var child in RenderCanvas.Children.OfType<FrameworkElement>())
            {
                if (child.Tag is Models.Token token && !token.IsPlayer)
                {
                    bool visible = _fogService.IsTokenVisible(token);
                    child.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        private void ShowAllTokens()
        {
            foreach (var child in RenderCanvas.Children.OfType<FrameworkElement>())
            {
                if (child.Tag is Models.Token)
                {
                    child.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Sets the brush mode for fog editing
        /// </summary>
        public void SetFogBrushMode(FogBrushMode mode)
        {
            _fogService.BrushMode = mode;
        }

        /// <summary>
        /// Sets the brush size for fog editing
        /// </summary>
        public void SetFogBrushSize(int size)
        {
            _fogService.BrushSize = size;
        }

        /// <summary>
        /// Starts a shape-based fog reveal/hide
        /// </summary>
        public void StartFogShapeTool(FogShapeTool tool)
        {
            _currentFogShapeTool = tool;
            _fogShapeStartPoint = null;

            if (tool != FogShapeTool.None)
            {
                RenderCanvas.Cursor = Cursors.Cross;
            }
        }

        /// <summary>
        /// Reveals area around all player tokens
        /// </summary>
        public void RevealAroundPlayers()
        {
            if (Tokens != null)
            {
                _fogService.RevealAroundTokens(Tokens, playersOnly: true);
            }
        }

        private void HandleFogMouseDown(Point position)
        {
            if (!_fogService.IsEnabled) return;

            int gridX = (int)(position.X / GridCellSize);
            int gridY = (int)(position.Y / GridCellSize);

            if (_currentFogShapeTool != FogShapeTool.None)
            {
                // Start shape drawing
                _fogShapeStartPoint = new Point(gridX, gridY);
            }
            else
            {
                // Brush mode - start painting
                _isFogBrushActive = true;
                _fogService.ApplyBrush(gridX, gridY);
            }
        }

        // Add to RenderCanvas_MouseMove
        private void HandleFogMouseMove(Point position)
        {
            if (!_fogService.IsEnabled) return;

            int gridX = (int)(position.X / GridCellSize);
            int gridY = (int)(position.Y / GridCellSize);

            if (_isFogBrushActive)
            {
                _fogService.ApplyBrush(gridX, gridY);
            }
        }

        // Add to RenderCanvas_MouseLeftButtonUp
        private void HandleFogMouseUp(Point position)
        {
            if (!_fogService.IsEnabled) return;

            int gridX = (int)(position.X / GridCellSize);
            int gridY = (int)(position.Y / GridCellSize);

            if (_currentFogShapeTool != FogShapeTool.None && _fogShapeStartPoint.HasValue)
            {
                int startX = (int)_fogShapeStartPoint.Value.X;
                int startY = (int)_fogShapeStartPoint.Value.Y;

                if (_currentFogShapeTool == FogShapeTool.Rectangle)
                {
                    if (_fogService.BrushMode == FogBrushMode.Reveal)
                        _fogService.RevealRectangle(startX, startY, gridX, gridY);
                    else
                        _fogService.HideRectangle(startX, startY, gridX, gridY);
                }
                else if (_currentFogShapeTool == FogShapeTool.Circle)
                {
                    int radius = (int)Math.Max(Math.Abs(gridX - startX), Math.Abs(gridY - startY));
                    if (_fogService.BrushMode == FogBrushMode.Reveal)
                        _fogService.RevealCircle(startX, startY, radius);
                    else
                        _fogService.HideCircle(startX, startY, radius);
                }

                _fogShapeStartPoint = null;
                _currentFogShapeTool = FogShapeTool.None;
                RenderCanvas.Cursor = Cursors.Arrow;
            }

            _isFogBrushActive = false;
        }

        #endregion

        #region Condtions
        /// <summary>
        /// Creates condition badge icons that appear around the token.
        /// Uses cached brushes to avoid allocations.
        /// </summary>
        private FrameworkElement CreateConditionBadges(Token token)
        {
            if (token.Conditions == Models.Condition.None)
                return null;

            var activeConditions = token.Conditions.GetActiveConditions().ToList();
            if (activeConditions.Count == 0)
                return null;

            var badgePanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                MaxWidth = GridCellSize * token.SizeInSquares,
                Margin = new Thickness(2, 2, 0, 0)
            };

            int maxBadges = 6;
            int count = 0;

            foreach (var condition in activeConditions)
            {
                if (count >= maxBadges) break;

                var badge = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(3),
                    // Use cached brush instead of new SolidColorBrush
                    Background = ConditionExtensions.GetConditionBrush(condition),
                    Margin = new Thickness(1),
                    ToolTip = CreateConditionTooltip(condition),
                    Child = new TextBlock
                    {
                        Text = ConditionExtensions.GetConditionIcon(condition),
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                badgePanel.Children.Add(badge);
                count++;
            }

            // Show "+X" if there are more conditions
            if (activeConditions.Count > maxBadges)
            {
                var moreBadge = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(3),
                    // Use cached brush
                    Background = ConditionExtensions.GetOverflowBadgeBrush(),
                    Margin = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = $"+{activeConditions.Count - maxBadges}",
                        FontSize = 8,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                badgePanel.Children.Add(moreBadge);
            }

            return badgePanel;
        }

        /// <summary>
        /// Creates a tooltip for a condition badge.
        /// Uses cached brushes to avoid allocations.
        /// </summary>
        private ToolTip CreateConditionTooltip(Models.Condition condition)
        {
            var tooltip = new ToolTip
            {
                // Use cached brushes
                Background = ConditionExtensions.GetTooltipBackgroundBrush(),
                Foreground = ConditionExtensions.GetTooltipTextBrush(),
                BorderBrush = ConditionExtensions.GetConditionBrush(condition),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(10)
            };

            var stack = new StackPanel { MaxWidth = 300 };

            stack.Children.Add(new TextBlock
            {
                Text = $"{ConditionExtensions.GetConditionIcon(condition)} {ConditionExtensions.GetConditionName(condition)}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 8)
            });

            stack.Children.Add(new TextBlock
            {
                Text = ConditionExtensions.GetConditionDescription(condition),
                TextWrapping = TextWrapping.Wrap,
                // Use cached brush
                Foreground = ConditionExtensions.GetConditionDescriptionBrush()
            });

            tooltip.Content = stack;
            return tooltip;
        }

        /// <summary>
        /// Applies visual effects to the token image based on conditions
        /// </summary>
        private void ApplyConditionVisualEffects(Image img, Token token)
        {
            // Invisible - make semi-transparent
            if (token.HasCondition(Models.Condition.Invisible))
            {
                img.Opacity = 0.4;
            }

            // Petrified - grayscale effect
            if (token.HasCondition(Models.Condition.Petrified))
            {
                var grayscaleEffect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    ShadowDepth = 0,
                    BlurRadius = 0
                };
                // Note: For true grayscale you'd need a shader, this is a simple approximation
                img.Opacity = 0.7;
            }

            // Unconscious/Prone - rotate slightly
            if (token.HasCondition(Models.Condition.Unconscious) || token.HasCondition(Models.Condition.Prone))
            {
                img.RenderTransformOrigin = new Point(0.5, 0.5);
                img.RenderTransform = new RotateTransform(token.HasCondition(Models.Condition.Unconscious) ? 90 : 15);
            }

            // Dead (HP <= -MaxHP) - very faded
            if (token.IsDead)
            {
                img.Opacity = 0.3;
            }
        }

        /// <summary>
        /// Creates a small HP bar under the token
        /// </summary>
        private FrameworkElement CreateTokenHPBar(Token token)
        {
            double hpPercent = token.MaxHP > 0 ? (double)Math.Max(0, token.HP) / token.MaxHP : 0;

            var barWidth = GridCellSize * token.SizeInSquares - 8;

            var container = new Grid
            {
                Width = barWidth,
                Height = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 2)
            };

            // Background - use cached brush
            container.Children.Add(new Border
            {
                Background = HPBarBackgroundBrush,
                CornerRadius = new CornerRadius(2),
                Opacity = 0.8
            });

            // HP fill - use cached brush
            container.Children.Add(new Border
            {
                Background = GetHPBarBrush(hpPercent),
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(0, barWidth * hpPercent)
            });

            return container;
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

        public void SetRoomDrawMode(bool enabled, WallType wallType = WallType.Solid)
        {
            _roomDrawMode = enabled;
            _currentWallType = wallType;
            _roomVertices.Clear();
            _wallDrawMode = false;
            _wallDrawStart = null;

            RenderCanvas.Cursor = enabled ? Cursors.Cross : Cursors.Arrow;
            RedrawWalls();
        }

        public bool IsRoomDrawMode => _roomDrawMode;

        public void AddWall(Wall wall) =>
            _wallService.AddWall(wall);

        public void RemoveWall(Wall wall) =>
            _wallService.RemoveWall(wall);

        public void RedrawWalls()
        {
            using (var dc = _wallVisual.RenderOpen())
            {
                // ✅ Use cached pens - zero allocations!

                foreach (var wall in _wallService.Walls)
                {
                    var startPx = new Point(
                        wall.StartPoint.X * GridCellSize + GridCellSize / 2,
                        wall.StartPoint.Y * GridCellSize + GridCellSize / 2);
                    var endPx = new Point(
                        wall.EndPoint.X * GridCellSize + GridCellSize / 2,
                        wall.EndPoint.Y * GridCellSize + GridCellSize / 2);

                    // Select the appropriate cached pen
                    Pen wallPen = wall.WallType switch
                    {
                        WallType.Solid => WallSolidPen,
                        WallType.Door => wall.IsOpen ? WallDoorOpenPen : WallDoorPen,
                        WallType.Window => WallWindowPen,
                        WallType.Halfwall => WallHalfwallPen,
                        _ => WallSolidPen
                    };

                    // Use appropriate cached shadow pen based on wall thickness
                    Pen shadowPen = (wallPen.Thickness >= 6) ? WallShadowPen6 : WallShadowPen4;

                    // Draw shadow
                    dc.DrawLine(shadowPen,
                        new Point(startPx.X + 2, startPx.Y + 2),
                        new Point(endPx.X + 2, endPx.Y + 2));

                    // Draw wall
                    dc.DrawLine(wallPen, startPx, endPx);

                    // Draw selection highlight
                    if (wall == _selectedWall)
                    {
                        dc.DrawLine(WallSelectedPen, startPx, endPx);

                        // Draw handles
                        dc.DrawEllipse(Brushes.Yellow, WallHandlePen, startPx, 8, 8);
                        dc.DrawEllipse(Brushes.Yellow, WallHandlePen, endPx, 8, 8);
                    }

                    // Draw labels (unchanged - dynamic text requires FormattedText)
                    if (wall == _selectedWall || !string.IsNullOrEmpty(wall.Label))
                    {
                        var midPoint = new Point(
                            (startPx.X + endPx.X) / 2,
                            (startPx.Y + endPx.Y) / 2);

                        string labelText = wall.Label ?? $"{wall.WallType}";

                        if (wall == _selectedWall || wall.WallType == WallType.Door)
                        {
                            var labelFormatted = new FormattedText(
                                labelText,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                CachedTypefaces.SegoeUI,  // ✅ Use cached typeface
                                10,
                                Brushes.White,
                                1.0);

                            var labelBg = new Rect(
                                midPoint.X - labelFormatted.Width / 2 - 4,
                                midPoint.Y - labelFormatted.Height / 2 - 15,
                                labelFormatted.Width + 8,
                                labelFormatted.Height + 4);

                            dc.DrawRoundedRectangle(
                                new SolidColorBrush(Color.FromArgb(200, 40, 40, 40)),
                                null, labelBg, 3, 3);

                            dc.DrawText(labelFormatted, new Point(
                                midPoint.X - labelFormatted.Width / 2,
                                midPoint.Y - labelFormatted.Height / 2 - 13));
                        }
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
                    // Wall mode indicator
                    var indicatorText = new FormattedText(
                        $"Wall Mode: {_currentWallType}\nClick to start, click again to place\nRight-click to cancel",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        CachedTypefaces.SegoeUI,
                        12,
                        Brushes.White,
                        1.0);

                    var bgRect = new Rect(10, 10, indicatorText.Width + 20, indicatorText.Height + 10);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)), null, bgRect);
                    dc.DrawText(indicatorText, new Point(20, 15));
                }

                // Draw room preview
                if (_roomDrawMode && _roomVertices.Count > 0)
                {
                    var roomPen = new Pen(Brushes.Cyan, 3);
                    roomPen.DashStyle = DashStyles.Dash;
                    roomPen.Freeze();

                    // Draw existing vertices and lines
                    for (int i = 0; i < _roomVertices.Count; i++)
                    {
                        var vertexPx = new Point(
                            _roomVertices[i].X * GridCellSize + GridCellSize / 2,
                            _roomVertices[i].Y * GridCellSize + GridCellSize / 2);

                        // Draw vertex marker
                        dc.DrawEllipse(Brushes.Cyan, new Pen(Brushes.White, 2), vertexPx, 8, 8);

                        // Draw vertex number
                        // Vertex number
                        var numText = new FormattedText(
                            (i + 1).ToString(),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            CachedTypefaces.SegoeUI,
                            10,
                            Brushes.Black,
                            1.0);
                        dc.DrawText(numText, new Point(vertexPx.X - 4, vertexPx.Y - 6));

                        // Draw line to previous vertex
                        if (i > 0)
                        {
                            var prevPx = new Point(
                                _roomVertices[i - 1].X * GridCellSize + GridCellSize / 2,
                                _roomVertices[i - 1].Y * GridCellSize + GridCellSize / 2);
                            dc.DrawLine(roomPen, prevPx, vertexPx);
                        }
                    }

                    // Draw closing line preview (from last vertex back to first)
                    if (_roomVertices.Count >= 3)
                    {
                        var firstPx = new Point(
                            _roomVertices[0].X * GridCellSize + GridCellSize / 2,
                            _roomVertices[0].Y * GridCellSize + GridCellSize / 2);
                        var lastPx = new Point(
                            _roomVertices[^1].X * GridCellSize + GridCellSize / 2,
                            _roomVertices[^1].Y * GridCellSize + GridCellSize / 2);

                        var closingPen = new Pen(Brushes.Cyan, 2);
                        closingPen.DashStyle = DashStyles.Dot;
                        closingPen.Freeze();
                        dc.DrawLine(closingPen, lastPx, firstPx);
                    }

                    // Show instructions
                    var instructionText = _roomVertices.Count < 3
                        ? $"Click to add corners ({_roomVertices.Count}/3 min)\nDouble-click to finish"
                        : "Double-click to finish room\nRight-click to cancel";

                    // Room instructions
                    var instructions = new FormattedText(
                        instructionText,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        CachedTypefaces.SegoeUI,
                        12,
                        Brushes.White,
                        1.0);

                    var instrBg = new Rect(10, 10, instructions.Width + 20, instructions.Height + 10);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)), null, instrBg);
                    dc.DrawText(instructions, new Point(20, 15));
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

        private void HandleRoomDrawClick(Point gridPoint, MouseButtonEventArgs e)
        {
            if (!_roomDrawMode) return;

            if (e.ClickCount >= 2 && _roomVertices.Count >= 3)
            {
                FinishRoomDrawing();
                e.Handled = true;
                return;
            }

            _roomVertices.Add(gridPoint);
            RedrawWalls();
            e.Handled = true;
        }

        private void HandleWallSelection(Point gridPoint, bool isRightClick = false)
        {
            // Check for endpoint hit first (for dragging)
            var endPointWall = _wallService.HitTestEndPoint(gridPoint, out bool isStart, 0.5);
            if (endPointWall != null && !isRightClick)
            {
                _selectedWall = endPointWall;
                _isDraggingWallEndpoint = true;
                _draggingWallIsStart = isStart;
                RedrawWalls();
                return;
            }

            // Check for wall body hit
            var hitWall = _wallService.HitTest(gridPoint, 0.5);
            if (hitWall != null)
            {
                _selectedWall = hitWall;

                if (isRightClick)
                {
                    // Show context menu
                    var menu = CreateWallContextMenu(hitWall);
                    menu.IsOpen = true;
                }
                else if (hitWall.WallType == WallType.Door)
                {
                    // Left-click toggles doors
                    hitWall.IsOpen = !hitWall.IsOpen;
                    AddToActionLog("Door", $"{hitWall.Label} is now {(hitWall.IsOpen ? "OPEN" : "CLOSED")}");
                    RedrawLighting();
                }

                RedrawWalls();
                return;
            }

            _selectedWall = null;
            RedrawWalls();
        }

        private ContextMenu CreateWallContextMenu(Wall wall)
        {
            var menu = new ContextMenu();

            // Wall type submenu
            var typeMenu = new MenuItem { Header = "🔄 Change Type" };

            foreach (WallType wallType in Enum.GetValues(typeof(WallType)))
            {
                var typeItem = new MenuItem
                {
                    Header = wallType.ToString(),
                    IsChecked = wall.WallType == wallType,
                    Tag = wallType
                };
                typeItem.Click += (s, e) =>
                {
                    wall.WallType = (WallType)((MenuItem)s).Tag;
                    AddToActionLog("Wall", $"Changed wall to {wall.WallType}");
                    RedrawWalls();
                    MarkLightingDirty();  // Wall type affects light blocking
                    RedrawLighting();
                };
                typeMenu.Items.Add(typeItem);
            }
            menu.Items.Add(typeMenu);

            // Toggle door state (only for doors)
            if (wall.WallType == WallType.Door)
            {
                var toggleItem = new MenuItem
                {
                    Header = wall.IsOpen ? "🔒 Close Door" : "🚪 Open Door"
                };
                toggleItem.Click += (s, e) =>
                {
                    wall.IsOpen = !wall.IsOpen;
                    AddToActionLog("Door", $"{wall.Label} is now {(wall.IsOpen ? "OPEN" : "CLOSED")}");
                    RedrawWalls();
                    RedrawLighting();
                };
                menu.Items.Add(toggleItem);
            }

            menu.Items.Add(new Separator());

            // Edit label
            var labelItem = new MenuItem { Header = "✏️ Edit Label..." };
            labelItem.Click += (s, e) =>
            {
                string newLabel = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter wall label:", "Edit Wall Label", wall.Label ?? "");
                if (!string.IsNullOrWhiteSpace(newLabel))
                {
                    wall.Label = newLabel;
                    RedrawWalls();
                }
            };
            menu.Items.Add(labelItem);

            menu.Items.Add(new Separator());

            // Delete wall
            var deleteItem = new MenuItem { Header = "🗑️ Delete Wall" };
            deleteItem.Click += (s, e) =>
            {
                _wallService.RemoveWall(wall);
                AddToActionLog("Wall", $"Deleted {wall.Label}");
                _selectedWall = null;
                RedrawWalls();
                RedrawLighting();
            };
            menu.Items.Add(deleteItem);

            return menu;
        }

        private void FinishRoomDrawing()
        {
            if (_roomVertices.Count < 3) return;

            for (int i = 0; i < _roomVertices.Count; i++)
            {
                var start = _roomVertices[i];
                var end = _roomVertices[(i + 1) % _roomVertices.Count];

                var wall = new Wall()
                {
                    StartPoint = start,
                    EndPoint = end,
                    WallType = _currentWallType,
                    Label = $"Room Wall {i + 1}"
                };
                _wallService.AddWall(wall);
            }

            AddToActionLog("Room", $"Created room with {_roomVertices.Count} walls");

            _roomVertices.Clear();
            _roomDrawMode = false;
            RenderCanvas.Cursor = Cursors.Arrow;
            RedrawWalls();
            RedrawLighting();
        }

        #endregion

        #region Measurment Tools
        public void SetMeasureMode(bool enabled)
        {
            _measureMode = true;
            _measureStart = null;
            RenderCanvas.Cursor = enabled ? Cursors.Cross : Cursors.Arrow;
            RedrawMeasureVisual();
        }

        public bool IsMeasureMode => _measureMode;

        private void RedrawMeasureVisual()
        {
            using (var dc = _measureVisual.RenderOpen())
            {
                if (!_measureMode || !_measureStart.HasValue) return;

                var startPx = new Point(
                    _measureStart.Value.X * GridCellSize + GridCellSize / 2,
                    _measureStart.Value.Y * GridCellSize + GridCellSize / 2);
                var endPx = new Point(
                    _measureEnd.X * GridCellSize + GridCellSize / 2,
                    _measureEnd.Y * GridCellSize + GridCellSize / 2);

                // ✅ Use cached pens - zero allocations!

                // Draw shadow line
                dc.DrawLine(MeasureShadowPen,
                    new Point(startPx.X + 2, startPx.Y + 2),
                    new Point(endPx.X + 2, endPx.Y + 2));

                // Draw main measurement line
                dc.DrawLine(MeasureLinePen, startPx, endPx);

                // Draw endpoints with cached pen
                dc.DrawEllipse(Brushes.Yellow, MeasureEndpointPen, startPx, 8, 8);
                dc.DrawEllipse(Brushes.Yellow, MeasureEndpointPen, endPx, 8, 8);

                // Calculate distances
                double dx = _measureEnd.X - _measureStart.Value.X;
                double dy = _measureEnd.Y - _measureStart.Value.Y;

                double euclideanSquares = Math.Sqrt(dx * dx + dy * dy);
                double diagonals = Math.Min(Math.Abs(dx), Math.Abs(dy));
                double straights = Math.Max(Math.Abs(dx), Math.Abs(dy)) - diagonals;
                double dndSquares = straights + diagonals * 1.5;

                double feetEuclidean = euclideanSquares * 5;
                double feetDnd = dndSquares * 5;

                // Draw measurement label
                var midPoint = new Point((startPx.X + endPx.X) / 2, (startPx.Y + endPx.Y) / 2 - 40);

                string measureText = $"📏 {euclideanSquares:F1} squares ({feetEuclidean:F0} ft)\n" +
                                    $"🎲 D&D: {dndSquares:F1} squares ({feetDnd:F0} ft)";

                // ✅ Use cached typeface
                var formattedText = new FormattedText(
                    measureText,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    CachedTypefaces.SegoeUI,
                    14,
                    Brushes.White,
                    1.0);

                // Background for text - use cached resources
                var textBounds = new Rect(
                    midPoint.X - formattedText.Width / 2 - 8,
                    midPoint.Y - 4,
                    formattedText.Width + 16,
                    formattedText.Height + 8);

                // ✅ Use cached brush and pen
                dc.DrawRoundedRectangle(
                    MeasureLabelBackgroundBrush,
                    MeasureLabelBorderPen,
                    textBounds, 6, 6);

                dc.DrawText(formattedText, new Point(midPoint.X - formattedText.Width / 2, midPoint.Y));

                // Draw grid coordinate labels at start and end
                string startCoord = GridVisualHost.GetCoordinateString((int)_measureStart.Value.X, (int)_measureStart.Value.Y);
                string endCoord = GridVisualHost.GetCoordinateString((int)_measureEnd.X, (int)_measureEnd.Y);

                DrawCoordinateLabel(dc, startCoord, startPx, -25);
                DrawCoordinateLabel(dc, endCoord, endPx, 15);
            }
        }

        private void DrawCoordinateLabel(DrawingContext dc, string text, Point position, double yOffset)
        {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                CachedTypefaces.SegoeUI,
                11,
                Brushes.White,
                1.0);

            var labelPos = new Point(position.X - formattedText.Width / 2, position.Y + yOffset);
            var bgRect = new Rect(labelPos.X - 4, labelPos.Y - 2, formattedText.Width + 8, formattedText.Height + 4);

            dc.DrawRoundedRectangle(
                CachedDrawingResources.SemiTransparentBlackBrush,
                null,
                bgRect, 4, 4);

            dc.DrawText(formattedText, labelPos);
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

                // Check if we can use cached result
                bool cacheValid = _cachedReachableSquares != null &&
                                  _cachedReachableTokenId == SelectedToken.Id &&
                                  _cachedReachableStartX == startX &&
                                  _cachedReachableStartY == startY &&
                                  _cachedReachableMaxSquares == maxSquares &&
                                  _cachedReachableWallVersion == _wallVersion;

                HashSet<(int x, int y)> reachable;

                if (cacheValid)
                {
                    // Use cached result
                    reachable = _cachedReachableSquares;
                }
                else
                {
                    // Recalculate and cache
                    Func<int, int, bool> isBlocked = (gx, gy) =>
                    {
                        var cellCenter = new Point(gx + 0.5, gy + 0.5);

                        foreach (var wall in _wallService.Walls)
                        {
                            if (!wall.BlocksMovement) continue;
                            if (wall.IsPointNear(cellCenter, 0.6))
                                return true;
                        }
                        return false;
                    };

                    reachable = MovementService.GetReachableSquares(
                        startX, startY, maxSquares, _gridWidth, _gridHeight, isBlocked);

                    // Update cache
                    _cachedReachableSquares = reachable;
                    _cachedReachableTokenId = SelectedToken.Id;
                    _cachedReachableStartX = startX;
                    _cachedReachableStartY = startY;
                    _cachedReachableMaxSquares = maxSquares;
                    _cachedReachableWallVersion = _wallVersion;
                }

                foreach (var cell in reachable)
                {
                    var rect = new Rect(cell.x * GridCellSize, cell.y * GridCellSize, GridCellSize, GridCellSize);
                    dc.DrawRectangle(MovementOverlayBrush, MovementOverlayBorderPen, rect);
                }
            }
        }

        /// <summary>
        /// Invalidates the reachable squares cache, forcing recalculation on next draw.
        /// Call this when token position, selection, or walls change.
        /// </summary>
        public void InvalidateReachableCache()
        {
            _cachedReachableSquares = null;
        }
        #endregion

        #region Path preview
        private void ComputeAndDrawPathPreview((int x, int y) targetCell)
        {
            if (SelectedToken == null) return;

            if (DateTime.Now - _lastPathCalculation < _pathCalculationDebounce)
                return;
            _lastPathCalculation = DateTime.Now;

            var start = (SelectedToken.GridX, SelectedToken.GridY);
            var goal = targetCell;

            // === CACHE CHECK ===
            // If start, goal, and wall configuration haven't changed, reuse the cached path
            if (_cachedPathStart == start &&
                _cachedPathGoal == goal &&
                _cachedPathWallVersion == _wallVersion &&
                _lastPreviewPath != null)
            {
                // Path is already calculated and displayed - nothing to do!
                System.Diagnostics.Debug.WriteLine($"Path cache HIT: ({start.GridX},{start.GridY}) -> ({goal.x},{goal.y})");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Path cache MISS: calculating ({start.GridX},{start.GridY}) -> ({goal.x},{goal.y})");

            // Cache the walkability function to avoid recreating it
            // Also cache the wall list reference to avoid repeated property access
            var walls = _wallService.Walls;

            Func<int, int, bool> isWalkable = (gx, gy) =>
            {
                var cellCenter = new Point(gx + 0.5, gy + 0.5);

                // Check walls - use cached reference
                foreach (var wall in walls)
                {
                    if (!wall.BlocksMovement) continue;
                    if (wall.IsPointNear(cellCenter, 0.6))
                        return false;
                }
                return true;
            };

            var path = MovementService.FindPathAStar(start, goal, _gridWidth, _gridHeight, isWalkable);

            // === UPDATE CACHE ===
            _lastPreviewPath = path;
            _cachedPathStart = start;
            _cachedPathGoal = goal;
            _cachedPathWallVersion = _wallVersion;

            // Calculate AOO indices
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

                for (int i = 0; i < _lastPreviewPath.Count; i++)
                {
                    var s = _lastPreviewPath[i];
                    var rect = new Rect(s.x * GridCellSize, s.y * GridCellSize, GridCellSize, GridCellSize);
                    dc.DrawRectangle(PathStepBrush, null, rect);

                    if (_lastAooIndices != null && _lastAooIndices.Contains(i))
                    {
                        var cx = rect.Left + rect.Width / 2;
                        var cy = rect.Top + rect.Height / 2;
                        // ✅ Use cached pen instead of creating new one
                        dc.DrawEllipse(null, PathAOOPen, new Point(cx, cy), rect.Width * 0.35, rect.Height * 0.35);
                    }
                }

                // Draw path line
                var pts = _lastPreviewPath
                    .Select(p => new Point(p.x * GridCellSize + GridCellSize / 2.0, p.y * GridCellSize + GridCellSize / 2.0))
                    .ToArray();

                if (pts.Length >= 2)
                {
                    var pg = new PathGeometry();
                    var pf = new PathFigure { StartPoint = pts[0], IsClosed = false };
                    for (int i = 1; i < pts.Length; i++)
                        pf.Segments.Add(new LineSegment(pts[i], true));
                    pg.Figures.Add(pf);
                    dc.DrawGeometry(null, PathPreviewLinePen, pg);
                }
            }
        }

        /// <summary>
        /// Draws coordinate rulers along the top and left edges of the screen.
        /// Uses cached brushes to avoid allocations.
        /// </summary>
        private void DrawCoordinateRulers()
        {
            // Clear tick marks and labels, but keep backgrounds if they exist
            // This is more efficient than clearing everything
            for (int i = RulerCanvas.Children.Count - 1; i >= 0; i--)
            {
                var child = RulerCanvas.Children[i];
                if (child != _topRulerBackground && child != _leftRulerBackground)
                {
                    RulerCanvas.Children.RemoveAt(i);
                }
            }

            if (!_showCoordinateRulers) return;
            if (ActualWidth <= 0 || ActualHeight <= 0) return;

            var rulerHeight = 20.0;
            var rulerWidth = 25.0;

            // Create or update top ruler background
            if (_topRulerBackground == null)
            {
                _topRulerBackground = new System.Windows.Shapes.Rectangle
                {
                    Fill = RulerBackgroundBrush  // ✅ Cached brush
                };
                Canvas.SetLeft(_topRulerBackground, 0);
                Canvas.SetTop(_topRulerBackground, 0);
                RulerCanvas.Children.Add(_topRulerBackground);
            }
            _topRulerBackground.Width = ActualWidth;
            _topRulerBackground.Height = rulerHeight;

            // Create or update left ruler background
            if (_leftRulerBackground == null)
            {
                _leftRulerBackground = new System.Windows.Shapes.Rectangle
                {
                    Fill = RulerBackgroundBrush  // ✅ Cached brush
                };
                Canvas.SetLeft(_leftRulerBackground, 0);
                Canvas.SetTop(_leftRulerBackground, 0);
                RulerCanvas.Children.Add(_leftRulerBackground);
            }
            _leftRulerBackground.Width = rulerWidth;
            _leftRulerBackground.Height = ActualHeight;

            // Calculate visible grid range
            var topLeft = ScreenToWorld(new Point(rulerWidth, rulerHeight));
            var bottomRight = ScreenToWorld(new Point(ActualWidth, ActualHeight));

            int startCol = Math.Max(_gridMinX, (int)Math.Floor(topLeft.X / GridCellSize));
            int endCol = Math.Min(_gridMaxX, (int)Math.Ceiling(bottomRight.X / GridCellSize));
            int startRow = Math.Max(_gridMinY, (int)Math.Floor(topLeft.Y / GridCellSize));
            int endRow = Math.Min(_gridMaxY, (int)Math.Ceiling(bottomRight.Y / GridCellSize));

            // Draw column labels (A, B, C, ..., AA, AB, ...)
            for (int col = startCol; col <= endCol; col++)
            {
                if (col < _gridMinX) continue;

                var worldX = col * GridCellSize;
                var screenX = WorldToScreen(new Point(worldX, 0)).X;

                if (screenX < rulerWidth || screenX > ActualWidth) continue;

                // Draw tick mark with cached brush
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = screenX,
                    Y1 = rulerHeight - 5,
                    X2 = screenX,
                    Y2 = rulerHeight,
                    Stroke = RulerLineBrush,  // ✅ Cached brush
                    StrokeThickness = 1
                };
                RulerCanvas.Children.Add(tick);

                // Draw label with cached resources
                string label = GetColumnLabel(col);
                var text = new TextBlock
                {
                    Text = label,
                    Foreground = RulerTextBrush,    // ✅ Cached brush
                    FontSize = 10,
                    FontFamily = RulerFontFamily     // ✅ Cached FontFamily
                };
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(text, screenX + (GridCellSize * _zoom.ScaleX / 2) - text.DesiredSize.Width / 2);
                Canvas.SetTop(text, 2);
                RulerCanvas.Children.Add(text);
            }

            // Draw row labels (1, 2, 3, ...)
            for (int row = startRow; row <= endRow; row++)
            {
                if (row < _gridMinY) continue;

                var worldY = row * GridCellSize;
                var screenY = WorldToScreen(new Point(0, worldY)).Y;

                if (screenY < rulerHeight || screenY > ActualHeight) continue;

                // Draw tick mark with cached brush
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = rulerWidth - 5,
                    Y1 = screenY,
                    X2 = rulerWidth,
                    Y2 = screenY,
                    Stroke = RulerLineBrush,  // ✅ Cached brush
                    StrokeThickness = 1
                };
                RulerCanvas.Children.Add(tick);

                // Draw label with cached resources
                string label = (row + 1).ToString();
                var text = new TextBlock
                {
                    Text = label,
                    Foreground = RulerTextBrush,    // ✅ Cached brush
                    FontSize = 10,
                    FontFamily = RulerFontFamily     // ✅ Cached FontFamily
                };
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(text, rulerWidth - text.DesiredSize.Width - 3);
                Canvas.SetTop(text, screenY + (GridCellSize * _zoom.ScaleY / 2) - text.DesiredSize.Height / 2);
                RulerCanvas.Children.Add(text);
            }
        }

        private void InitializeRulerPools(double rulerWidth, double rulerHeight)
        {
            // Create background rectangles (only once)
            _topRulerBg = new System.Windows.Shapes.Rectangle
            {
                Height = rulerHeight,
                Fill = RulerBgBrush
            };
            Canvas.SetLeft(_topRulerBg, 0);
            Canvas.SetTop(_topRulerBg, 0);
            RulerCanvas.Children.Add(_topRulerBg);

            _leftRulerBg = new System.Windows.Shapes.Rectangle
            {
                Width = rulerWidth,
                Fill = RulerBgBrush
            };
            Canvas.SetLeft(_leftRulerBg, 0);
            Canvas.SetTop(_leftRulerBg, 0);
            RulerCanvas.Children.Add(_leftRulerBg);

            _rulerPoolsInitialized = true;
        }

        private void EnsureColPoolSize(int requiredCount, double rulerHeight)
        {
            while (_colLabelPool.Count < requiredCount)
            {
                var tick = new System.Windows.Shapes.Line
                {
                    Y1 = rulerHeight - 5,
                    Y2 = rulerHeight,
                    Stroke = RulerLineBrush,
                    StrokeThickness = 1,
                    Visibility = Visibility.Collapsed
                };
                RulerCanvas.Children.Add(tick);
                _colTickPool.Add(tick);

                var text = new TextBlock
                {
                    Foreground = RulerTextBrush,
                    FontSize = 10,
                    FontFamily = new FontFamily("Segoe UI"),
                    Visibility = Visibility.Collapsed
                };
                Canvas.SetTop(text, 2);
                RulerCanvas.Children.Add(text);
                _colLabelPool.Add(text);
            }
        }

        private void EnsureRowPoolSize(int requiredCount, double rulerWidth)
        {
            while (_rowLabelPool.Count < requiredCount)
            {
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = rulerWidth - 5,
                    X2 = rulerWidth,
                    Stroke = RulerLineBrush,
                    StrokeThickness = 1,
                    Visibility = Visibility.Collapsed
                };
                RulerCanvas.Children.Add(tick);
                _rowTickPool.Add(tick);

                var text = new TextBlock
                {
                    Foreground = RulerTextBrush,
                    FontSize = 10,
                    FontFamily = new FontFamily("Segoe UI"),
                    Visibility = Visibility.Collapsed
                };
                RulerCanvas.Children.Add(text);
                _rowLabelPool.Add(text);
            }
        }

        /// <summary>
        /// Clears the path visual and invalidates the cache
        /// </summary>
        private void ClearPathVisual()
        {
            _lastPreviewPath = null;
            _lastAooIndices = null;

            // Clear the cache as well
            _cachedPathStart = null;
            _cachedPathGoal = null;

            using (var dc = _pathVisual.RenderOpen()) { }
        }

        /// <summary>
        /// Call this when something changes that should invalidate cached paths
        /// (e.g., token moved, walls changed, etc.)
        /// </summary>
        public void InvalidatePathCache()
        {
            _cachedPathStart = null;
            _cachedPathGoal = null;
            _cachedPathWallVersion = -1;
        }

        #endregion

        #region Area Effect Methods

        /// <summary>
        /// Starts placing an area effect of the given shape
        /// </summary>
        public void StartAreaEffectPlacement(AreaEffectShape shape, int sizeInFeet, Color color)
        {
            _isPlacingAreaEffect = true;
            _currentAoeShape = shape;
            _currentAoeSize = sizeInFeet;
            _currentAoeColor = color;

            _previewEffect = new AreaEffect
            {
                Shape = shape,
                SizeInFeet = sizeInFeet,
                Color = color,
                IsPreview = true
            };

            Cursor = Cursors.Cross;
        }

        /// <summary>
        /// Starts placing a preset area effect
        /// </summary>
        public void StartAreaEffectPlacement(AreaEffect preset)
        {
            _isPlacingAreaEffect = true;
            _currentAoeShape = preset.Shape;
            _currentAoeSize = preset.SizeInFeet;
            _currentAoeColor = preset.Color;

            _previewEffect = new AreaEffect
            {
                Name = preset.Name,
                Shape = preset.Shape,
                SizeInFeet = preset.SizeInFeet,
                WidthInFeet = preset.WidthInFeet,
                Color = preset.Color,
                IsPreview = true
            };

            Cursor = Cursors.Cross;
        }

        /// <summary>
        /// Cancels the current area effect placement
        /// </summary>
        public void CancelAreaEffectPlacement()
        {
            _isPlacingAreaEffect = false;
            _currentAoeShape = null;
            _previewEffect = null;
            Cursor = Cursors.Arrow;
            RedrawAreaEffects();
        }

        /// <summary>
        /// Updates the area effect size during placement
        /// </summary>
        public void UpdateAreaEffectSize(int sizeInFeet)
        {
            _currentAoeSize = sizeInFeet;
            if (_previewEffect != null)
            {
                _previewEffect.SizeInFeet = sizeInFeet;
                RedrawAreaEffects();
            }
        }

        /// <summary>
        /// Updates the area effect color during placement
        /// </summary>
        public void UpdateAreaEffectColor(Color color)
        {
            _currentAoeColor = color;
            if (_previewEffect != null)
            {
                _previewEffect.Color = color;
                RedrawAreaEffects();
            }
        }

        /// <summary>
        /// Redraws all area effects
        /// </summary>
        private void RedrawAreaEffects()
        {
            using var dc = _areaEffectVisual.RenderOpen();

            // Draw active effects
            foreach (var effect in _areaEffectService.ActiveEffects)
            {
                DrawAreaEffect(dc, effect);
            }

            // Draw preview effect
            if (_previewEffect != null)
            {
                DrawAreaEffect(dc, _previewEffect);
            }
        }

        private void DrawAreaEffect(DrawingContext dc, AreaEffect effect)
        {
            var geometry = AreaEffectService.GetEffectGeometry(effect, GridCellSize);

            // ✅ Get cached brushes for this color
            var (fillBrush, outlinePen, previewOutlinePen) = GetEffectBrushes(effect.Color);
            var pen = effect.IsPreview ? previewOutlinePen : outlinePen;

            dc.DrawGeometry(fillBrush, pen, geometry);

            // Draw origin point for cones and lines
            if (effect.Shape == AreaEffectShape.Cone || effect.Shape == AreaEffectShape.Line)
            {
                double originX = effect.Origin.X * GridCellSize;
                double originY = effect.Origin.Y * GridCellSize;

                // ✅ Use cached pen
                dc.DrawEllipse(
                    Brushes.White,
                    AreaEffectOriginPen,
                    new Point(originX, originY),
                    5, 5);
            }

            // Draw label for placed effects
            if (!effect.IsPreview && !string.IsNullOrEmpty(effect.Name))
            {
                var bounds = geometry.Bounds;
                var text = new FormattedText(
                    effect.Name,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    CachedTypefaces.SegoeUI,  // ✅ Use cached typeface
                    12,
                    Brushes.White,
                    1.0);

                // Draw text background
                var textPos = new Point(
                    bounds.X + (bounds.Width - text.Width) / 2,
                    bounds.Y + (bounds.Height - text.Height) / 2);

                // ✅ Use cached brush
                dc.DrawRectangle(
                    AreaEffectLabelBackgroundBrush,
                    null,
                    new Rect(textPos.X - 4, textPos.Y - 2, text.Width + 8, text.Height + 4));

                dc.DrawText(text, textPos);
            }
        }

        /// <summary>
        /// Calculates the angle from origin to target point
        /// </summary>
        private double CalculateAngle(Point origin, Point target)
        {
            double dx = target.X - origin.X;
            double dy = target.Y - origin.Y;
            return Math.Atan2(dy, dx) * 180.0 / Math.PI;
        }

        #endregion

        #region Lighting (with dirty flag optimization)

        // Lighting dirty flag system (add to States region if not already there)
        // private bool _lightingDirty = true;
        // private int _lightingWallVersion = -1;  
        // private int _lightingLightCount = -1;

        public void AddLight(LightSource light)
        {
            _lights.Add(light);
            _spatialIndex.IndexLight(light);
            MarkLightingDirty();
            RedrawLighting();
        }

        public void RemoveLightPublic(LightSource light)
        {
            if (light == null) return;
            _lights.Remove(light);
            MarkLightingDirty();
            RedrawLighting();
        }

        /// <summary>
        /// Call this when something changes that affects lighting calculations
        /// (lights moved, walls changed, doors opened/closed)
        /// </summary>
        public void MarkLightingDirty()
        {
            _lightingDirty = true;
            System.Diagnostics.Debug.WriteLine("Lighting marked dirty");
        }

        /// <summary>
        /// Force a full lighting recalculation (useful after major changes)
        /// </summary>
        public void InvalidateLighting()
        {
            _lightingDirty = true;
            _lightingWallVersion = -1;
            _lightingLightCount = -1;
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

                    // ✅ Get cached gradient brush
                    var rg = GetLightGradient(light.Intensity);

                    if (litPolygonGrid.Count < 3)
                    {
                        // No walls blocking - draw full circle
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
                        dc.PushClip(litGeometry);
                        dc.PushTransform(new TranslateTransform(centerPixel.X - radiusPx, centerPixel.Y - radiusPx));
                        dc.DrawRectangle(rg, null, new Rect(0, 0, radiusPx * 2, radiusPx * 2));
                        dc.Pop();
                        dc.Pop();
                    }

                    // ✅ Draw light source indicator with cached resources
                    dc.DrawEllipse(LightCenterBrush, LightIndicatorPen, centerPixel, 8, 8);
                }
            }
        }

        /// <summary>
        /// Draws a single light source with shadow casting
        /// </summary>
        private void DrawLight(DrawingContext dc, LightSource light)
        {
            var centerGrid = light.CenterGrid;
            var centerPixel = new Point(
                centerGrid.X * GridCellSize + GridCellSize / 2.0,
                centerGrid.Y * GridCellSize + GridCellSize / 2.0);
            double radiusPx = Math.Max(GridCellSize, light.RadiusSquares * GridCellSize);

            // Compute lit polygon using wall service (this is the expensive part!)
            var litPolygonGrid = _wallService.ComputeLitPolygon(centerGrid, light.RadiusSquares, 180);

            if (litPolygonGrid.Count < 3)
            {
                // No walls blocking - draw full circle
                var rg = CreateLightGradientInternal(light.Intensity);
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
                var rg = CreateLightGradientInternal(light.Intensity);

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

        /// <summary>
        /// Gets or creates a cached light gradient for the given intensity.
        /// Intensities are rounded to 2 decimal places for better cache hits.
        /// </summary>
        private RadialGradientBrush GetLightGradient(double intensity)
        {
            // Round to 2 decimal places for better cache hits
            double roundedIntensity = Math.Round(intensity, 2);

            if (_lightGradientCache.TryGetValue(roundedIntensity, out var cached))
            {
                return cached;
            }

            // Evict oldest if cache is full
            if (_lightGradientCache.Count >= MaxLightGradientCacheSize)
            {
                var firstKey = _lightGradientCache.Keys.First();
                _lightGradientCache.Remove(firstKey);
            }

            // Create new gradient
            var rg = new RadialGradientBrush();
            byte centerAlpha = (byte)(200 * roundedIntensity);
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(centerAlpha, 255, 255, 200), 0.0));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(centerAlpha * 0.5), 255, 220, 150), 0.5));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 200, 100), 1.0));
            rg.Freeze();

            _lightGradientCache[roundedIntensity] = rg;
            return rg;
        }

        /// <summary>
        /// Creates a new light gradient brush (internal, used by cache)
        /// </summary>
        private static RadialGradientBrush CreateLightGradientInternal(double intensity)
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

        #region Public API: commit previewed path with animation
        public async Task CommitPreviewedPathAsync()
        {
            if (_lastPreviewPath == null || _lastPreviewPath.Count == 0 || SelectedToken == null) return;

            var tokenVis = GetTokenVisual(SelectedToken.Id);
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
        /// <summary>
        /// Creates an EncounterDto for saving the current state
        /// </summary>
        public EncounterDto GetEncounterDto()
        {
            var dto = new EncounterDto();

            // Map image
            if (MapImage?.Source is BitmapImage bi && bi.UriSource != null)
                dto.MapImagePath = bi.UriSource.LocalPath;
            else
                dto.MapImagePath = null;

            // Tokens
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
                        MaxHP = t.MaxHP,
                        AC = t.ArmorClass,
                        Initiative = t.Initiative,
                        InitiativeMod = t.InitiativeModifier,
                        Speed = t.Speed,
                        IsPlayer = t.IsPlayer,
                        SizeInSquares = t.SizeInSquares,
                        IconPath = (t.Image is BitmapImage b && b.UriSource != null) ? b.UriSource.LocalPath : null
                    });
                }
            }

            // Walls
            foreach (var wall in _wallService.Walls)
            {
                dto.Walls.Add(new WallDto
                {
                    StartX = wall.StartPoint.X,
                    StartY = wall.StartPoint.Y,
                    EndX = wall.EndPoint.X,
                    EndY = wall.EndPoint.Y,
                    WallType = wall.WallType.ToString(),
                    IsOpen = wall.IsOpen,
                    Label = wall.Label
                });
            }

            // Lights
            dto.Lights = _lights.Select(l => new LightDto
            {
                X = l.CenterGrid.X,
                Y = l.CenterGrid.Y,
                RadiusSquares = l.RadiusSquares,
                Intensity = l.Intensity
            }).ToList();

            return dto;
        }

        /// <summary>
        /// Loads an encounter from a DTO
        /// </summary>
        public void LoadEncounterDto(EncounterDto dto)
        {
            // Map image
            if (!string.IsNullOrEmpty(dto.MapImagePath) && System.IO.File.Exists(dto.MapImagePath))
            {
                MapImage.Source = new BitmapImage(new Uri(dto.MapImagePath));
                MapImage.SetValue(Canvas.ZIndexProperty, -100);
            }

            // Tokens: clear and add
            Tokens?.Clear();
            foreach (var td in dto.Tokens)
            {
                var token = new Token
                {
                    Name = td.Name,
                    HP = td.MaxHP,
                    MaxHP = td.MaxHP,
                    ArmorClass = td.AC,
                    Initiative = td.Initiative,
                    InitiativeModifier = td.InitiativeMod,
                    Speed = td.Speed,
                    IsPlayer = td.IsPlayer,
                    GridX = td.GridX,
                    GridY = td.GridY,
                    SizeInSquares = td.SizeInSquares
                };

                if (!string.IsNullOrEmpty(td.IconPath) && System.IO.File.Exists(td.IconPath))
                {
                    token.Image = new BitmapImage(new Uri(td.IconPath));
                }
                Tokens?.Add(token);
            }

            // Walls: clear and add
            _wallService.Clear();
            if (dto.Walls != null)
            {
                foreach (var wd in dto.Walls)
                {
                    var wall = new Wall
                    {
                        StartPoint = new Point(wd.StartX, wd.StartY),
                        EndPoint = new Point(wd.EndX, wd.EndY),
                        IsOpen = wd.IsOpen,
                        Label = wd.Label
                    };

                    // Parse wall type
                    if (Enum.TryParse<WallType>(wd.WallType, out var wallType))
                    {
                        wall.WallType = wallType;
                    }

                    _wallService.AddWall(wall);
                }
            }

            // Lights: clear and add
            _lights.Clear();
            if (dto.Lights != null)
            {
                foreach (var ld in dto.Lights)
                {
                    _lights.Add(new LightSource
                    {
                        CenterGrid = new Point(ld.X, ld.Y),
                        RadiusSquares = ld.RadiusSquares,
                        Intensity = ld.Intensity
                    });
                }
            }

            RebuildTokenVisuals();
            RedrawWalls();
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

        /// <summary>
        /// Sets the grid boundaries
        /// </summary>
        public void SetGridBounds(int minX, int minY, int maxX, int maxY)
        {
            _gridMinX = minX;
            _gridMinY = minY;
            _gridMaxX = maxX;
            _gridMaxY = maxY;
            ClampPanToBoundaries();
            UpdateGridVisual();
        }

        /// <summary>
        /// Clamps the pan so the view stays within grid boundaries
        /// </summary>
        private void ClampPanToBoundaries()
        {
            // Calculate the world bounds
            double minWorldX = _gridMinX * GridCellSize;
            double minWorldY = _gridMinY * GridCellSize;
            double maxWorldX = _gridMaxX * GridCellSize;
            double maxWorldY = _gridMaxY * GridCellSize;

            // Get the current view size in world coordinates
            double viewWidth = ActualWidth / _zoom.ScaleX;
            double viewHeight = ActualHeight / _zoom.ScaleY;

            // Calculate the current view position in world coordinates
            double viewLeft = -_pan.X / _zoom.ScaleX;
            double viewTop = -_pan.Y / _zoom.ScaleY;

            // Clamp so we can't scroll past the grid edges (with some padding)
            double padding = GridCellSize * 2;

            // Don't allow scrolling to show negative coordinates
            if (viewLeft < minWorldX - padding)
            {
                _pan.X = -(minWorldX - padding) * _zoom.ScaleX;
            }
            if (viewTop < minWorldY - padding)
            {
                _pan.Y = -(minWorldY - padding) * _zoom.ScaleY;
            }

            // Don't allow scrolling too far past the max
            if (viewLeft + viewWidth > maxWorldX + padding)
            {
                _pan.X = -(maxWorldX + padding - viewWidth) * _zoom.ScaleX;
            }
            if (viewTop + viewHeight > maxWorldY + padding)
            {
                _pan.Y = -(maxWorldY + padding - viewHeight) * _zoom.ScaleY;
            }
        }

        /// <summary>
        /// Converts a column index to a letter label (0=A, 1=B, ..., 25=Z, 26=AA, etc.)
        /// </summary>
        private string GetColumnLabel(int columnIndex)
        {
            string result = "";
            int index = columnIndex;
            while (index >= 0)
            {
                result = (char)('A' + (index % 26)) + result;
                index = index / 26 - 1;
            }
            return result;
        }

        private void PlaceAreaEffect()
        {
            if (_previewEffect == null) return;

            // Create a copy of the preview as a placed effect
            var placedEffect = new AreaEffect
            {
                Name = _previewEffect.Name,
                Shape = _previewEffect.Shape,
                SizeInFeet = _previewEffect.SizeInFeet,
                WidthInFeet = _previewEffect.WidthInFeet,
                Origin = _previewEffect.Origin,
                DirectionAngle = _previewEffect.DirectionAngle,
                Color = _previewEffect.Color,
                IsPreview = false
            };

            _areaEffectService.AddEffect(placedEffect);

            // Log the action
            AddToActionLog("AoE", $"Placed {placedEffect.Name ?? placedEffect.Shape.ToString()} ({placedEffect.SizeInFeet} ft)");

            // Reset for next placement (keep the same settings)
            _previewEffect = new AreaEffect
            {
                Name = placedEffect.Name,
                Shape = placedEffect.Shape,
                SizeInFeet = placedEffect.SizeInFeet,
                WidthInFeet = placedEffect.WidthInFeet,
                Color = placedEffect.Color,
                IsPreview = true
            };

            RedrawAreaEffects();
        }

        /// <summary>
        /// Gets the appropriate cached brush for an HP percentage
        /// </summary>
        private SolidColorBrush GetHPBarBrush(double hpPercent)
        {
            if (hpPercent > 0.5) return HPBarGreenBrush;
            if (hpPercent > 0.25) return HPBarYellowBrush;
            return HPBarRedBrush;
        }

        /// <summary>
        /// Converts a world coordinate to screen coordinate
        /// </summary>
        public Point WorldToScreen(Point worldPoint)
        {
            var x = worldPoint.X * _zoom.ScaleX + _pan.X;
            var y = worldPoint.Y * _zoom.ScaleY + _pan.Y;
            return new Point(x, y);
        }

        /// <summary>
        /// Gets the visual container for a token by ID. O(1) lookup.
        /// </summary>
        /// <param name="tokenId">The token's GUID</param>
        /// <returns>The Grid container, or null if not found</returns>
        private Grid GetTokenVisual(Guid tokenId)
        {
            _tokenVisualLookup.TryGetValue(tokenId, out var container);
            return container;
        }

        /// <summary>
        /// Gets or creates cached brushes and pens for an area effect color.
        /// </summary>
        private (SolidColorBrush fill, Pen outline, Pen previewOutline) GetEffectBrushes(Color color)
        {
            if (_effectBrushCache.TryGetValue(color, out var cached))
            {
                return cached;
            }

            // Evict oldest if cache is full (simple LRU approximation)
            if (_effectBrushCache.Count >= MaxEffectBrushCacheSize)
            {
                // Remove first entry (not perfect LRU but simple and effective)
                var firstKey = _effectBrushCache.Keys.First();
                _effectBrushCache.Remove(firstKey);
            }

            // Create and cache new brushes
            var fillBrush = new SolidColorBrush(color);
            fillBrush.Freeze();

            var outlineColor = Color.FromArgb(200, color.R, color.G, color.B);
            var outlineBrush = new SolidColorBrush(outlineColor);
            outlineBrush.Freeze();

            var outlinePen = new Pen(outlineBrush, 3);
            outlinePen.Freeze();

            var previewPen = new Pen(outlineBrush, 2) { DashStyle = DashStyles.Dash };
            previewPen.Freeze();

            var entry = (fillBrush, outlinePen, previewPen);
            _effectBrushCache[color] = entry;
            return entry;
        }
        #endregion
    }
}