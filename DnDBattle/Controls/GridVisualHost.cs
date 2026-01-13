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

        public GridVisualHost()
        {
            _visual = new DrawingVisual();
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

        /// <summary>
        /// Draw grid lines covering the provided view rectangle (in world/pixel coords).
        /// Call this whenever cell size or viewport changes.
        /// </summary>
        /// <param name="cellSize">Pixels per grid cell</param>
        /// <param name="viewWorldPixels">Viewport rectangle in canvas (world) pixel coordinates</param>
        /// <param name="extraPaddingPixels">Padding to draw slightly beyond viewport</param>
        public void DrawGridViewport(double cellSize, Rect viewWorldPixels, double extraPaddingPixels = 16)
        {
            using (var dc = _visual.RenderOpen())
            {
                if (cellSize <= 0) return;

                viewWorldPixels.Inflate(extraPaddingPixels, extraPaddingPixels);

                int startCol = (int)Math.Floor(viewWorldPixels.Left / cellSize);
                int endCol = (int)Math.Ceiling(viewWorldPixels.Right / cellSize);
                int startRow = (int)Math.Floor(viewWorldPixels.Top / cellSize);
                int endRow = (int)Math.Ceiling(viewWorldPixels.Bottom / cellSize);

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

                // Shadown lines (offset by 1 pixel)
                for (int c = startCol; c <= endCol; c++)
                {
                    double x = c * cellSize;
                    if (c % 5 == 0)
                    {
                        dc.DrawLine(shadowPen, new Point(x + 1, viewWorldPixels.Top), new Point(x + 1, viewWorldPixels.Bottom));
                    }
                }
                for (int r = startRow; r <= endRow; r++)
                {
                    double y = r * cellSize;
                    if (r % 5 == 0)
                    {
                        dc.DrawLine(shadowPen, new Point(viewWorldPixels.Left, y + 1), new Point(viewWorldPixels.Right, y + 1));
                    }
                }

                // Main grid lines
                for (int c = startCol; c <= endCol; c++)
                {
                    double x = c * cellSize;
                    var pen = (c % 5 == 0) ? majorPen : minorPen;
                    dc.DrawLine(pen, new Point(x, viewWorldPixels.Top), new Point(x, viewWorldPixels.Bottom));
                }

                for (int r = startRow; r <= endRow; r++)
                {
                    double y = r * cellSize;
                    var pen = (r % 5 == 0) ? majorPen : minorPen;
                    dc.DrawLine(pen, new Point(viewWorldPixels.Left, y), new Point(viewWorldPixels.Right, y));
                }
            }
            InvalidateVisual();
        }
    }
}