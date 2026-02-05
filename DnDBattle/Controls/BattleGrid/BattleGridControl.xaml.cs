using DnDBattle.Controls.BattleGrid.Managers;
using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.FogOfWar;
using DnDBattle.Services.TileService;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Controls.BattleGrid
{
    public partial class BattleGridControl : UserControl
    {
        #region Events

        public event Action<Token> TokenClicked;
        public event Action<Token> TokenDoubleClicked;
        public event Action<Token, int, int, int, int> TokenMoved;
        public event Action<string, string> LogMessage;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty GridWidthProperty =
            DependencyProperty.Register(nameof(GridWidth), typeof(int), typeof(BattleGridControl),
                new PropertyMetadata(50, OnGridPropertyChanged));

        public static readonly DependencyProperty GridHeightProperty =
            DependencyProperty.Register(nameof(GridHeight), typeof(int), typeof(BattleGridControl),
                new PropertyMetadata(50, OnGridPropertyChanged));

        public static readonly DependencyProperty GridCellSizeProperty =
            DependencyProperty.Register(nameof(GridCellSize), typeof(double), typeof(BattleGridControl),
                new PropertyMetadata(48.0, OnGridPropertyChanged));

        public static readonly DependencyProperty ShowGridProperty =
            DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(BattleGridControl),
                new PropertyMetadata(true, OnGridPropertyChanged));

        public static readonly DependencyProperty LockToGridProperty =
            DependencyProperty.Register(nameof(LockToGrid), typeof(bool), typeof(BattleGridControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty TokensProperty =
            DependencyProperty.Register(nameof(Tokens), typeof(System.Collections.ObjectModel.ObservableCollection<Token>),
                typeof(BattleGridControl), new PropertyMetadata(null, OnTokensChanged));

        public static readonly DependencyProperty SelectedTokenProperty =
            DependencyProperty.Register(nameof(SelectedToken), typeof(Token), typeof(BattleGridControl), new PropertyMetadata(null, OnSelectedTokenChanged));

        public int GridWidth
        {
            get => (int)GetValue(GridWidthProperty);
            set => SetValue(GridWidthProperty, value);
        }

        public int GridHeight
        {
            get => (int)GetValue(GridHeightProperty);
            set => SetValue(GridHeightProperty, value);
        }

        public double GridCellSize
        {
            get => (double)GetValue(GridCellSizeProperty);
            set => SetValue(GridCellSizeProperty, value);
        }

        public bool ShowGrid
        {
            get => (bool)GetValue(ShowGridProperty);
            set => SetValue(ShowGridProperty, value);
        }

        public bool LockToGrid
        {
            get => (bool)GetValue(LockToGridProperty);
            set => SetValue(LockToGridProperty, value);
        }

        public System.Collections.ObjectModel.ObservableCollection<Token> Tokens
        {
            get => (System.Collections.ObjectModel.ObservableCollection<Token>)GetValue(TokensProperty);
            set => SetValue(TokensProperty, value);
        }

        public Token SelectedToken
        {
            get => (Token)GetValue(SelectedTokenProperty);
            set => SetValue(SelectedTokenProperty, value);
        }

        #endregion

        #region Fields - Managers

        private readonly BattleGridRenderManager _renderManager;
        private readonly BattleGridTokenManager _tokenManager;
        private readonly BattleGridInputManager _inputManager;
        private readonly BattleGridTileMapManager _tileMapManager;
        private readonly BattleGridFogOfWarManager _fogManager;

        #endregion

        #region Fields - Mouse Interaction State

        private bool _isPanning = false;
        private Point _panStartPoint;

        private Token _draggedToken = null;
        private FrameworkElement _draggedTokenVisual = null;
        private Point _tokenDragStartPoint;
        private int _tokenDragStartGridX;
        private int _tokenDragStartGridY;

        private DateTime _lastViewportUpdate = DateTime.MinValue;

        // Fog interaction
        private bool _isFogBrushActive = false;
        private Point _lastFogBrushPoint = new Point(-1, -1);

        // Fog shape tools ← ADD THESE
        private DnDBattle.Views.FogShapeTool? _activeFogShapeTool = null;
        private Point? _fogShapeStartPoint = null;
        private System.Windows.Shapes.Rectangle _fogShapePreview = null;
        private System.Windows.Shapes.Ellipse _fogShapeCirclePreview = null;

        #endregion

        #region Constructor

        public BattleGridControl()
        {
            InitializeComponent();

            Debug.WriteLine("[BattleGrid] Constructor START");

            // Initialize managers
            _renderManager = new BattleGridRenderManager(RenderCanvas, RulerCanvas);
            _tokenManager = new BattleGridTokenManager(RenderCanvas);
            _inputManager = new BattleGridInputManager();
            _tileMapManager = new BattleGridTileMapManager();
            _fogManager = new BattleGridFogOfWarManager();

            // Setup events
            SetupManagerEvents();

            // Wire up control events
            Loaded += BattleGridControl_Loaded;
            SizeChanged += BattleGridControl_SizeChanged;

            Debug.WriteLine("[BattleGrid] Constructor END");
        }

        #endregion

        #region Initialization

        private void BattleGridControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[BattleGrid] Control loaded");

            _renderManager.SetViewportSize(ActualWidth, ActualHeight);
            _renderManager.SetGridDimensions(GridWidth, GridHeight, GridCellSize);
            _renderManager.ResetView();

            _renderManager.RenderGrid(GridWidth, GridHeight, GridCellSize, ShowGrid);
            _renderManager.RenderRulers(GridWidth, GridHeight, GridCellSize, ActualWidth, ActualHeight);

            if (Tokens != null)
            {
                _tokenManager.SetTokens(Tokens);
                _tokenManager.RebuildAllTokenVisuals(GridCellSize);
            }

            Focusable = true;
            Focus();
        }

        private void BattleGridControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Debug.WriteLine($"[BattleGrid] Size changed: {e.NewSize}");
            _renderManager.SetViewportSize(e.NewSize.Width, e.NewSize.Height);
            OnViewportChanged();
        }

        private void SetupManagerEvents()
        {
            // Render manager
            _renderManager.ViewportChanged += OnViewportChanged;

            // Token manager
            _tokenManager.TokenClicked += (token) => TokenClicked?.Invoke(token);
            _tokenManager.TokenDoubleClicked += (token) => TokenDoubleClicked?.Invoke(token);
            _tokenManager.TokenMoved += (token, oldX, oldY, newX, newY) => TokenMoved?.Invoke(token, oldX, oldY, newX, newY);
            _tokenManager.LogMessage += (category, message) => LogMessage?.Invoke(category, message);

            // Input manager
            _inputManager.GridPositionChanged += OnGridPositionChanged;

            // Fog manager ← ADD THIS BLOCK
            _fogManager.FogChanged += OnFogChanged;
        }

        private void OnFogChanged()
        {
            Debug.WriteLine("[BattleGrid] Fog changed - requesting redraw");
            _renderManager.RenderFog(_fogManager, GridCellSize);
        }

        #endregion

        #region Mouse Handling

        private void RenderCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();

            var mousePos = e.GetPosition(RenderCanvas);

            if (e.ChangedButton == MouseButton.Left)
            {
                // CHECK 1A: Are we in fog shape tool mode?
                if (_activeFogShapeTool.HasValue && _activeFogShapeTool.Value != DnDBattle.Views.FogShapeTool.None)
                {
                    var worldPos = ScreenToWorld(mousePos);
                    int gridX = (int)Math.Floor(worldPos.X / GridCellSize);
                    int gridY = (int)Math.Floor(worldPos.Y / GridCellSize);

                    _fogShapeStartPoint = new Point(gridX, gridY);
                    RenderCanvas.CaptureMouse();

                    Debug.WriteLine($"[BattleGrid] Fog shape started at ({gridX},{gridY})");
                    e.Handled = true;
                    return;
                }

                // CHECK 1B: Are we in fog brush mode?
                if (_fogManager.IsEnabled && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    // Ctrl+Click = Fog brush mode
                    _isFogBrushActive = true;
                    var worldPos = ScreenToWorld(mousePos);
                    int gridX = (int)Math.Floor(worldPos.X / GridCellSize);
                    int gridY = (int)Math.Floor(worldPos.Y / GridCellSize);

                    _fogManager.ApplyBrush(gridX, gridY);
                    _lastFogBrushPoint = new Point(gridX, gridY);

                    RenderCanvas.CaptureMouse();
                    Cursor = Cursors.Cross;

                    Debug.WriteLine($"[BattleGrid] Fog brush started at ({gridX},{gridY})");
                    e.Handled = true;
                    return;
                }

                // CHECK 2: Did we click on a token?
                var clickedElement = FindVisualAtPoint(mousePos);

                if (clickedElement?.Tag is Token token)
                {
                    // Start token drag
                    _draggedToken = token;
                    _draggedTokenVisual = clickedElement;
                    _tokenDragStartPoint = mousePos;
                    _tokenDragStartGridX = token.GridX;
                    _tokenDragStartGridY = token.GridY;

                    _draggedTokenVisual.CaptureMouse();
                    Panel.SetZIndex(_draggedTokenVisual, 1000);

                    TokenClicked?.Invoke(token);

                    Debug.WriteLine($"[BattleGrid] Started dragging token: {token.Name}");
                    e.Handled = true;
                    return;
                }
                else
                {
                    // CHECK 3: Start panning
                    _isPanning = true;
                    _panStartPoint = mousePos;
                    RenderCanvas.CaptureMouse();
                    Cursor = Cursors.SizeAll;

                    Debug.WriteLine($"[BattleGrid] Started panning");
                }
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = true;
                _panStartPoint = mousePos;
                RenderCanvas.CaptureMouse();
                Cursor = Cursors.SizeAll;
            }
        }

        private void RenderCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = e.GetPosition(RenderCanvas);
            var transform = _renderManager.GetTransform();

            // Always update cell position display
            UpdateCellPositionDisplay(mousePos, transform);

            // PRIORITY 0: Preview fog shape tool
            if (_fogShapeStartPoint.HasValue && _activeFogShapeTool.HasValue)
            {
                var worldPos = ScreenToWorld(mousePos);
                int gridX = (int)Math.Floor(worldPos.X / GridCellSize);
                int gridY = (int)Math.Floor(worldPos.Y / GridCellSize);

                DrawFogShapePreview(_fogShapeStartPoint.Value, new Point(gridX, gridY));
                e.Handled = true;
                return;
            }

            // PRIORITY 1: Handle fog brush painting
            if (_isFogBrushActive)
            {
                var worldPos = ScreenToWorld(mousePos);
                int gridX = (int)Math.Floor(worldPos.X / GridCellSize);
                int gridY = (int)Math.Floor(worldPos.Y / GridCellSize);

                // Only paint if we moved to a new cell (optimization)
                if (gridX != (int)_lastFogBrushPoint.X || gridY != (int)_lastFogBrushPoint.Y)
                {
                    _fogManager.ApplyBrush(gridX, gridY);
                    _lastFogBrushPoint = new Point(gridX, gridY);
                }

                e.Handled = true;
                return;
            }

            // PRIORITY 2: Handle token dragging
            if (_draggedToken != null && _draggedTokenVisual != null)
            {
                var delta = mousePos - _tokenDragStartPoint;

                var currentLeft = Canvas.GetLeft(_draggedTokenVisual);
                var currentTop = Canvas.GetTop(_draggedTokenVisual);

                Canvas.SetLeft(_draggedTokenVisual, currentLeft + delta.X);
                Canvas.SetTop(_draggedTokenVisual, currentTop + delta.Y);

                _tokenDragStartPoint = mousePos;
                e.Handled = true;
                return;
            }

            // PRIORITY 3: Handle panning
            if (_isPanning)
            {
                var delta = mousePos - _panStartPoint;

                // Apply pan without any clamping or rendering
                _renderManager.ApplyPanWithoutRedraw(delta.X, delta.Y);

                _panStartPoint = mousePos;
                e.Handled = true;
                return;
            }

            // Update cursor based on fog mode
            if (_fogManager.IsEnabled && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Cursor = Cursors.Cross; // Show we're in fog mode
            }
            else if (!_isPanning && _draggedToken == null)
            {
                var element = FindVisualAtPoint(mousePos);
                Cursor = (element?.Tag is Token) ? Cursors.Hand : Cursors.Arrow;
            }
        }

        private void RenderCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(RenderCanvas);

            // Handle fog shape tool completion
            if (_fogShapeStartPoint.HasValue && _activeFogShapeTool.HasValue)
            {
                var worldPos = ScreenToWorld(mousePos);
                int endX = (int)Math.Floor(worldPos.X / GridCellSize);
                int endY = (int)Math.Floor(worldPos.Y / GridCellSize);

                int startX = (int)_fogShapeStartPoint.Value.X;
                int startY = (int)_fogShapeStartPoint.Value.Y;

                if (_activeFogShapeTool == DnDBattle.Views.FogShapeTool.Rectangle)
                {
                    _fogManager.RevealRectangle(startX, startY, endX, endY);
                    LogMessage?.Invoke("Fog", $"Revealed rectangle ({startX},{startY}) to ({endX},{endY})");
                }
                else if (_activeFogShapeTool == DnDBattle.Views.FogShapeTool.Circle)
                {
                    double dx = endX - startX;
                    double dy = endY - startY;
                    int radius = (int)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy));

                    _fogManager.RevealCircle(startX, startY, radius);
                    LogMessage?.Invoke("Fog", $"Revealed circle at ({startX},{startY}) radius {radius}");
                }

                // Clear shape tool
                _fogShapeStartPoint = null;
                _activeFogShapeTool = null;

                // Remove preview
                if (_fogShapePreview != null)
                {
                    RenderCanvas.Children.Remove(_fogShapePreview);
                    _fogShapePreview = null;
                }
                if (_fogShapeCirclePreview != null)
                {
                    RenderCanvas.Children.Remove(_fogShapeCirclePreview);
                    _fogShapeCirclePreview = null;
                }

                RenderCanvas.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;

                e.Handled = true;
                return;
            }

            // Handle fog brush stop
            if (_isFogBrushActive)
            {
                _isFogBrushActive = false;
                _lastFogBrushPoint = new Point(-1, -1);
                RenderCanvas.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;

                Debug.WriteLine($"[BattleGrid] Fog brush stopped");
                e.Handled = true;
                return;
            }

            // Handle token drop
            if (_draggedToken != null && _draggedTokenVisual != null)
            {
                _draggedTokenVisual.ReleaseMouseCapture();
                Panel.SetZIndex(_draggedTokenVisual, 100);

                var left = Canvas.GetLeft(_draggedTokenVisual);
                var top = Canvas.GetTop(_draggedTokenVisual);

                int newGridX, newGridY;

                if (LockToGrid)
                {
                    newGridX = (int)Math.Round(left / GridCellSize);
                    newGridY = (int)Math.Round(top / GridCellSize);
                }
                else
                {
                    newGridX = (int)(left / GridCellSize);
                    newGridY = (int)(top / GridCellSize);
                }

                Canvas.SetLeft(_draggedTokenVisual, newGridX * GridCellSize);
                Canvas.SetTop(_draggedTokenVisual, newGridY * GridCellSize);

                int oldX = _tokenDragStartGridX;
                int oldY = _tokenDragStartGridY;

                _draggedToken.GridX = newGridX;
                _draggedToken.GridY = newGridY;

                if (newGridX != oldX || newGridY != oldY)
                {
                    Debug.WriteLine($"[BattleGrid] Token {_draggedToken.Name} moved from ({oldX},{oldY}) to ({newGridX},{newGridY})");
                    TokenMoved?.Invoke(_draggedToken, oldX, oldY, newGridX, newGridY);
                    LogMessage?.Invoke("Movement", $"{_draggedToken.Name} moved to {GetColumnLabel(newGridX)}{newGridY + 1}");
                }

                _draggedToken = null;
                _draggedTokenVisual = null;
                Cursor = Cursors.Arrow;
                e.Handled = true;
                return;
            }

            // Handle pan end
            if (_isPanning)
            {
                _isPanning = false;
                RenderCanvas.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;

                // NOW do clamping and full render
                _renderManager.FinishInteraction();
                OnViewportChanged();

                Debug.WriteLine($"[BattleGrid] Panning stopped");
            }
        }

        private void RenderCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var mousePos = e.GetPosition(RenderCanvas);
            double zoomFactor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;

            _renderManager.ApplyZoom(zoomFactor, mousePos);

            UpdateZoomDisplay();
            OnViewportChanged();
            e.Handled = true;
        }

        #endregion

        #region Keyboard Handling

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _inputManager.HandleKeyDown(e, GridCellSize);
        }

        #endregion

        #region Helper Methods

        private FrameworkElement FindVisualAtPoint(Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(RenderCanvas, point);

            if (result != null)
            {
                DependencyObject obj = result.VisualHit;

                while (obj != null && obj != RenderCanvas)
                {
                    if (obj is FrameworkElement element && element.Tag is Token)
                    {
                        return element;
                    }
                    obj = VisualTreeHelper.GetParent(obj);
                }
            }

            return null;
        }

        private void UpdateCellPositionDisplay(Point screenPoint, TransformGroup transform)
        {
            try
            {
                var worldPoint = ScreenToWorld(screenPoint);

                int gridX = (int)Math.Floor(worldPoint.X / GridCellSize);
                int gridY = (int)Math.Floor(worldPoint.Y / GridCellSize);

                if (gridX >= 0 && gridX < GridWidth && gridY >= 0 && gridY < GridHeight)
                {
                    string col = GetColumnLabel(gridX);
                    TxtCurrentCell.Text = $"{col}{gridY + 1}";
                }
                else
                {
                    TxtCurrentCell.Text = "-";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BattleGrid] Error updating cell display: {ex.Message}");
                TxtCurrentCell.Text = "-";
            }
        }

        private Point ScreenToWorld(Point screenPoint)
        {
            double zoom = _renderManager.GetZoomLevel();
            var pan = _renderManager.GetTransform().Children[0].Value; // Pan is first

            return new Point(
                screenPoint.X / zoom - pan.OffsetX,
                screenPoint.Y / zoom - pan.OffsetY
            );
        }

        private void UpdateZoomDisplay()
        {
            double zoom = _renderManager.GetZoomLevel();
            TxtZoomLevel.Text = $"{(int)(zoom * 100)}%";
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

        #region Event Handlers

        private void OnViewportChanged()
        {
            // Don't update during interaction
            if (_draggedToken != null)
            {
                return;
            }

            var now = DateTime.Now;
            if ((now - _lastViewportUpdate).TotalMilliseconds < 100)
            {
                return;
            }
            _lastViewportUpdate = now;

            double currentZoom = _renderManager.GetZoomLevel();

            if (currentZoom >= 0.5)
            {
                _renderManager.RenderGrid(GridWidth, GridHeight, GridCellSize, ShowGrid);
            }

            _renderManager.RenderRulers(GridWidth, GridHeight, GridCellSize, ActualWidth, ActualHeight);
            _renderManager.RenderFog(_fogManager, GridCellSize);
            _tokenManager.UpdateTokenPositions(GridCellSize);
        }

        private void OnGridPositionChanged(Point gridPosition)
        {
            // Grid position updates handled by UpdateCellPositionDisplay
        }

        private static void OnGridPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BattleGridControl control && control.IsLoaded)
            {
                control._renderManager.SetGridDimensions(control.GridWidth, control.GridHeight, control.GridCellSize);
                control._renderManager.RenderGrid(control.GridWidth, control.GridHeight, control.GridCellSize, control.ShowGrid);
                control._renderManager.RenderRulers(control.GridWidth, control.GridHeight, control.GridCellSize, control.ActualWidth, control.ActualHeight);
            }
        }

        private static void OnTokensChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BattleGridControl control)
            {
                var tokens = e.NewValue as System.Collections.ObjectModel.ObservableCollection<Token>;
                control._tokenManager.SetTokens(tokens);

                if (control.IsLoaded)
                {
                    control._tokenManager.RebuildAllTokenVisuals(control.GridCellSize);
                }
            }
        }

        private static void OnSelectedTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle selected token changed if needed
        }

        #endregion

        #region Public Methods

        public void SetGridSize(int width, int height)
        {
            GridWidth = width;
            GridHeight = height;
        }

        public async System.Threading.Tasks.Task LoadTileMapAsync(TileMap tileMap)
        {
            try
            {
                Debug.WriteLine($"[BattleGrid] Loading tile map: {tileMap?.Name ?? "null"}");

                if (tileMap == null)
                {
                    _tileMapManager.ClearTileMap();
                    _renderManager.RenderTileMap(null, GridCellSize);
                    LogMessage?.Invoke("Map", "Tile map cleared");
                    return;
                }

                SetGridSize(tileMap.Width, tileMap.Height);
                _renderManager.ResetView();

                await _tileMapManager.LoadTileMapAsync(tileMap, GridCellSize);
                _renderManager.RenderTileMap(_tileMapManager.LoadedTileMap, GridCellSize);

                OnViewportChanged();

                LogMessage?.Invoke("Map", $"✅ Loaded: {tileMap.Name} ({tileMap.Width}×{tileMap.Height}, {tileMap.PlacedTiles?.Count ?? 0} tiles)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BattleGrid] Error loading tile map: {ex.Message}");
                LogMessage?.Invoke("Error", $"Failed to load tile map: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public Methods - Fog Of War

        public void InitializeFogOfWar()
        {
            _fogManager.Initialize(GridWidth, GridHeight);
            Debug.WriteLine($"[BattleGrid] InitializeFogOfWar: {GridWidth}×{GridHeight}");
        }

        public void SetFogEnabled(bool enabled)
        {
            _fogManager.SetEnabled(enabled);
            Debug.WriteLine($"[BattleGrid] SetFogEnabled: {enabled}");
        }

        public void SetFogBrushMode(DnDBattle.Services.FogBrushMode mode)
        {
            _fogManager.SetBrushMode((FogBrushMode)mode);
            Debug.WriteLine($"[BattleGrid] SetFogBrushMode: {mode}");
        }

        public void SetFogBrushSize(int size)
        {
            _fogManager.SetBrushSize(size);
            Debug.WriteLine($"[BattleGrid] SetFogBrushSize: {size}");
        }

        public void SetPlayerView(bool isPlayerView)
        {
            _fogManager.SetPlayerView(isPlayerView);
            Debug.WriteLine($"[BattleGrid] SetPlayerView: {isPlayerView}");
        }

        public void RevealAroundPlayers()
        {
            _fogManager.RevealAroundTokens(Tokens, visionRange: 6);
            Debug.WriteLine($"[BattleGrid] RevealAroundPlayers");
        }

        public void StartFogShapeTool(DnDBattle.Views.FogShapeTool tool)
        {
            _activeFogShapeTool = tool;
            _fogShapeStartPoint = null;

            // Clear any existing preview
            if (_fogShapePreview != null)
            {
                RenderCanvas.Children.Remove(_fogShapePreview);
                _fogShapePreview = null;
            }
            if (_fogShapeCirclePreview != null)
            {
                RenderCanvas.Children.Remove(_fogShapeCirclePreview);
                _fogShapeCirclePreview = null;
            }

            Cursor = Cursors.Cross;
            Debug.WriteLine($"[BattleGrid] StartFogShapeTool: {tool}");
            LogMessage?.Invoke("Fog", $"Draw {tool} - click and drag on map");
        }

        public void SetFogOfWar(bool enabled, FogMode mode)
        {
            Debug.WriteLine($"[BattleGrid] SetFogOfWar: enabled={enabled}, mode={mode}");

            _fogManager.SetEnabled(enabled);

            if (mode == FogMode.Exploration)
            {
                _fogManager.HideAll();
                LogMessage?.Invoke("Fog", "Exploration mode - map hidden");
            }
            else if (mode == FogMode.Dynamic)
            {
                _fogManager.RevealAroundTokens(Tokens, visionRange: 6);
                LogMessage?.Invoke("Fog", "Dynamic mode - revealing player vision");
            }
        }

        public void SetFogOfWar(byte[,] fogData)
        {
            _fogManager.SetFogData(fogData);
            Debug.WriteLine($"[BattleGrid] SetFogOfWar: loaded data");
        }

        public void RevealAllFog()
        {
            _fogManager.RevealAll();
            Debug.WriteLine($"[BattleGrid] RevealAllFog");
        }

        public void ResetFog()
        {
            _fogManager.HideAll();
            Debug.WriteLine($"[BattleGrid] ResetFog");
        }

        #endregion

        #region Private Methods

        private void DrawFogShapePreview(Point start, Point end)
        {
            // Remove old preview
            if (_fogShapePreview != null)
            {
                RenderCanvas.Children.Remove(_fogShapePreview);
                _fogShapePreview = null;
            }
            if (_fogShapeCirclePreview != null)
            {
                RenderCanvas.Children.Remove(_fogShapeCirclePreview);
                _fogShapeCirclePreview = null;
            }

            var brush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 0));
            var stroke = new SolidColorBrush(Colors.Yellow);

            if (_activeFogShapeTool == DnDBattle.Views.FogShapeTool.Rectangle)
            {
                double left = Math.Min(start.X, end.X) * GridCellSize;
                double top = Math.Min(start.Y, end.Y) * GridCellSize;
                double width = (Math.Abs(end.X - start.X) + 1) * GridCellSize;
                double height = (Math.Abs(end.Y - start.Y) + 1) * GridCellSize;

                _fogShapePreview = new System.Windows.Shapes.Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = brush,
                    Stroke = stroke,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(_fogShapePreview, left);
                Canvas.SetTop(_fogShapePreview, top);
                Canvas.SetZIndex(_fogShapePreview, 1000);

                RenderCanvas.Children.Add(_fogShapePreview);
            }
            else if (_activeFogShapeTool == DnDBattle.Views.FogShapeTool.Circle)
            {
                double dx = end.X - start.X;
                double dy = end.Y - start.Y;
                double radius = Math.Sqrt(dx * dx + dy * dy);

                double centerX = start.X * GridCellSize + GridCellSize / 2;
                double centerY = start.Y * GridCellSize + GridCellSize / 2;
                double radiusPx = radius * GridCellSize;

                _fogShapeCirclePreview = new System.Windows.Shapes.Ellipse
                {
                    Width = radiusPx * 2,
                    Height = radiusPx * 2,
                    Fill = brush,
                    Stroke = stroke,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(_fogShapeCirclePreview, centerX - radiusPx);
                Canvas.SetTop(_fogShapeCirclePreview, centerY - radiusPx);
                Canvas.SetZIndex(_fogShapeCirclePreview, 1000);

                RenderCanvas.Children.Add(_fogShapeCirclePreview);
            }
        }

        #endregion

        #region Compatibility Methods (Matching OLD BattleGridControl Signatures)

        // Events
        public event Action<Token> TokenSelected;
        public event Action<Token> TokenAddedToMap;
        public event Action<Token> TargetSelected;

        // Tile Map
        public void LoadTileMap(TileMap tileMap)
        {
            _ = LoadTileMapAsync(tileMap);
        }

        // Token Visuals
        public void RebuildTokenVisuals()
        {
            _tokenManager.RebuildAllTokenVisuals(GridCellSize);
        }

        // Coordinate Conversion
        public Point ScreenToWorldPublic(Point screenPoint)
        {
            return ScreenToWorld(screenPoint);
        }

        // Panning
        public void PanBy(double dx, double dy)
        {
            _renderManager.ApplyPanWithoutRedraw(dx, dy);
            _renderManager.FinishInteraction();
            OnViewportChanged();
        }

        // Keyboard - Takes Key enum
        public void HandleKeyDown(Key key)
        {
            Debug.WriteLine($"[BattleGrid] HandleKeyDown: {key}");
        }

        // Measure Mode
        private bool _isMeasureMode = false;
        public bool IsMeasureMode => _isMeasureMode;

        public void SetMeasureMode(bool enabled)
        {
            _isMeasureMode = enabled;
            LogMessage?.Invoke("Measure", enabled ? "Measure mode enabled" : "Measure mode disabled");
        }

        // Grid Settings
        public void SetGridMaxSize(int maxWidth, int maxHeight)
        {
            Debug.WriteLine($"[BattleGrid] SetGridMaxSize: {maxWidth}×{maxHeight}");
        }

        public void UpdateShadowSoftness()
        {
            Debug.WriteLine($"[BattleGrid] UpdateShadowSoftness stub");
        }

        // Area Effects - Takes AreaEffectShape enum and int size
        public void StartAreaEffectPlacement(DnDBattle.Models.AreaEffectShape shape, int sizeInFeet, System.Windows.Media.Color color)
        {
            Debug.WriteLine($"[BattleGrid] StartAreaEffectPlacement: {shape}, {sizeInFeet}ft");
        }

        public void UpdateAreaEffectSize(int sizeInFeet)
        {
            Debug.WriteLine($"[BattleGrid] UpdateAreaEffectSize: {sizeInFeet}ft");
        }

        public void UpdateAreaEffectColor(System.Windows.Media.Color color)
        {
            Debug.WriteLine($"[BattleGrid] UpdateAreaEffectColor stub");
        }

        public void CancelAreaEffectPlacement()
        {
            Debug.WriteLine($"[BattleGrid] CancelAreaEffectPlacement stub");
        }

        // Targeting - Takes TargetingState parameter
        public void EnterTargetingMode(TargetingState state)
        {
            Debug.WriteLine($"[BattleGrid] EnterTargetingMode stub");
        }

        public void ExitTargetingMode()
        {
            Debug.WriteLine($"[BattleGrid] ExitTargetingMode stub");
        }

        // Lighting - Takes LightSource object
        public void AddLight(DnDBattle.Models.LightSource light)
        {
            Debug.WriteLine($"[BattleGrid] AddLight: ({light.CenterGrid.X}, {light.CenterGrid.Y})");
        }

        // Walls & Rooms - Takes WallType enum as default parameter
        public void SetWallDrawMode(bool enabled, WallType wallType = WallType.Solid)
        {
            Debug.WriteLine($"[BattleGrid] SetWallDrawMode: {enabled}, {wallType}");
        }

        public void SetRoomDrawMode(bool enabled, WallType wallType = WallType.Solid)
        {
            Debug.WriteLine($"[BattleGrid] SetRoomDrawMode: {enabled}, {wallType}");
        }

        public bool IsRoomDrawMode => false;

        public WallService WallService => null; // Stub

        // Pathfinding
        public async System.Threading.Tasks.Task CommitPreviewedPathAsync()
        {
            Debug.WriteLine($"[BattleGrid] CommitPreviewedPathAsync stub");
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // Turn Management (these don't exist in old code - stubs for MainViewModel)
        public void OnTokenTurnStart(Token token)
        {
            Debug.WriteLine($"[BattleGrid] OnTokenTurnStart stub: {token?.Name}");
        }

        public void OnTokenTurnEnd(Token token)
        {
            Debug.WriteLine($"[BattleGrid] OnTokenTurnEnd stub: {token?.Name}");
        }

        public void OnRoundChanged(int round)
        {
            Debug.WriteLine($"[BattleGrid] OnRoundChanged stub: round {round}");
        }

        // Encounter Serialization
        public DnDBattle.Models.EncounterDto GetEncounterDto()
        {
            Debug.WriteLine($"[BattleGrid] GetEncounterDto stub - returning null");
            return null;
        }

        public void LoadEncounterDto(DnDBattle.Models.EncounterDto dto)
        {
            Debug.WriteLine($"[BattleGrid] LoadEncounterDto stub");
        }

        #endregion
    }
}