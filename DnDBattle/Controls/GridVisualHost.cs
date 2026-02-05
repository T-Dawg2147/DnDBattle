using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Lightweight FrameworkElement that hosts a single DrawingVisual and lets callers draw viewport-aware grid lines.
    /// This is pure code (no XAML) to avoid designer/runtime conflicts with visuals.
    /// </summary>
    public class GridVisualHost : FrameworkElement
    {
        private readonly DrawingVisual _visual;

        public DrawingVisual Visual;

        public GridVisualHost()
        {
            _visual = new DrawingVisual();
            Visual = _visual;
            // Register the visual child with the visual/logical tree
            AddVisualChild(_visual);
            AddLogicalChild(_visual);
        }

        // One visual child
        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return _visual;
        }

        // Optionally provide a desired size so layout engine can allocate some space
        protected override Size MeasureOverride(Size availableSize)
        {
            // We don't demand specific size; take available if finite, otherwise 0.
            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                return new Size(0, 0);
            return availableSize;
        }

        #region DrawGridViewport
        /// <summary>
        /// Draw grid lines covering the provided view rectangle (in world/pixel coords).
        /// Call this whenever cell size or viewport changes.
        /// </summary>
        /// <param name="cellSize">Pixels per grid cell</param>
        /// <param name="viewWorldPixels">Viewport rectangle in canvas (world) pixel coordinates</param>
        /// <param name="extraPaddingPixels">Padding to draw slightly beyond viewport</param>
        /// <summary>
        /// Draw grid lines covering the provided view rectangle (in world/pixel coords).
        /// Call this whenever cell size or viewport changes.
        /// </summary>
        /// <param name="cellSize">Pixels per grid cell</param>
        /// <param name="viewWorldPixels">Viewport rectangle in canvas (world) pixel coordinates</param>
        /// <param name="extraPaddingPixels">Padding to draw slightly beyond viewport</param>
        /// <param name="showCoordinates">Whether to show A1, B2 style coordinates</param>
        /// <param name="showGrid">Whether to show the grid lines</param>
        public void DrawGridViewport(double cellSize, Rect viewWorldPixels, double extraPaddingPixels = 16,
            bool showCoordinates = true, bool showGrid = true)
        {
            using (var dc = _visual.RenderOpen())
            {
                if (cellSize <= 0) return;

                viewWorldPixels.Inflate(extraPaddingPixels, extraPaddingPixels);

                int startCol = (int)Math.Floor(viewWorldPixels.Left / cellSize);
                int endCol = (int)Math.Ceiling(viewWorldPixels.Right / cellSize);
                int startRow = (int)Math.Floor(viewWorldPixels.Top / cellSize);
                int endRow = (int)Math.Ceiling(viewWorldPixels.Bottom / cellSize);

                // Clamp to reasonable values (0 to max grid size)
                startCol = Math.Max(0, startCol);
                startRow = Math.Max(0, startRow);
                endCol = Math.Min(endCol, 200); // Max grid width
                endRow = Math.Min(endRow, 200); // Max grid height

                // Only draw grid lines if enabled
                if (showGrid)
                {
                    DrawGridLines(dc, cellSize, startCol, endCol, startRow, endRow, viewWorldPixels);
                }

                // Draw coordinate labels if enabled
                if (showCoordinates)
                {
                    DrawCoordinateLabels(dc, cellSize, startCol, endCol, startRow, endRow);
                }
            }
        }

        private void DrawGridLines(DrawingContext dc, double cellSize, int startCol, int endCol,
            int startRow, int endRow, Rect viewWorldPixels)
        {
            var minorLineColour = Color.FromArgb(120, 200, 200, 200);
            var majorLineColour = Color.FromArgb(200, 255, 255, 255);
            var shadowColour = Color.FromArgb(100, 0, 0, 0);

            var minorLineBrush = new SolidColorBrush(minorLineColour);
            var majorLineBrush = new SolidColorBrush(majorLineColour);
            var shadowBrush = new SolidColorBrush(shadowColour);
            minorLineBrush.Freeze();
            majorLineBrush.Freeze();
            shadowBrush.Freeze();

            var shadowPen = new Pen(shadowBrush, 2.0);
            shadowPen.Freeze();

            var minorPen = new Pen(minorLineBrush, 1.0);
            minorPen.Freeze();

            var majorPen = new Pen(majorLineBrush, 2.0);
            majorPen.Freeze();

            // Draw vertical lines
            for (int c = startCol; c <= endCol; c++)
            {
                double x = c * cellSize;
                bool isMajor = c % 5 == 0;
                var pen = isMajor ? majorPen : minorPen;

                // Shadow line (offset by 1 pixel)
                if (isMajor)
                {
                    dc.DrawLine(shadowPen,
                        new Point(x + 1, viewWorldPixels.Top),
                        new Point(x + 1, viewWorldPixels.Bottom));
                }

                dc.DrawLine(pen,
                    new Point(x, viewWorldPixels.Top),
                    new Point(x, viewWorldPixels.Bottom));
            }

            // Draw horizontal lines
            for (int r = startRow; r <= endRow; r++)
            {
                double y = r * cellSize;
                bool isMajor = r % 5 == 0;
                var pen = isMajor ? majorPen : minorPen;

                // Shadow line
                if (isMajor)
                {
                    dc.DrawLine(shadowPen,
                        new Point(viewWorldPixels.Left, y + 1),
                        new Point(viewWorldPixels.Right, y + 1));
                }

                dc.DrawLine(pen,
                    new Point(viewWorldPixels.Left, y),
                    new Point(viewWorldPixels.Right, y));
            }
        }

        private void DrawCoordinateLabels(DrawingContext dc, double cellSize, int startCol, int endCol,
            int startRow, int endRow)
        {
            // Only draw labels if cells are big enough to show them
            if (cellSize < 24) return;

            var labelBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
            var bgBrush = new SolidColorBrush(Color.FromArgb(150, 30, 30, 30));
            labelBrush.Freeze();
            bgBrush.Freeze();

            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
            double fontSize = Math.Min(12, cellSize / 4);

            // Draw column letters at the top
            for (int c = startCol; c <= endCol; c++)
            {
                if (c < 0) continue;

                string colLabel = GetColumnLabel(c);
                var formattedText = new FormattedText(
                    colLabel,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    labelBrush,
                    1.0);

                double x = c * cellSize + (cellSize - formattedText.Width) / 2;
                double y = startRow * cellSize + 2;

                // Background rectangle
                dc.DrawRectangle(bgBrush, null,
                    new Rect(x - 2, y - 1, formattedText.Width + 4, formattedText.Height + 2));
                dc.DrawText(formattedText, new Point(x, y));
            }

            // Draw row numbers on the left
            for (int r = startRow; r <= endRow; r++)
            {
                if (r < 0) continue;

                string rowLabel = (r + 1).ToString(); // 1-based row numbers
                var formattedText = new FormattedText(
                    rowLabel,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    labelBrush,
                    1.0);

                double x = startCol * cellSize + 2;
                double y = r * cellSize + (cellSize - formattedText.Height) / 2;

                // Background rectangle
                dc.DrawRectangle(bgBrush, null,
                    new Rect(x - 2, y - 1, formattedText.Width + 4, formattedText.Height + 2));
                dc.DrawText(formattedText, new Point(x, y));
            }
        }

        /// <summary>
        /// Draws a tile map
        /// </summary>
        public void DrawTileMap(DnDBattle.Models.Tiles.TileMap tileMap, double cellSize)
        {
            using (var dc = _visual.RenderOpen())
            {
                if (tileMap == null) return;

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
                            drawnCount++;
                    }
                    Debug.WriteLine($"[GridVisualHost] Drew {drawnCount}/{tileMap.PlacedTiles.Count} tiles");
                }
            }
        }

        private bool DrawTile(DrawingContext dc, DnDBattle.Models.Tiles.Tile tile, double cellSize)
        {
            try
            {
                var tileDef = Services.TileService.TileLibraryService.Instance.GetTileById(tile.TileDefinitionId);
                if (tileDef == null)
                {
                    Debug.WriteLine($"[GridVisualHost] Tile definition not found: {tile.TileDefinitionId}");
                    return false;
                }

                var image = Services.TileService.TileImageCacheService.Instance.GetOrLoadImage(tileDef.ImagePath);
                if (image == null)
                {
                    Debug.WriteLine($"[GridVisualHost] Image not loaded: {tileDef.ImagePath}");
                    return false;
                }

                double x = tile.GridX * cellSize;
                double y = tile.GridY * cellSize;

                // Simple rendering (add transformations if needed)
                dc.DrawImage(image, new Rect(x, y, cellSize, cellSize));

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GridVisualHost] Error drawing tile: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Converts a column index to a letter label (0=A, 1=B, ..., 25=Z, 26=AA, etc.)
        /// </summary>
        private string GetColumnLabel(int columnIndex)
        {
            string result = "";
            while (columnIndex >= 0)
            {
                result = (char)('A' + (columnIndex % 26)) + result;
                columnIndex = columnIndex / 26 - 1;
            }
            return result;
        }

        /// <summary>
        /// Gets the coordinate string for a grid position (e.g., "A1", "B5", "AA10")
        /// </summary>
        public static string GetCoordinateString(int gridX, int gridY)
        {
            string col = "";
            int colIndex = gridX;
            while (colIndex >= 0)
            {
                col = (char)('A' + (colIndex % 26)) + col;
                colIndex = colIndex / 26 - 1;
            }
            return $"{col}{gridY + 1}";
        }

        public void Clear()
        {
            using (var dc = _visual.RenderOpen())
            {
                // Opening and immediately closing clears the visual
            }
        }

        #endregion
    }
}