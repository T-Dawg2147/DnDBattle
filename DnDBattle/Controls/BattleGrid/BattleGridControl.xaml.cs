using DnDBattle.Controls.BattleGrid.Managers;
using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.FogOfWar;
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
        }

        #endregion

        #region Mouse Handling

        private void RenderCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();

            var mousePos = e.GetPosition(RenderCanvas);

            if (e.ChangedButton == MouseButton.Left)
            {
                // Check if we clicked on a token
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
                    // Start panning
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

            // Handle token dragging
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

            // Handle panning
            if (_isPanning)
            {
                var delta = mousePos - _panStartPoint;

                // Apply pan without any clamping or rendering
                _renderManager.ApplyPanWithoutRedraw(delta.X, delta.Y);

                _panStartPoint = mousePos;
                e.Handled = true;
                return;
            }

            // Update cursor
            if (!_isPanning && _draggedToken == null)
            {
                var element = FindVisualAtPoint(mousePos);
                Cursor = (element?.Tag is Token) ? Cursors.Hand : Cursors.Arrow;
            }
        }

        private void RenderCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(RenderCanvas);

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

        // Fog of War
        public void InitializeFogOfWar()
        {
            Debug.WriteLine($"[BattleGrid] InitializeFogOfWar stub");
        }

        public void SetFogEnabled(bool enabled)
        {
            Debug.WriteLine($"[BattleGrid] SetFogEnabled: {enabled}");
        }

        public void SetFogBrushMode(DnDBattle.Services.FogBrushMode mode)
        {
            Debug.WriteLine($"[BattleGrid] SetFogBrushMode: {mode}");
        }

        public void SetFogBrushSize(int size)
        {
            Debug.WriteLine($"[BattleGrid] SetFogBrushSize: {size}");
        }

        public void SetPlayerView(bool isPlayerView)
        {
            Debug.WriteLine($"[BattleGrid] SetPlayerView: {isPlayerView}");
        }

        public void RevealAroundPlayers()
        {
            Debug.WriteLine($"[BattleGrid] RevealAroundPlayers stub");
        }

        public void StartFogShapeTool(DnDBattle.Views.FogShapeTool tool)
        {
            Debug.WriteLine($"[BattleGrid] StartFogShapeTool: {tool}");
        }

        public void SetFogOfWar(byte[,] fogData)
        {
            Debug.WriteLine($"[BattleGrid] SetFogOfWar stub");
        }

        public void SetFogOfWar(bool isEnabled, FogMode mode)
        {
            Debug.WriteLine($"[BattleGrid] SetFogOfWar stub");
        }

        public void RevealAllFog()
        {
            Debug.WriteLine($"[BattleGrid] RevealAllFog stub");
        }

        public void ResetFog()
        {
            Debug.WriteLine($"[BattleGrid] ResetFog stub");
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
        public void SetWallDrawMode(bool enabled, DnDBattle.Models.WallType wallType = DnDBattle.Models.WallType.Solid)
        {
            Debug.WriteLine($"[BattleGrid] SetWallDrawMode: {enabled}, {wallType}");
        }

        public void SetRoomDrawMode(bool enabled, DnDBattle.Models.WallType wallType = DnDBattle.Models.WallType.Solid)
        {
            Debug.WriteLine($"[BattleGrid] SetRoomDrawMode: {enabled}, {wallType}");
        }

        public bool IsRoomDrawMode => false;

        public DnDBattle.Services.WallService WallService => null; // Stub

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