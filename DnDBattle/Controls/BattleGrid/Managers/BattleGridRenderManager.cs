using DnDBattle.Controls;
using DnDBattle.Models.Tiles;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    /// <summary>
    /// OPTION A: Maximum Performance Render Manager
    /// - No grid rendering during pan/zoom
    /// - Aggressive throttling
    /// - Deferred rendering
    /// </summary>
    public class BattleGridRenderManager
    {
        #region Fields

        private readonly Canvas _renderCanvas;
        private readonly Canvas _rulerCanvas;

        private readonly GridVisualHost _gridHost;
        private readonly GridVisualHost _tileMapHost;

        private readonly TransformGroup _transformGroup = new TransformGroup();
        private readonly ScaleTransform _zoomTransform = new ScaleTransform(1, 1);
        private readonly TranslateTransform _panTransform = new TranslateTransform(0, 0);

        private int _gridWidth = 100;
        private int _gridHeight = 100;
        private double _cellSize = 48;
        private double _viewWidth;
        private double _viewHeight;

        // Performance optimization: Track if we're actively interacting
        private bool _isInteracting = false;
        private System.Windows.Threading.DispatcherTimer _deferredRenderTimer;

        // Throttling
        private DateTime _lastRulerRender = DateTime.MinValue;
        private const int RULER_THROTTLE_MS = 100; // Rulers update every 100ms max

        #endregion

        #region Events

        public event Action ViewportChanged;

        #endregion

        #region Constructor

        public BattleGridRenderManager(Canvas renderCanvas, Canvas rulerCanvas)
        {
            _renderCanvas = renderCanvas ?? throw new ArgumentNullException(nameof(renderCanvas));
            _rulerCanvas = rulerCanvas ?? throw new ArgumentNullException(nameof(rulerCanvas));

            _gridHost = new GridVisualHost();
            _tileMapHost = new GridVisualHost();

            _transformGroup.Children.Add(_zoomTransform);
            _transformGroup.Children.Add(_panTransform);
            _renderCanvas.RenderTransform = _transformGroup;

            _renderCanvas.Children.Add(_tileMapHost);
            Canvas.SetZIndex(_tileMapHost, 0);

            _renderCanvas.Children.Add(_gridHost);
            Canvas.SetZIndex(_gridHost, 10);

            // Setup deferred render timer (fires 150ms after last interaction)
            _deferredRenderTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _deferredRenderTimer.Tick += (s, e) =>
            {
                _deferredRenderTimer.Stop();
                _isInteracting = false;
                RenderFullViewport(); // Full quality render after interaction stops
            };

            Debug.WriteLine("[RenderManager] Initialized - MAXIMUM PERFORMANCE MODE");
        }

        #endregion

        #region Public Methods - Transform

        public TransformGroup GetTransform() => _transformGroup;
        public double GetZoomLevel() => _zoomTransform.ScaleX;

        public void SetViewportSize(double width, double height)
        {
            _viewWidth = width;
            _viewHeight = height;
        }

        public void SetGridDimensions(int width, int height, double cellSize)
        {
            _gridWidth = width;
            _gridHeight = height;
            _cellSize = cellSize;
        }

        /// <summary>
        /// OPTIMIZED: Pan without any rendering (instant response)
        /// </summary>
        public void ApplyPanWithoutRedraw(double deltaX, double deltaY)
        {
            _panTransform.X += deltaX;
            _panTransform.Y += deltaY;

            // Mark as interacting
            _isInteracting = true;

            // Restart deferred render timer
            _deferredRenderTimer.Stop();
            _deferredRenderTimer.Start();
        }

        /// <summary>
        /// OPTIMIZED: Zoom without grid rendering
        /// </summary>
        public void ApplyZoom(double factor, Point center)
        {
            double absX = center.X * _zoomTransform.ScaleX + _panTransform.X;
            double absY = center.Y * _zoomTransform.ScaleY + _panTransform.Y;

            _zoomTransform.ScaleX *= factor;
            _zoomTransform.ScaleY *= factor;

            _zoomTransform.ScaleX = Math.Max(0.1, Math.Min(5.0, _zoomTransform.ScaleX));
            _zoomTransform.ScaleY = Math.Max(0.1, Math.Min(5.0, _zoomTransform.ScaleY));

            _panTransform.X = absX - center.X * _zoomTransform.ScaleX;
            _panTransform.Y = absY - center.Y * _zoomTransform.ScaleY;

            ClampPan();

            // Mark as interacting
            _isInteracting = true;

            // Restart deferred render timer
            _deferredRenderTimer.Stop();
            _deferredRenderTimer.Start();
        }

        /// <summary>
        /// Call this when interaction finishes (mouse up)
        /// </summary>
        public void FinishInteraction()
        {
            ClampPan();

            // Cancel deferred timer and render immediately
            _deferredRenderTimer.Stop();
            _isInteracting = false;

            RenderFullViewport();
        }

        public void ResetView()
        {
            _zoomTransform.ScaleX = 1.0;
            _zoomTransform.ScaleY = 1.0;
            _panTransform.X = 0;
            _panTransform.Y = 0;

            RenderFullViewport();
            Debug.WriteLine("[RenderManager] View reset");
        }

        #endregion

        #region Public Methods - Rendering

        /// <summary>
        /// OPTIMIZED: Full viewport render (called after interaction stops)
        /// </summary>
        private void RenderFullViewport()
        {
            var sw = Stopwatch.StartNew();

            double currentZoom = _zoomTransform.ScaleX;
            var viewport = GetViewportRect();

            // Render grid (with adaptive detail)
            RenderGridOptimized(viewport, currentZoom);

            // Render rulers
            RenderRulersOptimized(currentZoom);

            sw.Stop();
            Debug.WriteLine($"[RenderManager] Full render: {sw.ElapsedMilliseconds}ms at {currentZoom:P0} zoom");

            ViewportChanged?.Invoke();
        }

        /// <summary>
        /// OPTIMIZED: Adaptive grid rendering based on zoom
        /// </summary>
        private void RenderGridOptimized(Rect viewport, double zoom)
        {
            // Don't render grid when zoomed way out
            if (zoom < 0.3)
            {
                _gridHost.Clear();
                return;
            }

            // Calculate cell count
            int cellCountX = (int)Math.Ceiling(viewport.Width / _cellSize);
            int cellCountY = (int)Math.Ceiling(viewport.Height / _cellSize);
            int totalCells = cellCountX * cellCountY;

            // Adaptive rendering based on cell count
            bool showGrid = true;
            bool showCoords = true;

            if (totalCells > 10000) // Way too many
            {
                _gridHost.Clear();
                return;
            }
            else if (totalCells > 5000) // A lot
            {
                showCoords = false; // Skip coordinates
            }

            _gridHost.DrawGridViewport(_cellSize, viewport, 50, showCoords, showGrid);
        }

        /// <summary>
        /// OPTIMIZED: Ruler rendering with throttling
        /// </summary>
        private void RenderRulersOptimized(double zoom)
        {
            var now = DateTime.Now;
            if ((now - _lastRulerRender).TotalMilliseconds < RULER_THROTTLE_MS)
                return;
            _lastRulerRender = now;

            _rulerCanvas.Children.Clear();

            // Don't show rulers when zoomed way out
            if (zoom < 0.5)
                return;

            const double rulerHeight = 30;
            const double rulerWidth = 35;

            // Backgrounds
            var bgBrush = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            bgBrush.Freeze();

            var topBg = new System.Windows.Shapes.Rectangle
            {
                Width = _viewWidth,
                Height = rulerHeight,
                Fill = bgBrush
            };
            Canvas.SetLeft(topBg, 0);
            Canvas.SetTop(topBg, 0);
            _rulerCanvas.Children.Add(topBg);

            var leftBg = new System.Windows.Shapes.Rectangle
            {
                Width = rulerWidth,
                Height = _viewHeight,
                Fill = bgBrush
            };
            Canvas.SetLeft(leftBg, 0);
            Canvas.SetTop(leftBg, 0);
            _rulerCanvas.Children.Add(leftBg);

            // Calculate visible range
            var viewport = GetViewportRect();
            int startCol = Math.Max(0, (int)Math.Floor(viewport.Left / _cellSize));
            int endCol = Math.Min(_gridWidth, (int)Math.Ceiling(viewport.Right / _cellSize));
            int startRow = Math.Max(0, (int)Math.Floor(viewport.Top / _cellSize));
            int endRow = Math.Min(_gridHeight, (int)Math.Ceiling(viewport.Bottom / _cellSize));

            // Adaptive label density
            int skipFactor = 1;
            if (endCol - startCol > 30) skipFactor = 2;
            if (endCol - startCol > 60) skipFactor = 5;

            var textBrush = Brushes.LightGray;
            var typeface = new System.Windows.Media.Typeface("Segoe UI");

            // Column labels
            for (int col = startCol; col <= endCol; col += skipFactor)
            {
                var worldX = col * _cellSize;
                var screenX = WorldToScreen(new Point(worldX, 0)).X;

                if (screenX < rulerWidth || screenX > _viewWidth) continue;

                var label = new TextBlock
                {
                    Text = GetColumnLabel(col),
                    FontSize = 11,
                    Foreground = textBrush,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                };

                Canvas.SetLeft(label, screenX + 5);
                Canvas.SetTop(label, 7);
                _rulerCanvas.Children.Add(label);
            }

            // Row labels
            for (int row = startRow; row <= endRow; row += skipFactor)
            {
                var worldY = row * _cellSize;
                var screenY = WorldToScreen(new Point(0, worldY)).Y;

                if (screenY < rulerHeight || screenY > _viewHeight) continue;

                var label = new TextBlock
                {
                    Text = (row + 1).ToString(),
                    FontSize = 11,
                    Foreground = textBrush,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                };

                Canvas.SetLeft(label, 10);
                Canvas.SetTop(label, screenY + 5);
                _rulerCanvas.Children.Add(label);
            }
        }

        /// <summary>
        /// Render grid (called externally, but optimized internally)
        /// </summary>
        public void RenderGrid(int width, int height, double cellSize, bool showGrid)
        {
            if (!showGrid)
            {
                _gridHost.Clear();
                return;
            }

            // If currently interacting, skip render
            if (_isInteracting)
                return;

            var viewport = GetViewportRect();
            RenderGridOptimized(viewport, _zoomTransform.ScaleX);
        }

        /// <summary>
        /// Render rulers (called externally)
        /// </summary>
        public void RenderRulers(int gridWidth, int gridHeight, double cellSize, double viewWidth, double viewHeight)
        {
            // If currently interacting, skip render
            if (_isInteracting)
                return;

            RenderRulersOptimized(_zoomTransform.ScaleX);
        }

        /// <summary>
        /// Render tile map
        /// </summary>
        public void RenderTileMap(TileMap tileMap, double cellSize)
        {
            var sw = Stopwatch.StartNew();

            if (tileMap == null)
            {
                _tileMapHost.Clear();
                return;
            }

            Debug.WriteLine($"[RenderManager] Rendering {tileMap.PlacedTiles?.Count ?? 0} tiles");

            _tileMapHost.DrawTileMap(tileMap, cellSize);

            sw.Stop();
            Debug.WriteLine($"[RenderManager] Tile map render: {sw.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Private Methods

        private Rect GetViewportRect()
        {
            var topLeft = ScreenToWorld(new Point(0, 0));
            var bottomRight = ScreenToWorld(new Point(_viewWidth, _viewHeight));
            return new Rect(topLeft, bottomRight);
        }

        private void ClampPan()
        {
            if (_viewWidth <= 0 || _viewHeight <= 0) return;

            double gridPixelWidth = _gridWidth * _cellSize;
            double gridPixelHeight = _gridHeight * _cellSize;
            double zoom = _zoomTransform.ScaleX;

            double minPanX = _viewWidth - (gridPixelWidth * zoom);
            double minPanY = _viewHeight - (gridPixelHeight * zoom);
            double maxPanX = 0;
            double maxPanY = 0;

            double paddingX = _viewWidth * 0.15;
            double paddingY = _viewHeight * 0.15;

            _panTransform.X = Math.Max(minPanX - paddingX, Math.Min(maxPanX + paddingX, _panTransform.X));
            _panTransform.Y = Math.Max(minPanY - paddingY, Math.Min(maxPanY + paddingY, _panTransform.Y));
        }

        private Point WorldToScreen(Point worldPoint)
        {
            return _transformGroup.Transform(worldPoint);
        }

        private Point ScreenToWorld(Point screenPoint)
        {
            return _transformGroup.Inverse?.Transform(screenPoint) ?? screenPoint;
        }

        private string GetColumnLabel(int index)
        {
            string result = "";
            while (index >= 0)
            {
                result = (char)('A' + (index % 26)) + result;
                index = index / 26 - 1;
            }
            return result;
        }

        #endregion
    }
}