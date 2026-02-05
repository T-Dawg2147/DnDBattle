using DnDBattle.Controls;
using DnDBattle.Models.Tiles;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    public class BattleGridRenderManager
    {
        #region Fields

        private readonly Canvas _renderCanvas;
        private readonly Canvas _rulerCanvas;

        private readonly GridVisualHost _gridHost;
        private readonly GridVisualHost _tileMapHost;
        private readonly GridVisualHost _fogHost;

        private readonly TransformGroup _transformGroup = new TransformGroup();
        private readonly ScaleTransform _zoomTransform = new ScaleTransform(1, 1);
        private readonly TranslateTransform _panTransform = new TranslateTransform(0, 0);

        private int _gridWidth = 100;
        private int _gridHeight = 100;
        private double _cellSize = 48;
        private double _viewWidth;
        private double _viewHeight;

        private bool _isInteracting = false;
        private System.Windows.Threading.DispatcherTimer _deferredRenderTimer;

        private DateTime _lastRulerRender = DateTime.MinValue;
        private const int RULER_THROTTLE_MS = 100;

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
            _fogHost = new GridVisualHost();

            _transformGroup.Children.Add(_panTransform);   // Index 0
            _transformGroup.Children.Add(_zoomTransform);  // Index 1

            _renderCanvas.RenderTransform = _transformGroup;

            _renderCanvas.Children.Add(_tileMapHost);
            Canvas.SetZIndex(_tileMapHost, 0);

            _renderCanvas.Children.Add(_gridHost);
            Canvas.SetZIndex(_gridHost, 10);

            _renderCanvas.Children.Add(_fogHost);
            Canvas.SetZIndex(_fogHost, 50);

            // Setup deferred render timer
            _deferredRenderTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _deferredRenderTimer.Tick += (s, e) =>
            {
                _deferredRenderTimer.Stop();
                _isInteracting = false;
                RenderFullViewport();
            };

            Debug.WriteLine("[RenderManager] Initialized - Transform order: Pan → Zoom");
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

            _isInteracting = true;

            // Don't start timer during pan - only FinishInteraction will render
        }

        /// <summary>
        /// OPTIMIZED: Zoom with correct transform order
        /// </summary>
        public void ApplyZoom(double factor, Point center)
        {
            var worldPoint = ScreenToWorld(center);

            _zoomTransform.ScaleX *= factor;
            _zoomTransform.ScaleY *= factor;

            _zoomTransform.ScaleX = Math.Max(0.1, Math.Min(5.0, _zoomTransform.ScaleX));
            _zoomTransform.ScaleY = Math.Max(0.1, Math.Min(5.0, _zoomTransform.ScaleY));

            double newZoom = _zoomTransform.ScaleX;

            _panTransform.X = center.X / newZoom - worldPoint.X;
            _panTransform.Y = center.Y / newZoom - worldPoint.Y;

            _isInteracting = true;
            _deferredRenderTimer.Stop();
            _deferredRenderTimer.Start();
        }

        /// <summary>
        /// Call when interaction finishes (mouse up)
        /// </summary>
        public void FinishInteraction()
        {
            _deferredRenderTimer.Stop();
            _isInteracting = false;

            ClampPan();
            RenderFullViewport();

            Debug.WriteLine("[RenderManager] Interaction finished");
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
        /// Full viewport render
        /// </summary>
        private void RenderFullViewport()
        {
            var sw = Stopwatch.StartNew();

            double currentZoom = _zoomTransform.ScaleX;
            var viewport = GetViewportRect();

            RenderGridOptimized(viewport, currentZoom);
            RenderRulersOptimized(currentZoom);

            sw.Stop();
            Debug.WriteLine($"[RenderManager] Full render: {sw.ElapsedMilliseconds}ms at {currentZoom:P0} zoom");

            ViewportChanged?.Invoke();
        }

        private void RenderGridOptimized(Rect viewport, double zoom)
        {
            if (zoom < 0.3)
            {
                _gridHost.Clear();
                return;
            }

            int cellCountX = (int)Math.Ceiling(viewport.Width / _cellSize);
            int cellCountY = (int)Math.Ceiling(viewport.Height / _cellSize);
            int totalCells = cellCountX * cellCountY;

            if (totalCells > 10000)
            {
                _gridHost.Clear();
                return;
            }

            bool showCoords = totalCells < 5000;

            _gridHost.DrawGridViewport(_cellSize, viewport, 50, showCoords, true);
        }

        private void RenderRulersOptimized(double zoom)
        {
            var now = DateTime.Now;
            if ((now - _lastRulerRender).TotalMilliseconds < RULER_THROTTLE_MS)
                return;
            _lastRulerRender = now;

            _rulerCanvas.Children.Clear();

            if (zoom < 0.5)
                return;

            const double rulerHeight = 30;
            const double rulerWidth = 35;

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

            var viewport = GetViewportRect();
            int startCol = Math.Max(0, (int)Math.Floor(viewport.Left / _cellSize));
            int endCol = Math.Min(_gridWidth, (int)Math.Ceiling(viewport.Right / _cellSize));
            int startRow = Math.Max(0, (int)Math.Floor(viewport.Top / _cellSize));
            int endRow = Math.Min(_gridHeight, (int)Math.Ceiling(viewport.Bottom / _cellSize));

            int skipFactor = 1;
            if (endCol - startCol > 30) skipFactor = 2;
            if (endCol - startCol > 60) skipFactor = 5;

            var textBrush = Brushes.LightGray;

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

        public void RenderGrid(int width, int height, double cellSize, bool showGrid)
        {
            if (!showGrid)
            {
                _gridHost.Clear();
                return;
            }

            if (_isInteracting)
                return;

            var viewport = GetViewportRect();
            RenderGridOptimized(viewport, _zoomTransform.ScaleX);
        }

        public void RenderRulers(int gridWidth, int gridHeight, double cellSize, double viewWidth, double viewHeight)
        {
            if (_isInteracting)
                return;

            RenderRulersOptimized(_zoomTransform.ScaleX);
        }

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

        /// <summary>
        /// Renders fog of war overlay
        /// </summary>
        public void RenderFog(BattleGridFogOfWarManager fogManager, double cellSize)
        {
            if (fogManager == null || !fogManager.IsEnabled)
            {
                _fogHost.Clear();
                return;
            }

            var sw = Stopwatch.StartNew();

            using (var dc = _fogHost.Visual.RenderOpen())
            {
                // Determine fog opacity based on view mode
                double opacity = fogManager.IsPlayerView ? 0.9 : 0.6;
                var fogBrush = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 0, 0, 0));
                fogBrush.Freeze();

                // Get visible viewport to optimize rendering
                var viewport = GetViewportRect();
                int startX = Math.Max(0, (int)Math.Floor(viewport.Left / cellSize));
                int endX = Math.Min(fogManager.GridWidth - 1, (int)Math.Ceiling(viewport.Right / cellSize));
                int startY = Math.Max(0, (int)Math.Floor(viewport.Top / cellSize));
                int endY = Math.Min(fogManager.GridHeight - 1, (int)Math.Ceiling(viewport.Bottom / cellSize));

                int renderedCells = 0;

                // Only render fog for hidden cells in viewport
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        if (!fogManager.IsCellRevealed(x, y))
                        {
                            var rect = new Rect(x * cellSize, y * cellSize, cellSize, cellSize);
                            dc.DrawRectangle(fogBrush, null, rect);
                            renderedCells++;
                        }
                    }
                }

                sw.Stop();
                Debug.WriteLine($"[RenderManager] Fog render: {renderedCells} cells in {sw.ElapsedMilliseconds}ms");
            }
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
            if (_isInteracting)
            {
                Debug.WriteLine("⚠️ ClampPan blocked during interaction");
                return;
            }

            if (_viewWidth <= 0 || _viewHeight <= 0) return;

            double gridPixelWidth = _gridWidth * _cellSize;
            double gridPixelHeight = _gridHeight * _cellSize;
            double zoom = _zoomTransform.ScaleX;

            double maxPanX = 0;
            double maxPanY = 0;
            double minPanX = (_viewWidth / zoom) - gridPixelWidth;
            double minPanY = (_viewHeight / zoom) - gridPixelHeight;

            double paddingX = (_viewWidth / zoom) * 0.5;
            double paddingY = (_viewHeight / zoom) * 0.5;

            _panTransform.X = Math.Max(minPanX - paddingX, Math.Min(maxPanX + paddingX, _panTransform.X));
            _panTransform.Y = Math.Max(minPanY - paddingY, Math.Min(maxPanY + paddingY, _panTransform.Y));
        }

        private Point WorldToScreen(Point worldPoint)
        {
            double zoom = _zoomTransform.ScaleX;
            return new Point(
                (worldPoint.X + _panTransform.X) * zoom,
                (worldPoint.Y + _panTransform.Y) * zoom
            );
        }

        private Point ScreenToWorld(Point screenPoint)
        {
            double zoom = _zoomTransform.ScaleX;
            return new Point(
                screenPoint.X / zoom - _panTransform.X,
                screenPoint.Y / zoom - _panTransform.Y
            );
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