using DnDBattle.Controls; // For GridVisualHost
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    /// <summary>
    /// Manages all rendering using GridVisualHost for proper visual integration
    /// </summary>
    public class BattleGridRenderManager
    {
        #region Fields

        private readonly Canvas _renderCanvas;
        private readonly Canvas _rulerCanvas;

        // Visual hosts (properly integrated into visual tree)
        private readonly GridVisualHost _gridHost;
        private readonly GridVisualHost _tileMapHost;

        // Transform for pan/zoom
        private readonly TransformGroup _transformGroup = new TransformGroup();
        private readonly ScaleTransform _zoomTransform = new ScaleTransform(1, 1);
        private readonly TranslateTransform _panTransform = new TranslateTransform(0, 0);

        // Grid dimensions
        private int _gridWidth = 100;
        private int _gridHeight = 100;
        private double _cellSize = 48;
        private double _viewWidth;
        private double _viewHeight;

        // Throttling
        private DateTime _lastGridRender = DateTime.MinValue;
        private DateTime _lastRulerRender = DateTime.MinValue;
        private const int RENDER_THROTTLE_MS = 16; // ~60 FPS

        #endregion

        #region Events

        public event Action ViewportChanged;

        #endregion

        #region Constructor

        public BattleGridRenderManager(Canvas renderCanvas, Canvas rulerCanvas)
        {
            _renderCanvas = renderCanvas ?? throw new ArgumentNullException(nameof(renderCanvas));
            _rulerCanvas = rulerCanvas ?? throw new ArgumentNullException(nameof(rulerCanvas));

            // Create visual hosts
            _gridHost = new GridVisualHost();
            _tileMapHost = new GridVisualHost();

            // Setup transform on render canvas
            _transformGroup.Children.Add(_zoomTransform);
            _transformGroup.Children.Add(_panTransform);
            _renderCanvas.RenderTransform = _transformGroup;

            // Add hosts to canvas with proper Z-index
            _renderCanvas.Children.Add(_tileMapHost);
            Canvas.SetZIndex(_tileMapHost, 0); // Tiles at bottom

            _renderCanvas.Children.Add(_gridHost);
            Canvas.SetZIndex(_gridHost, 10); // Grid on top of tiles

            System.Diagnostics.Debug.WriteLine("[RenderManager] Initialized with GridVisualHost");
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

        public void ApplyPan(double deltaX, double deltaY)
        {
            _panTransform.X += deltaX;
            _panTransform.Y += deltaY;
            ClampPan();
            ViewportChanged?.Invoke();
        }

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
            ViewportChanged?.Invoke();
        }

        public void ResetView()
        {
            _zoomTransform.ScaleX = 1.0;
            _zoomTransform.ScaleY = 1.0;
            _panTransform.X = 0;
            _panTransform.Y = 0;
            ViewportChanged?.Invoke();
            System.Diagnostics.Debug.WriteLine("[RenderManager] View reset");
        }

        #endregion

        #region Public Methods - Rendering

        /// <summary>
        /// Renders the grid using GridVisualHost
        /// </summary>
        public void RenderGrid(int width, int height, double cellSize, bool showGrid)
        {
            var now = DateTime.Now;
            if ((now - _lastGridRender).TotalMilliseconds < RENDER_THROTTLE_MS)
                return;
            _lastGridRender = now;

            if (!showGrid)
            {
                _gridHost.Clear(); // Add a Clear method to GridVisualHost
                return;
            }

            // Calculate viewport in world coordinates
            var viewport = GetViewportRect();

            // Use your existing GridVisualHost.DrawGridViewport method!
            _gridHost.DrawGridViewport(cellSize, viewport, 100, false, true);

            System.Diagnostics.Debug.WriteLine($"[RenderManager] Grid rendered");
        }

        /// <summary>
        /// Renders the tile map
        /// </summary>
        public void RenderTileMap(TileMap tileMap, double cellSize)
        {
            if (tileMap == null)
            {
                _tileMapHost.Clear();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[RenderManager] Rendering {tileMap.PlacedTiles?.Count ?? 0} tiles");

            // Draw directly on the tile map host
            _tileMapHost.DrawTileMap(tileMap, cellSize);
        }

        /// <summary>
        /// Renders coordinate rulers
        /// </summary>
        public void RenderRulers(int gridWidth, int gridHeight, double cellSize, double viewWidth, double viewHeight)
        {
            var now = DateTime.Now;
            if ((now - _lastRulerRender).TotalMilliseconds < RENDER_THROTTLE_MS)
                return;
            _lastRulerRender = now;

            _rulerCanvas.Children.Clear();

            const double rulerHeight = 30;
            const double rulerWidth = 35;

            // Top ruler background
            var topBg = new System.Windows.Shapes.Rectangle
            {
                Width = viewWidth,
                Height = rulerHeight,
                Fill = new SolidColorBrush(Color.FromRgb(26, 26, 26))
            };
            Canvas.SetLeft(topBg, 0);
            Canvas.SetTop(topBg, 0);
            _rulerCanvas.Children.Add(topBg);

            // Left ruler background
            var leftBg = new System.Windows.Shapes.Rectangle
            {
                Width = rulerWidth,
                Height = viewHeight,
                Fill = new SolidColorBrush(Color.FromRgb(26, 26, 26))
            };
            Canvas.SetLeft(leftBg, 0);
            Canvas.SetTop(leftBg, 0);
            _rulerCanvas.Children.Add(leftBg);

            // Calculate visible range
            var viewport = GetViewportRect();
            int startCol = Math.Max(0, (int)Math.Floor(viewport.Left / cellSize));
            int endCol = Math.Min(gridWidth, (int)Math.Ceiling(viewport.Right / cellSize));
            int startRow = Math.Max(0, (int)Math.Floor(viewport.Top / cellSize));
            int endRow = Math.Min(gridHeight, (int)Math.Ceiling(viewport.Bottom / cellSize));

            // Draw column labels
            for (int col = startCol; col <= endCol; col++)
            {
                var worldX = col * cellSize;
                var screenX = WorldToScreen(new Point(worldX, 0)).X;

                if (screenX < rulerWidth || screenX > viewWidth) continue;

                var label = new TextBlock
                {
                    Text = GetColumnLabel(col),
                    FontSize = 11,
                    Foreground = Brushes.LightGray,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                };

                Canvas.SetLeft(label, screenX + 5);
                Canvas.SetTop(label, 7);
                _rulerCanvas.Children.Add(label);
            }

            // Draw row labels
            for (int row = startRow; row <= endRow; row++)
            {
                var worldY = row * cellSize;
                var screenY = WorldToScreen(new Point(0, worldY)).Y;

                if (screenY < rulerHeight || screenY > viewHeight) continue;

                var label = new TextBlock
                {
                    Text = (row + 1).ToString(),
                    FontSize = 11,
                    Foreground = Brushes.LightGray,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                };

                Canvas.SetLeft(label, 10);
                Canvas.SetTop(label, screenY + 5);
                _rulerCanvas.Children.Add(label);
            }
        }

        #endregion

        #region Private Methods

        private Rect GetViewportRect()
        {
            // Transform viewport corners to world space
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

            double paddingX = _viewWidth * 0.25;
            double paddingY = _viewHeight * 0.25;

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