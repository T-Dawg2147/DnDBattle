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
using DnDBattle.Services.TileService;
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
using DnDBattle.Views.Editors;
using DnDBattle.Views.TileMap;
using Action = DnDBattle.Models.Combat.Action;
using Condition = DnDBattle.Models.Effects.Condition;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Main BattleGridControl class - provides the battle map canvas for D&D encounters.
    /// This partial class contains core fields, properties, events and initialization.
    /// Additional functionality is organized in separate partial class files:
    /// - BattleGridControl.TokenRendering.cs - Token visual creation and management
    /// - BattleGridControl.FogOfWar.cs - Fog of war functionality
    /// - BattleGridControl.WallDrawing.cs - Wall drawing and management
    /// - BattleGridControl.AreaEffects.cs - Area effect (AoE) placement and rendering
    /// - BattleGridControl.Lighting.cs - Light source management
    /// - BattleGridControl.Measurement.cs - Measurement tools, path preview, movement overlay
    /// - BattleGridControl.TileMap.cs - Tile map loading and rendering
    /// - BattleGridControl.TileInteractions.cs - Tile interactions (traps, secrets, etc.)
    /// - BattleGridControl.Conditions.cs - Condition badges and visual effects
    /// </summary>
    public partial class BattleGridControl : UserControl
    {
        #region Events

        public event Action<Token> TokenDoubleClicked;
        public event Action<Token> RequestDeleteToken;
        public event Action<Token> RequestDuplicateToken;
        public event Action<Token> RequestEditToken;
        public event Action<Point> RequestAddTokenAtPosition;
        public event Action<Token> TokenAddedToMap;
        public event Action<Token> TargetSelected;
        public event Action<string> CurrentCellChanged;

        #endregion

        #region Dependency Properties

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

        #endregion

        #region Public Properties

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
        public WallService WallService => _wallService;

        #endregion

        #region Private Fields - Visual Components

        private readonly SpatialIndex _spatialIndex = new SpatialIndex(1);
        private readonly List<LightSource> _lights = new List<LightSource>();

        private readonly DrawingVisual _movementVisual = new DrawingVisual();
        private readonly DrawingVisual _pathVisual = new DrawingVisual();
        private readonly DrawingVisual _lightingVisual = new DrawingVisual();
        private readonly DrawingVisual _wallVisual = new DrawingVisual();
        private readonly DrawingVisual _measureVisual = new DrawingVisual();
        private readonly DrawingVisual _areaEffectVisual = new DrawingVisual();
        private readonly DrawingVisual _tileMapVisual = new DrawingVisual();
        private DrawingVisual _fogVisual = new DrawingVisual();

        #endregion

        #region Private Fields - Services

        private readonly WallService _wallService = new WallService();
        private readonly SpawnPointService _spawnService = new SpawnPointService();
        private readonly HazardTrackingService _hazardTracking = new HazardTrackingService();
        private readonly MetadataInteractionService _metadataService = new MetadataInteractionService();
        private readonly TrapTriggerService _trapService = new TrapTriggerService();
        private readonly AreaEffectService _areaEffectService = new AreaEffectService();
        private FogOfWarService _fogService;

        #endregion

        #region Private Fields - Transform State

        private TranslateTransform _pan = new TranslateTransform();
        private ScaleTransform _zoom = new ScaleTransform(1, 1);
        private TransformGroup _transformGroup = new TransformGroup();
        private bool _isPanning = false;
        private Point _lastPanPoint;
        private bool _isMiddlePanning = false;
        private Point _middlePanLast;

        #endregion

        #region Private Fields - Grid State

        private int _gridMinX = 0;
        private int _gridMinY = 0;
        private int _gridMaxX = 100;
        private int _gridMaxY = 100;
        private int _gridWidth = 200, _gridHeight = 200;

        #endregion

        #region Private Fields - Tile Map

        private TileMap _loadedTileMap;

        /// <summary>Gets or sets the currently loaded tile map.</summary>
        public TileMap TileMap
        {
            get => _loadedTileMap;
            set => LoadTileMap(value);
        }

        #endregion

        #region Private Fields - Area Effect State

        private AreaEffect _previewEffect;
        private bool _isPlacingAreaEffect;
        private AreaEffectShape? _currentAoeShape;
        private int _currentAoeSize = 20;
        private Color _currentAoeColor = Color.FromArgb(120, 255, 69, 0);

        #endregion

        #region Private Fields - Wall Drawing State

        private bool _wallDrawMode = false;
        private Point? _wallDrawStart = null;
        private Point _wallDrawPreview;
        private WallType _currentWallType = WallType.Solid;
        private Wall _selectedWall = null;
        private bool _isDraggingWallEndpoint = false;
        private bool _draggingWallIsStart = false;
        private bool _roomDrawMode = false;
        private List<Point> _roomVertices = new List<Point>();

        #endregion

        #region Private Fields - Measurement State

        private bool _measureMode = false;
        private Point? _measureStart = null;
        private Point _measureEnd;
        private bool _showCoordinateRulers = true;

        #endregion

        #region Private Fields - Token Drag State

        private bool _isDraggingToken = false;
        private FrameworkElement _draggingVisual;
        private Point _dragOrigin;
        private int _dragStartGridX = 0;
        private int _dragStartGridY = 0;

        #endregion

        #region Private Fields - Path Preview State

        private List<(int x, int y)> _lastPreviewPath = null;
        private HashSet<int> _lastAooIndices = null;

        #endregion

        #region Private Fields - Drop Event Handling

        private DateTime _lastDropTime = DateTime.MinValue;
        private string _lastDropPrototypeId = null;
        private readonly TimeSpan _duplicateDropThreshold = TimeSpan.FromMilliseconds(300);

        #endregion

        #region Private Fields - Targeting State

        private bool _isInTargetingMode = false;
        private TargetingState _currentTargetingState;
        private Dictionary<Token, Border> _targetHighlights = new Dictionary<Token, Border>();

        #endregion

        #region Private Fields - Fog of War State

        private FogOfWarState _fogOfWar = new FogOfWarState();
        private Canvas _fogCanvas;
        private bool _isFogBrushActive = false;
        private FogShapeTool _currentFogShapeTool = FogShapeTool.None;
        private Point? _fogShapeStartPoint;

        #endregion

        #region Private Fields - UI State

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

        #endregion

        #region Constructor

        public BattleGridControl()
        {
            InitializeComponent();

            GridCellSize = Options.DefaultGridCellSize;
            SetGridMaxSize(Options.GridMaxWidth, Options.GridMaxHeight);
            UpdateShadowSoftness();

            _transformGroup.Children.Add(_zoom);
            _transformGroup.Children.Add(_pan);
            RenderCanvas.RenderTransform = _transformGroup;

            // Add visual overlay layers in correct Z-order
            AddVisualOverlay(_tileMapVisual, 5);
            AddVisualOverlay(_lightingVisual, 50, makeBlur: true);
            AddVisualOverlay(_movementVisual, 60);
            AddVisualOverlay(_pathVisual, 65);
            AddVisualOverlay(_wallVisual, 70);
            AddVisualOverlay(_measureVisual, 80);
            AddVisualOverlay(_areaEffectVisual, 85);
            AddVisualOverlay(_visionVisual, 90);
            AddVisualOverlay(_fogVisual, 2500);

            _wallService.WallsChanged += () => { InvalidateShadowCache(); RedrawWalls(); };

            // Wire up metadata services
            SetupMetadataServices();

            InitializePhase5Visuals();

            Loaded += BattleGridControl_Loaded;
            KeyDown += BattleGridControl_KeyDown;

            MouseDown += (s, e) => Focus();
        }

        #endregion

        #region Initialization

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
            KeyDown += BattleGridControl_KeyDown;
            MouseDown += (s, e) => Focus();
            SizeChanged += (s, e) =>
            {
                UpdateGridVisual();
                DrawCoordinateRulers();
            };
        }

        private void AddVisualOverlay(DrawingVisual visual, int zIndex, bool makeBlur = false)
        {
            var wrapper = new FrameworkElementForVisual(visual) { IsHitTestVisible = false, Tag = "Overlay" };
            if (makeBlur)
                wrapper.Effect = new BlurEffect { Radius = Options.ShadowSoftnessPx, RenderingBias = RenderingBias.Quality };
            RenderCanvas.Children.Add(wrapper);
            Canvas.SetZIndex(RenderCanvas.Children[^1], zIndex);
        }

        #endregion

        #region Visual Host Helper Class

        private class FrameworkElementForVisual : FrameworkElement
        {
            private readonly DrawingVisual _visual;
            public FrameworkElementForVisual(DrawingVisual visual)
            {
                _visual = visual;
                AddVisualChild(visual);
                AddLogicalChild(visual);
                                
            }
            protected override int VisualChildrenCount => 1;
            protected override Visual GetVisualChild(int index) => _visual;
        }

        #endregion

        #region Dependency Property Callbacks

        private static void OnGridPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (BattleGridControl)d;
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
            ctrl.RedrawMovementOverlay();
            ctrl.ClearPathVisual();
        }

        private void Tokens_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RebuildTokenVisuals();

        #endregion

        #region Grid Visual Updates

        // VISUAL REFRESH - GRID
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

            GridHost.DrawGridViewport(GridCellSize, worldRect, 16, showCoordinates: false, showGrid: ShowGrid);
            DrawCoordinateRulers();
        }

        #endregion

        #region Options Getters

        // VISUAL REFRESH - GRID
        public void SetGridMaxSize(int maxWidth, int maxHeight)
        {
            _gridWidth = Math.Max(1, maxWidth);
            _gridHeight = Math.Max(1, maxHeight);
            UpdateGridVisual();
            RedrawMovementOverlay();
            RedrawLighting();
        }

        // VISUAL REFRESH - SHADOW
        public void UpdateShadowSoftness()
        {
            var wrapper = RenderCanvas.Children.OfType<FrameworkElement>().FirstOrDefault(w => Panel.GetZIndex(w) == 50);
            if (wrapper != null) wrapper.Effect = new BlurEffect { Radius = Options.ShadowSoftnessPx, RenderingBias = RenderingBias.Quality };
        }

        #endregion

        #region Combat Events

        public void OnTokenTurnStart(Token token)
        {
            _hazardTracking.ApplyStartOfTurnDamage(token, _metadataService);
        }

        public void OnTokenTurnEnd(Token token)
        {
            _hazardTracking.ApplyEndOfTurnDamage(token, _metadataService);
        }

        public void OnCombatStarted()
        {
            CheckSpawnTriggers(combatJustStarted: true);
        }

        public void OnRoundChanged(int newRound)
        {
            CheckSpawnTriggers();
        }

        #endregion

        #region Coordinate and Transform Utilities

        private Point ScreenToWorld(Point screenPt)
        {
            var inv = _transformGroup.Inverse;
            if (inv != null) return inv.Transform(screenPt);
            return screenPt;
        }

        public Point ScreenToWorldPublic(Point screenPoint)
        {
            var inv = _transformGroup.Inverse;
            if (inv != null) return inv.Transform(screenPoint);
            return screenPoint;
        }

        public Point WorldToScreen(Point worldPoint)
        {
            var x = worldPoint.X * _zoom.ScaleX + _pan.X;
            var y = worldPoint.Y * _zoom.ScaleY + _pan.Y;
            return new Point(x, y);
        }

        public void SetGridBounds(int minX, int minY, int maxX, int maxY)
        {
            _gridMinX = minX;
            _gridMinY = minY;
            _gridMaxX = maxX;
            _gridMaxY = maxY;
            ClampPanToBoundaries();
            UpdateGridVisual();
        }

        private void ClampPanToBoundaries()
        {
            double minWorldX = _gridMinX * GridCellSize;
            double minWorldY = _gridMinY * GridCellSize;
            double maxWorldX = _gridMaxX * GridCellSize;
            double maxWorldY = _gridMaxY * GridCellSize;

            double viewWidth = ActualWidth / _zoom.ScaleX;
            double viewHeight = ActualHeight / _zoom.ScaleY;

            double viewLeft = -_pan.X / _zoom.ScaleX;
            double viewTop = -_pan.Y / _zoom.ScaleY;

            double padding = GridCellSize * 2;

            if (viewLeft < minWorldX - padding)
            {
                _pan.X = -(minWorldX - padding) * _zoom.ScaleX;
            }
            if (viewTop < minWorldY - padding)
            {
                _pan.Y = -(minWorldY - padding) * _zoom.ScaleY;
            }

            if (viewLeft + viewWidth > maxWorldX + padding)
            {
                _pan.X = -(maxWorldX + padding - viewWidth) * _zoom.ScaleX;
            }
            if (viewTop + viewHeight > maxWorldY + padding)
            {
                _pan.Y = -(maxWorldY + padding - viewHeight) * _zoom.ScaleY;
            }
        }

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

        #endregion

        #region Visual Refresh Orchestration

        /// <summary>
        /// Refreshes ALL visuals on the battle grid. Use sparingly — this is expensive.
        /// Calls every category-level refresh method.
        /// </summary>
        // VISUAL REFRESH - ALL
        public void RefreshAllVisuals()
        {
            UpdateGridVisual();
            DrawCoordinateRulers();
            RebuildTokenVisuals();    // This internally calls LayoutTokens(), RedrawLighting(), RedrawAuras()
            RedrawWalls();
            RedrawMovementOverlay();
            RedrawPathVisual();
            RedrawMovementCostPreview();
            RedrawFog();
            RedrawVisionOverlay();
            RefreshAreaEffectsDisplay();
        }

        /// <summary>
        /// Refreshes only token-related visuals (tokens, HP bars, conditions, auras).
        /// Lighter than RefreshAllVisuals but still rebuilds all tokens.
        /// </summary>
        // VISUAL REFRESH - TOKEN_RENDERING
        public void RefreshTokenVisuals()
        {
            RebuildTokenVisuals(); // This already calls LayoutTokens(), UpdateGridVisual(), RedrawLighting(), RedrawAuras()
        }

        /// <summary>
        /// Refreshes all map overlay layers WITHOUT rebuilding tokens.
        /// Use after wall/lighting/fog changes that don't affect tokens.
        /// </summary>
        // VISUAL REFRESH - GRID
        public void RefreshMapOverlays()
        {
            UpdateGridVisual();
            RedrawLighting();
            RedrawWalls();
            RedrawMovementOverlay();
            RedrawPathVisual();
            RedrawFog();
            RedrawVisionOverlay();
            RefreshAreaEffectsDisplay();
        }

        #endregion

        #region Grid Cell Display

        // VISUAL REFRESH - GRID
        private void UpdateCurrentCellDisplay(Point gridPoint)
        {
            int cellX = (int)Math.Floor(gridPoint.X);
            int cellY = (int)Math.Floor(gridPoint.Y);

            if (cellX >= _gridMinX && cellY >= _gridMinY && cellX < _gridMaxX && cellY < _gridMaxY)
            {
                string colLabel = GetColumnLabel(cellX);
                CurrentCellText = $"{colLabel}{cellY + 1}";
            }
            else
            {
                CurrentCellText = "-";
            }
        }

        #endregion

        #region Pan and Zoom

        // VISUAL REFRESH - PAN_ZOOM
        public void PanBy(double dx, double dy)
        {
            _pan.X = dx;
            _pan.Y = dy;
            UpdateGridVisual();
            RedrawLighting();
            RedrawMovementOverlay();
            RedrawPathVisual();
        }

        private void ZoomAtCenter(double zoomFactor)
        {
            var centerX = ActualWidth / 2;
            var centerY = ActualHeight / 2;

            double absX = centerX * _zoom.ScaleX + _pan.X;
            double absY = centerY * _zoom.ScaleY + _pan.Y;

            _zoom.ScaleX *= zoomFactor;
            _zoom.ScaleY *= zoomFactor;

            _zoom.ScaleX = Math.Max(0.1, Math.Min(5.0, _zoom.ScaleX));
            _zoom.ScaleY = Math.Max(0.1, Math.Min(5.0, _zoom.ScaleY));

            _pan.X = absX - centerX * _zoom.ScaleX;
            _pan.Y = absY - centerY * _zoom.ScaleY;
        }

        #endregion

        #region Action Logging

        private void AddToActionLog(string source, string message)
        {
            try
            {
                var mw = Application.Current?.MainWindow;
                if (mw != null && mw.DataContext is DnDBattle.ViewModels.MainViewModel vm)
                {
                    var entry = new DnDBattle.Models.Combat.ActionLogEntry { Source = source, Message = message, Timestamp = DateTime.Now };
                    vm.ActionLog.Insert(0, entry);
                }
            }
            catch
            {
                Debug.WriteLine("Failed to write action log entry: " + message);
            }
        }

        #endregion
    }
}
