using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    /// <summary>
    /// Manages all rendering for the battle grid
    /// Handles: Grid lines, coordinate rulers, tile maps, backgrounds
    /// </summary>
    public class BattleGridRenderManager
    {
        #region Fields

        private readonly Canvas _renderCanvas;
        private readonly Canvas _rulerCanvas;

        // Canvas Dims
        private int _gridWidth = 100;
        private int _gridHeight = 100;
        private double _cellSize = 48;
        private double _viewWidth;
        private double _viewHeight;

        // Visual layers (ordered by Z-index)
        private readonly DrawingVisual _backgroundVisual = new DrawingVisual();
        private readonly DrawingVisual _tileMapVisual = new DrawingVisual();
        private readonly DrawingVisual _gridVisual = new DrawingVisual();

        // Transform for pan/zoom
        private readonly TransformGroup _transformGroup = new TransformGroup();
        private readonly ScaleTransform _zoomTransform = new ScaleTransform(1, 1);
        private readonly TranslateTransform _panTransform = new TranslateTransform(0, 0);

        // Render Thottling
        private DateTime _lastGridRender = DateTime.MinValue;
        private DateTime _lastRulerRender = DateTime.MinValue;
        private const int RENDER_THROTTLE_MS = 50;

        #endregion

        #region Properties

        public void SetViewportSize(double width, double height)
        {
            _viewWidth = width;
            _viewWidth = height;
        }

        public void SetGridDimensions(int width, int height, double cellSize)
        {
            _gridWidth = width;
            _gridHeight = height;
            _cellSize = cellSize;
        }

        #endregion

        #region Events

        public event Action ViewportChanged;

        #endregion

        #region Constructor

        public BattleGridRenderManager(Canvas renderCanvas, Canvas rulerCanvas)
        {
            _renderCanvas = renderCanvas ?? throw new ArgumentNullException(nameof(renderCanvas));
            _rulerCanvas = rulerCanvas ?? throw new ArgumentNullException(nameof(rulerCanvas));

            // Setup transform
            _transformGroup.Children.Add(_zoomTransform);
            _transformGroup.Children.Add(_panTransform);
            _renderCanvas.RenderTransform = _transformGroup;

            // Add visual layers
            AddVisualLayer(_backgroundVisual, 0);
            AddVisualLayer(_tileMapVisual, 5);
            AddVisualLayer(_gridVisual, 10);

            System.Diagnostics.Debug.WriteLine("[RenderManager] Initialized");
        }

        #endregion

        #region Public Methods - Transform

        public TransformGroup GetTransform() => _transformGroup;

        public double GetZoomLevel() => _zoomTransform.ScaleX;

        public void ApplyPan(double deltaX, double deltaY)
        {
            _panTransform.X += deltaX;
            _panTransform.Y += deltaY;

            ClampPan();

            ViewportChanged?.Invoke();
        }

        /// <summary>
        /// Clamps panning to prevent going outside grid bounds
        /// </summary>
        private void ClampPan()
        {
            if (_viewWidth <= 0 || _viewHeight <= 0) return;

            // Calculate grid bounds in world space
            double gridPixelWidth = _gridWidth * _cellSize;
            double gridPixelHeight = _gridHeight * _cellSize;

            // Calculate current zoom level
            double zoom = _zoomTransform.ScaleX;

            // Calculate minimum pan (grid's bottom-right corner at viewport's top-left)
            double minPanX = _viewWidth - (gridPixelWidth * zoom);
            double minPanY = _viewHeight - (gridPixelHeight * zoom);

            // Calculate maximum pan (grid's top-left at viewport's bottom-right)
            double maxPanX = 0;
            double maxPanY = 0;

            // Clamp with some padding (25% of viewport)
            double paddingX = _viewWidth * 0.25;
            double paddingY = _viewHeight * 0.25;

            _panTransform.X = Math.Max(minPanX - paddingX, Math.Min(maxPanX + paddingX, _panTransform.X));
            _panTransform.Y = Math.Max(minPanY - paddingY, Math.Min(maxPanY + paddingY, _panTransform.Y));
        }

        public void ApplyZoom(double factor, Point center)
        {
            // Calculate absolute position before zoom
            double absX = center.X * _zoomTransform.ScaleX + _panTransform.X;
            double absY = center.Y * _zoomTransform.ScaleY + _panTransform.Y;

            // Apply zoom
            _zoomTransform.ScaleX *= factor;
            _zoomTransform.ScaleY *= factor;

            // Clamp zoom level
            _zoomTransform.ScaleX = Math.Max(0.1, Math.Min(5.0, _zoomTransform.ScaleX));
            _zoomTransform.ScaleY = Math.Max(0.1, Math.Min(5.0, _zoomTransform.ScaleY));

            // Adjust pan to keep center point stable
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
        }

        #endregion

        #region Public Methods - Rendering

        /// <summary>
        /// Renders the grid lines and background
        /// </summary>
        public void RenderGrid(int width, int height, double cellSize, bool showGrid)
        {
            // THROTTLE: Don't render too frequently
            var now = DateTime.Now;
            if ((now - _lastGridRender).TotalMilliseconds < RENDER_THROTTLE_MS)
            {
                return; // Skip this render
            }
            _lastGridRender = now;

            using (var dc = _gridVisual.RenderOpen())
            {
                if (!showGrid)
                {
                    System.Diagnostics.Debug.WriteLine("[RenderManager] Grid hidden");
                    return;
                }

                double gridWidth = width * cellSize;
                double gridHeight = height * cellSize;

                // Grid line pen - MORE VISIBLE
                var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 150, 150, 150)), 1);
                gridPen.Freeze();

                // Draw vertical lines
                for (int x = 0; x <= width; x++)
                {
                    double xPos = x * cellSize;
                    dc.DrawLine(gridPen, new Point(xPos, 0), new Point(xPos, gridHeight));
                }

                // Draw horizontal lines
                for (int y = 0; y <= height; y++)
                {
                    double yPos = y * cellSize;
                    dc.DrawLine(gridPen, new Point(0, yPos), new Point(gridWidth, yPos));
                }
            }
        }

        /// <summary>
        /// Renders coordinate rulers (A, B, C... and 1, 2, 3...)
        /// </summary>
        public void RenderRulers(int gridWidth, int gridHeight, double cellSize, double viewWidth, double viewHeight)
        {
            // THROTTLE: Don't render too frequently
            var now = DateTime.Now;
            if ((now - _lastRulerRender).TotalMilliseconds < RENDER_THROTTLE_MS)
            {
                return; // Skip this render
            }
            _lastRulerRender = now;

            _rulerCanvas.Children.Clear();

            const double rulerHeight = 25;
            const double rulerWidth = 30;

            var bgColor = Color.FromRgb(37, 37, 38);
            var textColor = Color.FromRgb(200, 200, 200);
            var lineColor = Color.FromRgb(80, 80, 80);

            // Top ruler background
            var topBg = new Rectangle
            {
                Width = viewWidth,
                Height = rulerHeight,
                Fill = new SolidColorBrush(bgColor)
            };
            Canvas.SetLeft(topBg, 0);
            Canvas.SetTop(topBg, 0);
            _rulerCanvas.Children.Add(topBg);

            // Left ruler background
            var leftBg = new Rectangle
            {
                Width = rulerWidth,
                Height = viewHeight,
                Fill = new SolidColorBrush(bgColor)
            };
            Canvas.SetLeft(leftBg, 0);
            Canvas.SetTop(leftBg, 0);
            _rulerCanvas.Children.Add(leftBg);

            // Calculate visible range in WORLD coordinates
            var topLeftWorld = ScreenToWorld(new Point(0, 0));
            var bottomRightWorld = ScreenToWorld(new Point(viewWidth, viewHeight));

            int startCol = Math.Max(0, (int)Math.Floor(topLeftWorld.X / cellSize));
            int endCol = Math.Min(gridWidth - 1, (int)Math.Ceiling(bottomRightWorld.X / cellSize));
            int startRow = Math.Max(0, (int)Math.Floor(topLeftWorld.Y / cellSize));
            int endRow = Math.Min(gridHeight - 1, (int)Math.Ceiling(bottomRightWorld.Y / cellSize));

            System.Diagnostics.Debug.WriteLine($"[RenderManager] Visible range: Cols {startCol}-{endCol}, Rows {startRow}-{endRow}");

            // Draw column labels (top ruler)
            for (int col = startCol; col <= endCol; col++)
            {
                // World position of this column
                var worldX = col * cellSize;

                // Convert to screen position
                var screenPos = WorldToScreen(new Point(worldX, 0));

                // Skip if outside ruler area
                if (screenPos.X < rulerWidth || screenPos.X > viewWidth) continue;

                // Draw tick mark
                var tick = new Line
                {
                    X1 = screenPos.X,
                    Y1 = rulerHeight - 5,
                    X2 = screenPos.X,
                    Y2 = rulerHeight,
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 1
                };
                _rulerCanvas.Children.Add(tick);

                // Draw label
                var label = new TextBlock
                {
                    Text = GetColumnLabel(col),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(textColor),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                double labelX = screenPos.X + (cellSize * _zoomTransform.ScaleX / 2) - label.DesiredSize.Width / 2;
                Canvas.SetLeft(label, labelX);
                Canvas.SetTop(label, 4);
                _rulerCanvas.Children.Add(label);
            }

            // Draw row labels (left ruler)
            for (int row = startRow; row <= endRow; row++)
            {
                // World position of this row
                var worldY = row * cellSize;

                // Convert to screen position
                var screenPos = WorldToScreen(new Point(0, worldY));

                // Skip if outside ruler area
                if (screenPos.Y < rulerHeight || screenPos.Y > viewHeight) continue;

                // Draw tick mark
                var tick = new Line
                {
                    X1 = rulerWidth - 5,
                    Y1 = screenPos.Y,
                    X2 = rulerWidth,
                    Y2 = screenPos.Y,
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 1
                };
                _rulerCanvas.Children.Add(tick);

                // Draw label
                var label = new TextBlock
                {
                    Text = (row + 1).ToString(), // 1-based
                    FontSize = 10,
                    Foreground = new SolidColorBrush(textColor),
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                Canvas.SetLeft(label, rulerWidth - label.DesiredSize.Width - 5);
                Canvas.SetTop(label, screenPos.Y + (cellSize * _zoomTransform.ScaleY / 2) - label.DesiredSize.Height / 2);
                _rulerCanvas.Children.Add(label);
            }

            System.Diagnostics.Debug.WriteLine($"[RenderManager] Rulers rendered: {_rulerCanvas.Children.Count} elements");
        }

        /// <summary>
        /// Renders a tile map
        /// </summary>
        public void RenderTileMap(TileMap tileMap, double cellSize)
        {
            using (var dc = _tileMapVisual.RenderOpen())
            {
                if (tileMap == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RenderManager] No tile map to render");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[RenderManager] Rendering tile map: {tileMap.PlacedTiles?.Count ?? 0} tiles");

                // Draw background
                var bgColor = (Color)ColorConverter.ConvertFromString(tileMap.BackgroundColor ?? "#FF1A1A1A");
                var bgBrush = new SolidColorBrush(bgColor);
                bgBrush.Freeze();

                double mapWidth = tileMap.Width * cellSize;
                double mapHeight = tileMap.Height * cellSize;

                dc.DrawRectangle(bgBrush, null, new Rect(0, 0, mapWidth, mapHeight));

                // Draw tiles
                if (tileMap.PlacedTiles != null)
                {
                    int drawnCount = 0;
                    foreach (var tile in tileMap.PlacedTiles.OrderBy(t => t.ZIndex ?? 0))
                    {
                        if (DrawTile(dc, tile, cellSize))
                        {
                            drawnCount++;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[RenderManager] Drew {drawnCount} tiles");
                }
            }
        }

        #endregion

        #region Private Methods

        private void AddVisualLayer(DrawingVisual visual, int zIndex)
        {
            var host = new VisualHost(visual);
            _renderCanvas.Children.Add(host);
            Canvas.SetZIndex(host, zIndex);
        }

        private bool DrawTile(DrawingContext dc, Tile tile, double cellSize)
        {
            try
            {
                // Get tile definition
                var tileDef = Services.TileService.TileLibraryService.Instance.GetTileById(tile.TileDefinitionId);
                if (tileDef == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[RenderManager] ⚠️ Tile definition not found: {tile.TileDefinitionId}");

                    // Draw a placeholder red square so we can see SOMETHING
                    double x = tile.GridX * cellSize;
                    double y = tile.GridY * cellSize;
                    var redBrush = new SolidColorBrush(Colors.Red);
                    redBrush.Freeze();
                    dc.DrawRectangle(redBrush, null, new Rect(x, y, cellSize, cellSize));

                    return false;
                }

                // Load image
                var image = Services.TileService.TileImageCacheService.Instance.GetOrLoadImage(tileDef.ImagePath);
                if (image == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[RenderManager] ⚠️ Image not loaded: {tileDef.ImagePath}");

                    // Draw placeholder yellow square
                    double x = tile.GridX * cellSize;
                    double y = tile.GridY * cellSize;
                    var yellowBrush = new SolidColorBrush(Colors.Yellow);
                    yellowBrush.Freeze();
                    dc.DrawRectangle(yellowBrush, null, new Rect(x, y, cellSize, cellSize));

                    return false;
                }

                double posX = tile.GridX * cellSize;
                double posY = tile.GridY * cellSize;

                System.Diagnostics.Debug.WriteLine($"[RenderManager] Drawing tile at ({tile.GridX}, {tile.GridY}) → screen ({posX}, {posY})");

                // Apply transformations if needed
                if (tile.Rotation != 0 || tile.FlipHorizontal || tile.FlipVertical)
                {
                    dc.PushTransform(new TranslateTransform(posX + cellSize / 2, posY + cellSize / 2));

                    if (tile.Rotation != 0)
                        dc.PushTransform(new RotateTransform(tile.Rotation));

                    if (tile.FlipHorizontal || tile.FlipVertical)
                        dc.PushTransform(new ScaleTransform(tile.FlipHorizontal ? -1 : 1, tile.FlipVertical ? -1 : 1));

                    dc.DrawImage(image, new Rect(-cellSize / 2, -cellSize / 2, cellSize, cellSize));

                    if (tile.FlipHorizontal || tile.FlipVertical) dc.Pop();
                    if (tile.Rotation != 0) dc.Pop();
                    dc.Pop();
                }
                else
                {
                    dc.DrawImage(image, new Rect(posX, posY, cellSize, cellSize));
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RenderManager] ❌ Error drawing tile: {ex.Message}");
                return false;
            }
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

        #region Helper Class

        /// <summary>
        /// FrameworkElement wrapper for DrawingVisual
        /// </summary>
        private class VisualHost : FrameworkElement
        {
            private readonly DrawingVisual _visual;

            public VisualHost(DrawingVisual visual)
            {
                _visual = visual;
                IsHitTestVisible = false;
            }

            protected override int VisualChildrenCount => 1;
            protected override Visual GetVisualChild(int index) => _visual;
        }

        #endregion
    }
}