using System;
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

        #region Cached Brushes and Pens (static, frozen once)
        private static readonly SolidColorBrush MinorLineBrush;
        private static readonly SolidColorBrush MajorLineBrush;
        private static readonly SolidColorBrush ShadowBrush;
        private static readonly SolidColorBrush LabelBrush;
        private static readonly SolidColorBrush LabelBgBrush;
        private static readonly Pen ShadowPen;
        private static readonly Pen MinorPen;
        private static readonly Pen MajorPen;
        private static readonly Typeface LabelTypeface;

        static GridVisualHost()
        {
            // Initialize and freeze all brushes/pens once
            MinorLineBrush = new SolidColorBrush(Color.FromArgb(120, 200, 200, 200));
            MajorLineBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            ShadowBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
            LabelBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
            LabelBgBrush = new SolidColorBrush(Color.FromArgb(150, 30, 30, 30));

            MinorLineBrush.Freeze();
            MajorLineBrush.Freeze();
            ShadowBrush.Freeze();
            LabelBrush.Freeze();
            LabelBgBrush.Freeze();

            ShadowPen = new Pen(ShadowBrush, 2.0);
            MinorPen = new Pen(MinorLineBrush, 1.0);
            MajorPen = new Pen(MajorLineBrush, 2.0);

            ShadowPen.Freeze();
            MinorPen.Freeze();
            MajorPen.Freeze();

            LabelTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        }
        #endregion

        #region Viewport Cache (skip redraw if unchanged)
        private double _lastCellSize;
        private int _lastStartCol, _lastEndCol, _lastStartRow, _lastEndRow;
        private bool _lastShowGrid;
        private bool _lastShowCoordinates;
        #endregion

        public GridVisualHost()
        {
            _visual = new DrawingVisual();
            AddVisualChild(_visual);
            AddLogicalChild(_visual);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return _visual;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                return new Size(0, 0);
            return availableSize;
        }

        /// <summary>
        /// Forces a full redraw on next call (use when grid settings change)
        /// </summary>
        public void InvalidateGrid()
        {
            _lastCellSize = -1;
        }

        #region DrawGridViewport
        public void DrawGridViewport(double cellSize, Rect viewWorldPixels, double extraPaddingPixels = 16,
            bool showCoordinates = true, bool showGrid = true)
        {
            if (cellSize <= 0) return;

            viewWorldPixels.Inflate(extraPaddingPixels, extraPaddingPixels);

            int startCol = Math.Max(0, (int)Math.Floor(viewWorldPixels.Left / cellSize));
            int endCol = Math.Min(200, (int)Math.Ceiling(viewWorldPixels.Right / cellSize));
            int startRow = Math.Max(0, (int)Math.Floor(viewWorldPixels.Top / cellSize));
            int endRow = Math.Min(200, (int)Math.Ceiling(viewWorldPixels.Bottom / cellSize));

            // Skip redraw if nothing changed
            if (cellSize == _lastCellSize &&
                startCol == _lastStartCol && endCol == _lastEndCol &&
                startRow == _lastStartRow && endRow == _lastEndRow &&
                showGrid == _lastShowGrid && showCoordinates == _lastShowCoordinates)
            {
                return; // Nothing changed, skip expensive redraw
            }

            // Update cache
            _lastCellSize = cellSize;
            _lastStartCol = startCol;
            _lastEndCol = endCol;
            _lastStartRow = startRow;
            _lastEndRow = endRow;
            _lastShowGrid = showGrid;
            _lastShowCoordinates = showCoordinates;

            using (var dc = _visual.RenderOpen())
            {
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
            // Draw vertical lines
            for (int c = startCol; c <= endCol; c++)
            {
                double x = c * cellSize;
                bool isMajor = c % 5 == 0;
                var pen = isMajor ? MajorPen : MinorPen;

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

        private void DrawCoordinateLabels(DrawingContext dc, double cellSize, int startCol, int endCol,
            int startRow, int endRow)
        {
            if (cellSize < 24) return;

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
                    LabelTypeface,
                    fontSize,
                    LabelBrush,
                    1.0);

                double x = c * cellSize + (cellSize - formattedText.Width) / 2;
                double y = startRow * cellSize + 2;

                dc.DrawRectangle(LabelBgBrush, null,
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
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    LabelTypeface,
                    fontSize,
                    LabelBrush,
                    1.0);

                double x = startCol * cellSize + 2;
                double y = r * cellSize + (cellSize - formattedText.Height) / 2;

                dc.DrawRectangle(LabelBgBrush, null,
                    new Rect(x - 2, y - 1, formattedText.Width + 4, formattedText.Height + 2));
                dc.DrawText(formattedText, new Point(x, y));
            }
        }

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
        #endregion
    }
}