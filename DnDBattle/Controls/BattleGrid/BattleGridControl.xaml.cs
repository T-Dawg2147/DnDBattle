using DnDBattle.Controls.BattleGrid.Managers;
using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Controls.BattleGrid
{
    /// <summary>
    /// BattleGridControl v2.0 - Clean, organized, maintainable
    /// Core coordinator that manages specialized manager classes
    /// </summary>
    public partial class BattleGridControl : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty GridCellSizeProperty =
            DependencyProperty.Register(nameof(GridCellSize), typeof(double), typeof(BattleGridControl),
                new PropertyMetadata(48.0, OnGridCellSizeChanged));

        public static readonly DependencyProperty TokensProperty =
            DependencyProperty.Register(nameof(Tokens), typeof(ObservableCollection<Token>), typeof(BattleGridControl),
                new PropertyMetadata(null, OnTokensChanged));

        public static readonly DependencyProperty SelectedTokenProperty =
            DependencyProperty.Register(nameof(SelectedToken), typeof(Token), typeof(BattleGridControl),
                new PropertyMetadata(null, OnSelectedTokenChanged));

        public static readonly DependencyProperty LockToGridProperty =
            DependencyProperty.Register(nameof(LockToGrid), typeof(bool), typeof(BattleGridControl),
                new PropertyMetadata(true));

        public double GridCellSize
        {
            get => (double)GetValue(GridCellSizeProperty);
            set => SetValue(GridCellSizeProperty, value);
        }

        public ObservableCollection<Token> Tokens
        {
            get => (ObservableCollection<Token>)GetValue(TokensProperty);
            set => SetValue(TokensProperty, value);
        }

        public Token SelectedToken
        {
            get => (Token)GetValue(SelectedTokenProperty);
            set => SetValue(SelectedTokenProperty, value);
        }

        public bool LockToGrid
        {
            get => (bool)GetValue(LockToGridProperty);
            set => SetValue(LockToGridProperty, value);
        }

        #endregion

        #region Fields

        private DateTime _lastViewportUpdate = DateTime.MinValue;

        #endregion

        #region Events

        public event Action<Token> TokenDoubleClicked;
        public event Action<Token> TokenSelected;
        public event Action<string, string> LogMessage;

        #endregion

        #region Managers

        private readonly BattleGridRenderManager _renderManager;
        private readonly BattleGridTokenManager _tokenManager;
        private readonly BattleGridInputManager _inputManager;
        private readonly BattleGridTileMapManager _tileMapManager;

        #endregion

        #region Properties

        public bool ShowGrid { get; set; } = false;
        public int GridWidth { get; private set; } = 100;
        public int GridHeight { get; private set; } = 100;
        public Point CurrentMouseGridPosition { get; private set; }

        #endregion

        #region Constructor

        public BattleGridControl()
        {
            InitializeComponent();

            System.Diagnostics.Debug.WriteLine("[BattleGrid] Initializing BattleGridControl v2.0...");

            // Initialize managers
            _renderManager = new BattleGridRenderManager(RenderCanvas, RulerCanvas);
            _tokenManager = new BattleGridTokenManager(RenderCanvas);
            _inputManager = new BattleGridInputManager();
            _tileMapManager = new BattleGridTileMapManager();

            // Wire up manager events
            SetupManagerEvents();

            // Set default grid size
            SetGridSize(100, 100);

            // Handle loaded event
            Loaded += BattleGridControl_Loaded;

            ShowGrid = true;
            LockToGrid = true;

            System.Diagnostics.Debug.WriteLine("[BattleGrid] Initialization complete!");
        }

        #endregion

        #region Initialization

        private void BattleGridControl_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[BattleGrid] Control loaded, rendering initial state...");

            // Set viewport size
            _renderManager.SetViewportSize(ActualWidth, ActualHeight);
            _renderManager.SetGridDimensions(GridWidth, GridHeight, GridCellSize);

            // RESET VIEW TO (0, 0) - START AT A1!
            _renderManager.ResetView();
            System.Diagnostics.Debug.WriteLine("[BattleGrid] View reset to A1");

            // Initial render - FORCE grid to show
            _renderManager.RenderGrid(GridWidth, GridHeight, GridCellSize, true);
            _renderManager.RenderRulers(GridWidth, GridHeight, GridCellSize, ActualWidth, ActualHeight);

            // Wire up token manager with tokens collection
            if (Tokens != null)
            {
                _tokenManager.SetTokens(Tokens);
                _tokenManager.RebuildAllTokenVisuals(GridCellSize);
            }

            Focusable = true;
            Focus();
        }

        private void SetupManagerEvents()
        {
            // Render manager events
            _renderManager.ViewportChanged += OnViewportChanged;

            // Token manager events
            _tokenManager.TokenClicked += OnTokenClicked;
            _tokenManager.TokenDoubleClicked += (token) => TokenDoubleClicked?.Invoke(token);
            _tokenManager.TokenMoved += OnTokenMoved;
            _tokenManager.StopPanning += () => _inputManager.StopPanning();
            _tokenManager.LogMessage += (category, message) => LogMessage?.Invoke(category, message);

            // Input manager events
            _inputManager.PanChanged += OnPanChanged;
            _inputManager.ZoomChanged += OnZoomChanged;
            _inputManager.GridPositionChanged += OnGridPositionChanged;
            _inputManager.ResetViewRequested += OnResetViewRequested;
            _inputManager.ZoomAtCenterRequested += OnZoomAtCenterRequested;

            // Tile map manager events
            _tileMapManager.TileMapLoaded += OnTileMapLoaded;
            _tileMapManager.LogMessage += (category, message) => LogMessage?.Invoke(category, message);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the grid dimensions
        /// </summary>
        public void SetGridSize(int width, int height)
        {
            GridWidth = Math.Max(10, width);
            GridHeight = Math.Max(10, height);

            System.Diagnostics.Debug.WriteLine($"[BattleGrid] Grid size set to {GridWidth}×{GridHeight}");

            // Tell render manager about grid dimensions
            _renderManager.SetGridDimensions(GridWidth, GridHeight, GridCellSize);

            if (IsLoaded)
            {
                _renderManager.RenderGrid(GridWidth, GridHeight, GridCellSize, LockToGrid);
            }
        }


        /// <summary>
        /// Loads a tile map into the battle grid
        /// </summary>
        public async void LoadTileMap(TileMap tileMap)
        {
            await LoadTileMapAsync(tileMap);
        }

        /// <summary>
        /// Loads a tile map into the battle grid asyncornously
        /// </summary>
        public async Task LoadTileMapAsync(TileMap tileMap)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] ===== LoadTileMapAsync START =====");
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Tile map: {tileMap?.Name ?? "NULL"}");

                if (tileMap == null)
                {
                    _tileMapManager.ClearTileMap();
                    _renderManager.RenderTileMap(null, GridCellSize);
                    LogMessage?.Invoke("Map", "Tile map cleared");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Map size: {tileMap.Width}×{tileMap.Height}");
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Tiles: {tileMap.PlacedTiles?.Count ?? 0}");

                // Set grid to match tile map size
                SetGridSize(tileMap.Width, tileMap.Height);

                // RESET VIEW TO ORIGIN
                _renderManager.ResetView();
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] View reset to origin");

                // Load tile map (async in background)
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Calling TileMapManager.LoadTileMapAsync...");
                await _tileMapManager.LoadTileMapAsync(tileMap, GridCellSize);

                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Loaded tile map successfully");

                // Render tile map on UI thread
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Calling RenderManager.RenderTileMap...");
                _renderManager.RenderTileMap(_tileMapManager.LoadedTileMap, GridCellSize);

                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Render complete");

                // Force viewport update
                OnViewportChanged();

                LogMessage?.Invoke("Map", $"✅ Loaded: {tileMap.Name} ({tileMap.Width}×{tileMap.Height}, {tileMap.PlacedTiles?.Count ?? 0} tiles)");

                System.Diagnostics.Debug.WriteLine($"[BattleGrid] ===== LoadTileMapAsync END =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] ❌ ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Stack: {ex.StackTrace}");
                LogMessage?.Invoke("Error", $"Failed to load tile map: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Rebuilds all token visuals
        /// </summary>
        public void RefreshTokens()
        {
            _tokenManager.RebuildAllTokenVisuals(GridCellSize);
        }

        #endregion

        #region Dependency Property Callbacks

        private static void OnGridCellSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BattleGridControl control && control.IsLoaded)
            {
                control._renderManager.RenderGrid(control.GridWidth, control.GridHeight, control.GridCellSize, control.LockToGrid);
                control._tokenManager.RebuildAllTokenVisuals(control.GridCellSize);
            }
        }

        private static void OnTokensChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BattleGridControl control)
            {
                var tokens = e.NewValue as ObservableCollection<Token>;
                control._tokenManager.SetTokens(tokens);

                if (control.IsLoaded)
                {
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Tokens changed, rebuilding visuals for {tokens?.Count ?? 0} tokens");
                    control._tokenManager.RebuildAllTokenVisuals(control.GridCellSize);
                }
            }
        }

        private static void OnSelectedTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BattleGridControl control && e.NewValue is Token token)
            {
                control._tokenManager.SelectToken(token);
            }
        }

        #endregion

        #region Event Handlers - Input

        private void RenderCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();

            var position = e.GetPosition(RenderCanvas);
            var transform = _renderManager.GetTransform();

            // LEFT MOUSE BUTTON
            if (e.ChangedButton == MouseButton.Left)
            {
                // FIRST: Check if we clicked on a token
                var worldPos = transform.Inverse?.Transform(position) ?? position;
                var clickedToken = FindTokenAtPosition(worldPos);

                if (clickedToken != null)
                {
                    // Token clicked - let TokenManager handle it
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Token clicked: {clickedToken.Name}");
                    _tokenManager.HandleMouseDown(position, transform, GridCellSize);
                    e.Handled = true;
                    return; // DON'T start panning!
                }
                else
                {
                    // Empty space clicked - start panning
                    System.Diagnostics.Debug.WriteLine($"[BattleGrid] Starting pan at {position}");
                    _inputManager.HandleMouseDown(e, position, transform, GridCellSize);
                    RenderCanvas.CaptureMouse(); // ← CAPTURE MOUSE!
                }
            }
            // MIDDLE MOUSE BUTTON
            else if (e.ChangedButton == MouseButton.Middle)
            {
                System.Diagnostics.Debug.WriteLine($"[BattleGrid] Starting middle-mouse pan");
                _inputManager.HandleMouseDown(e, position, transform, GridCellSize);
                RenderCanvas.CaptureMouse(); // ← CAPTURE MOUSE!
            }
        }

        private void RenderCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(RenderCanvas);
            var transform = _renderManager.GetTransform();

            // FIRST: Check if TokenManager is dragging
            if (_tokenManager.IsDragging)
            {
                _tokenManager.HandleMouseMove(position, transform, GridCellSize, LockToGrid);
                e.Handled = true;
                return;
            }

            // SECOND: ONLY handle panning if mouse is captured or button is pressed
            if (RenderCanvas.IsMouseCaptured ||
                e.LeftButton == MouseButtonState.Pressed ||
                e.MiddleButton == MouseButtonState.Pressed)
            {
                _inputManager.HandleMouseMove(e, position, transform, GridCellSize, ActualWidth, ActualHeight);
            }
            else
            {
                // Just update grid position display (no panning)
                _inputManager.UpdateGridPosition(position, transform, GridCellSize);
            }
        }

        private void RenderCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(RenderCanvas);
            var transform = _renderManager.GetTransform();

            System.Diagnostics.Debug.WriteLine($"[BattleGrid] Mouse up: {e.ChangedButton}");

            // Release mouse capture
            RenderCanvas.ReleaseMouseCapture();

            // FIRST: Check if TokenManager is finishing a drag
            if (_tokenManager.IsDragging)
            {
                _tokenManager.HandleMouseUp(position, transform, GridCellSize, LockToGrid);
                e.Handled = true;
                return;
            }

            // SECOND: Let input manager finish panning
            _inputManager.HandleMouseUp(e, position);
        }

        private void RenderCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var position = e.GetPosition(RenderCanvas);
            _inputManager.HandleMouseWheel(e, position, ActualWidth, ActualHeight);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _inputManager.HandleKeyDown(e, GridCellSize);
        }

        #endregion

        #region Event Handlers - Managers

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (sizeInfo.WidthChanged || sizeInfo.HeightChanged)
            {
                _renderManager.SetViewportSize(ActualWidth, ActualHeight);
                OnViewportChanged();
            }
        }

        private void OnResetViewRequested()
        {
            _renderManager.ResetView();
        }

        private void OnZoomAtCenterRequested(double factor)
        {
            var center = new Point(ActualWidth / 2, ActualWidth / 2);
            _renderManager.ApplyZoom(factor, center);
        }

        private void OnViewportChanged()
        {
            var now = DateTime.Now;
            if ((now - _lastViewportUpdate).TotalMilliseconds < 50)
                return;

            _lastViewportUpdate = now;

            _renderManager.RenderGrid(GridWidth, GridHeight, GridCellSize, ShowGrid);
            _renderManager.RenderRulers(GridWidth, GridHeight, GridCellSize, ActualWidth, ActualHeight);
            _tokenManager.UpdateTokenPositions(GridCellSize);
        }

        private void OnPanChanged(double deltaX, double deltaY)
        {
            _renderManager.ApplyPan(deltaX, deltaY);
            OnViewportChanged();
        }

        private void OnZoomChanged(double zoomFactor, Point zoomCenter)
        {
            _renderManager.ApplyZoom(zoomFactor, zoomCenter);
            OnViewportChanged();
            UpdateZoomDisplay();
        }

        private void OnGridPositionChanged(Point gridPosition)
        {
            CurrentMouseGridPosition = gridPosition;
            UpdateCurrentCellDisplay(gridPosition);
        }

        private void OnTokenClicked(Token token)
        {
            SelectedToken = token;
            TokenSelected?.Invoke(token);
        }

        private void OnTokenMoved(Token token, int oldX, int oldY, int newX, int newY)
        {
            // TODO: Add undo/redo support
            // TODO: Check for tile metadata interactions
            System.Diagnostics.Debug.WriteLine($"[BattleGrid] Token {token.Name} moved from ({oldX},{oldY}) to ({newX},{newY})");
        }

        private void OnTileMapLoaded(TileMap tileMap)
        {
            _renderManager.RenderTileMap(tileMap, GridCellSize);
        }

        #endregion

        #region UI Updates

        private void UpdateCurrentCellDisplay(Point gridPosition)
        {
            int x = (int)Math.Floor(gridPosition.X);
            int y = (int)Math.Floor(gridPosition.Y);

            if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
            {
                string column = GetColumnLabel(x);
                TxtCurrentCell.Text = $"{column}{y + 1}";
            }
            else
            {
                TxtCurrentCell.Text = "-";
            }
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

        #region Helpers

        private Token FindTokenAtPosition(Point worldPosition)
        {
            if (Tokens == null) return null;

            foreach (var token in Tokens.Reverse())
            {
                double tokenLeft = token.GridX * GridCellSize;
                double tokenTop = token.GridY * GridCellSize;
                double tokenSize = GridCellSize * token.SizeInSquares;

                var tokenRect = new Rect(tokenLeft, tokenTop, tokenSize, tokenSize);

                if (tokenRect.Contains(worldPosition))
                    return token;
            }
            return null;
        }

        #endregion

        #region Compatibility Methods (Temporary - TODO: Implement properly)

        /// <summary>
        /// Refreshes token visuals - compatibility method
        /// </summary>
        public void RebuildTokenVisuals()
        {
            RefreshTokens();
        }

        /// <summary>
        /// Sets grid size - compatibility method
        /// </summary>
        public void SetGridMaxSize(int width, int height)
        {
            SetGridSize(width, height);
        }

        /// <summary>
        /// Update shadow softness (not yet implemented)
        /// </summary>
        public void UpdateShadowSoftness()
        {
            // TODO: Implement shadow effects
            System.Diagnostics.Debug.WriteLine("[BattleGrid] UpdateShadowSoftness - not yet implemented");
        }

        /// <summary>
        /// Pan by specific amount (compatibility method)
        /// </summary>
        public void PanBy(double deltaX, double deltaY)
        {
            _renderManager.ApplyPan(deltaX, deltaY);
        }

        /// <summary>
        /// Converts screen coordinates to world coordinates
        /// </summary>
        public Point ScreenToWorldPublic(Point screenPoint)
        {
            var transform = _renderManager.GetTransform();
            return transform.Inverse?.Transform(screenPoint) ?? screenPoint;
        }

        /// <summary>
        /// Handle key down from MainWindow
        /// </summary>
        public void HandleKeyDown(Key key)
        {
            var e = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, key)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            };
            _inputManager.HandleKeyDown(e, GridCellSize);
        }

        public void OnTokenTurnStart(Token token)
        {

        }

        public void OnTokenTurnEnd(Token token)
        {

        }

        public void OnRoundChanged(int currentRound)
        {

        }

        #endregion

        #region Not Yet Implemented Features (Stubs)

        // These features will be added back later in separate managers

        public event Action<Token> TokenAddedToMap;

        public void StartAreaEffectPlacement(object shape, int size, Color color)
        {
            MessageBox.Show("Area effects coming soon in Phase 2!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void UpdateAreaEffectSize(int size)
        {
            // TODO: Implement in AreaEffectManager
        }

        public void UpdateAreaEffectColor(Color color)
        {
            // TODO: Implement in AreaEffectManager
        }

        public void CancelAreaEffectPlacement()
        {
            // TODO: Implement in AreaEffectManager
        }

        public object AreaEffectService => null; // TODO: Implement

        public void EnterTargetingMode(object state)
        {
            MessageBox.Show("Targeting mode coming soon in Phase 3!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExitTargetingMode()
        {
            // TODO: Implement in CombatManager
        }

        public event Action<Token> TargetSelected;

        public void InitializeFogOfWar()
        {
            MessageBox.Show("Fog of War coming soon in Phase 2!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetFogEnabled(bool enabled)
        {
            // TODO: Implement in FogManager
        }

        public void SetFogBrushMode(object mode)
        {
            // TODO: Implement in FogManager
        }

        public void SetFogBrushSize(int size)
        {
            // TODO: Implement in FogManager
        }

        public void SetPlayerView(bool isPlayerView)
        {
            // TODO: Implement in FogManager
        }

        public void RevealAroundPlayers()
        {
            // TODO: Implement in FogManager
        }

        public object FogService => null; // TODO: Implement

        public void StartFogShapeTool(object tool)
        {
            // TODO: Implement in FogManager
        }

        public async Task CommitPreviewedPathAsync()
        {
            MessageBox.Show("Pathfinding coming soon in Phase 3!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
            await Task.CompletedTask;
        }

        public object GetEncounterDto()
        {
            MessageBox.Show("Encounter save/load coming soon in Phase 4!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        public void LoadEncounterDto(object dto)
        {
            MessageBox.Show("Encounter save/load coming soon in Phase 4!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool IsMeasureMode => false; // TODO: Implement

        public void SetMeasureMode(bool enabled)
        {
            MessageBox.Show("Measurement tool coming soon in Phase 2!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetWallDrawMode(bool enabled, object wallType = null)
        {
            MessageBox.Show("Wall drawing coming soon in Phase 3!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetRoomDrawMode(bool enabled, object wallType = null)
        {
            MessageBox.Show("Room drawing coming soon in Phase 3!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public object WallService => null; // TODO: Implement

        public void AddLight(object light)
        {
            MessageBox.Show("Lighting coming soon in Phase 3!", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetFogOfWar(bool enabled, object mode = null)
        {
            // TODO: Implement in FogManager
        }

        public void RevealAllFog()
        {
            // TODO: Implement in FogManager
        }

        public void ResetFog()
        {
            // TODO: Implement in FogManager
        }

        #endregion
    }
}