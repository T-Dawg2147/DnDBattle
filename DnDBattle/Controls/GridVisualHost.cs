using System;
using System.Globalization;
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
        // Grid line brushes and pens - created once, reused forever
        private static readonly SolidColorBrush MinorLineBrush;
        private static readonly SolidColorBrush MajorLineBrush;
        private static readonly SolidColorBrush ShadowBrush;
        private static readonly Pen MinorPen;
        private static readonly Pen MajorPen;
        private static readonly Pen ShadowPen;

        // Coordinate label resources
        private static readonly SolidColorBrush LabelBrush;
        private static readonly SolidColorBrush LabelBackgroundBrush;
        private static readonly Typeface LabelTypeface;

        #region Viewport Cache (skip redraw if unchanged)
        private double _lastCellSize;
        private Rect _lastViewport;
        private bool _lastShowCoordinates;
        private bool _lastShowGrid;
        #endregion

        static GridVisualHost()
        {
            // Grid line brushes
            MinorLineBrush = new SolidColorBrush(Color.FromArgb(120, 200, 200, 200));
            MinorLineBrush.Freeze();

            MajorLineBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            MajorLineBrush.Freeze();

            ShadowBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
            ShadowBrush.Freeze();

            // Grid line pens
            MinorPen = new Pen(MinorLineBrush, 1.0);
            MinorPen.Freeze();

            MajorPen = new Pen(MajorLineBrush, 2.0);
            MajorPen.Freeze();

            ShadowPen = new Pen(ShadowBrush, 2.0);
            ShadowPen.Freeze();

            // Coordinate label resources
            LabelBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
            LabelBrush.Freeze();

            LabelBackgroundBrush = new SolidColorBrush(Color.FromArgb(150, 30, 30, 30));
            LabelBackgroundBrush.Freeze();

            LabelTypeface = new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.SemiBold,
                FontStretches.Normal);
        }

        private readonly DrawingVisual _visual = new DrawingVisual();

        public GridVisualHost()
        {
            AddVisualChild(_visual);
            AddLogicalChild(_visual);
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _visual;

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
            // Skip redraw if nothing changed (viewport caching)
            if (Math.Abs(cellSize - _lastCellSize) < 0.01 && 
                viewWorldPixels == _lastViewport && 
                showCoordinates == _lastShowCoordinates && 
                showGrid == _lastShowGrid)
            {
                return;
            }

            // Update cache
            _lastCellSize = cellSize;
            _lastViewport = viewWorldPixels;
            _lastShowCoordinates = showCoordinates;
            _lastShowGrid = showGrid;

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
                endCol = Math.Min(endCol, 200);
                endRow = Math.Min(endRow, 200);

                if (showGrid)
                {
                    DrawGridLines(dc, cellSize, startCol, endCol, startRow, endRow, viewWorldPixels);
                }

                if (showCoordinates)
                {
                    DrawCoordinateLabels(dc, cellSize, startCol, endCol, startRow, endRow);
                }
            }
        }

        private void DrawGridLines(DrawingContext dc, double cellSize, int startCol, int endCol,
            int startRow, int endRow, Rect viewWorldPixels)
        {
            // ✅ Use cached pens - zero allocations!

            // Draw vertical lines
            for (int c = startCol; c <= endCol; c++)
            {
                double x = c * cellSize;
                bool isMajor = c % 5 == 0;
                var pen = isMajor ? MajorPen : MinorPen;

                // Shadow line (offset by 1 pixel) for major lines
                if (isMajor)
                {
                    dc.DrawLine(ShadowPen,
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
                var pen = isMajor ? MajorPen : MinorPen;

                // Shadow line
                if (isMajor)
                {
                    dc.DrawLine(ShadowPen,
                        new Point(viewWorldPixels.Left, y + 1),
                        new Point(viewWorldPixels.Right, y + 1));
                }

                dc.DrawLine(pen,
                    new Point(viewWorldPixels.Left, y),
                    new Point(viewWorldPixels.Right, y));
            }
        }

        public void InvalidateGrid()
        {
            _lastCellSize = -1;
            _lastViewport = Rect.Empty;
            _lastShowCoordinates = false;
            _lastShowGrid = false;
        }

        private void DrawCoordinateLabels(DrawingContext dc, double cellSize, int startCol, int endCol,
            int startRow, int endRow)
        {
            // Only draw labels if cells are big enough to show them
            if (cellSize < 24) return;

            // ✅ Use cached brushes and typeface - zero allocations!
            double fontSize = Math.Min(12, cellSize / 4);

            // Draw column letters at the top
            for (int c = startCol; c <= endCol; c++)
            {
                if (c < 0) continue;

                string colLabel = GetColumnLabel(c);
                var formattedText = new FormattedText(
                    colLabel,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    LabelTypeface,  // ✅ Cached
                    fontSize,
                    LabelBrush,     // ✅ Cached
                    1.0);

                double x = c * cellSize + (cellSize - formattedText.Width) / 2;
                double y = startRow * cellSize + 2;

                dc.DrawRectangle(LabelBackgroundBrush, null,  // ✅ Cached
                    new Rect(x - 2, y - 1, formattedText.Width + 4, formattedText.Height + 2));
                dc.DrawText(formattedText, new Point(x, y));
            }

            // Draw row numbers on the left
            for (int r = startRow; r <= endRow; r++)
            {
                if (r < 0) continue;

                string rowLabel = (r + 1).ToString();
                var formattedText = new FormattedText(
                    rowLabel,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    LabelTypeface,  // ✅ Cached
                    fontSize,
                    LabelBrush,     // ✅ Cached
                    1.0);

                double x = startCol * cellSize + 2;
                double y = r * cellSize + (cellSize - formattedText.Height) / 2;

                dc.DrawRectangle(LabelBackgroundBrush, null,  // ✅ Cached
                    new Rect(x - 2, y - 1, formattedText.Width + 4, formattedText.Height + 2));
                dc.DrawText(formattedText, new Point(x, y));
            }
        }

        /// <summary>
        /// Converts a column index to a letter label (0=A, 1=B, ..., 25=Z, 26=AA, etc.)
        /// </summary>
        private static string GetColumnLabel(int columnIndex)
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
        public static string GetCoordinateString(int col, int row)
        {
            return $"{GetColumnLabel(col)}{row + 1}";
        }
    }
}