using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DnDBattle.Services.TileService;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Measurement, Path Preview, and Movement functionality
    /// </summary>
    public partial class BattleGridControl
    {
        #region Measurement Mode

        public void SetMeasureMode(bool enabled)
        {
            _measureMode = true;
            _measureStart = null;
            RenderCanvas.Cursor = enabled ? System.Windows.Input.Cursors.Cross : System.Windows.Input.Cursors.Arrow;
            RedrawMeasureVisual();
        }

        public bool IsMeasureMode => _measureMode;

        private void RedrawMeasureVisual()
        {
            using (var dc = _measureVisual.RenderOpen())
            {
                if (!_measureMode || !_measureStart.HasValue) return;

                var startPx = new Point(
                    _measureStart.Value.X * GridCellSize + GridCellSize / 2,
                    _measureStart.Value.Y * GridCellSize + GridCellSize / 2);
                var endPx = new Point(
                    _measureEnd.X * GridCellSize + GridCellSize / 2,
                    _measureEnd.Y * GridCellSize + GridCellSize / 2);

                // Draw the measurement line
                var linePen = new Pen(Brushes.Yellow, 3);
                linePen.DashStyle = DashStyles.Dash;
                linePen.Freeze();

                // Shadow line
                var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), 5);
                shadowPen.Freeze();
                dc.DrawLine(shadowPen,
                    new Point(startPx.X + 2, startPx.Y + 2),
                    new Point(endPx.X + 2, endPx.Y + 2));

                dc.DrawLine(linePen, startPx, endPx);

                // Draw endpoints
                dc.DrawEllipse(Brushes.Yellow, new Pen(Brushes.Black, 2), startPx, 8, 8);
                dc.DrawEllipse(Brushes.Yellow, new Pen(Brushes.Black, 2), endPx, 8, 8);

                // Calculate distances
                double dx = _measureEnd.X - _measureStart.Value.X;
                double dy = _measureEnd.Y - _measureStart.Value.Y;

                // D&D uses different movement costs:
                // - Orthogonal (straight): 1 square = 5ft
                // - Diagonal: Using the 5-10-5 rule or Euclidean
                double euclideanSquares = Math.Sqrt(dx * dx + dy * dy);
                double manhattanSquares = Math.Abs(dx) + Math.Abs(dy);

                // 5-10-5 diagonal rule approximation
                double diagonals = Math.Min(Math.Abs(dx), Math.Abs(dy));
                double straights = Math.Max(Math.Abs(dx), Math.Abs(dy)) - diagonals;
                double dndSquares = straights + diagonals * 1.5; // Each diagonal costs 1.5 squares

                double feetEuclidean = euclideanSquares * 5;
                double feetDnd = dndSquares * 5;

                // Draw measurement label
                var midPoint = new Point((startPx.X + endPx.X) / 2, (startPx.Y + endPx.Y) / 2 - 40);

                string measureText = $"📏 {euclideanSquares:F1} squares ({feetEuclidean:F0} ft)\n" +
                                    $"🎲 D&D: {dndSquares:F1} squares ({feetDnd:F0} ft)";

                var formattedText = new FormattedText(
                    measureText,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    14,
                    Brushes.White,
                    1.0);

                // Background for text
                var textBounds = new Rect(
                    midPoint.X - formattedText.Width / 2 - 8,
                    midPoint.Y - 4,
                    formattedText.Width + 16,
                    formattedText.Height + 8);

                dc.DrawRoundedRectangle(
                    new SolidColorBrush(Color.FromArgb(220, 30, 30, 30)),
                    new Pen(Brushes.Yellow, 2),
                    textBounds, 6, 6);

                dc.DrawText(formattedText, new Point(midPoint.X - formattedText.Width / 2, midPoint.Y));

                // Draw grid coordinate labels at start and end
                string startCoord = GridVisualHost.GetCoordinateString((int)_measureStart.Value.X, (int)_measureStart.Value.Y);
                string endCoord = GridVisualHost.GetCoordinateString((int)_measureEnd.X, (int)_measureEnd.Y);

                DrawCoordinateLabel(dc, startCoord, startPx, -25);
                DrawCoordinateLabel(dc, endCoord, endPx, 15);
            }
        }

        private void DrawCoordinateLabel(DrawingContext dc, string text, Point position, double yOffset)
        {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                11,
                Brushes.White,
                1.0);

            var labelPos = new Point(position.X - formattedText.Width / 2, position.Y + yOffset);
            var bgRect = new Rect(labelPos.X - 4, labelPos.Y - 2, formattedText.Width + 8, formattedText.Height + 4);

            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromArgb(200, 50, 50, 50)),
                null,
                bgRect, 4, 4);

            dc.DrawText(formattedText, labelPos);
        }

        #endregion

        #region Movement Overlay

        public void RedrawMovementOverlay()
        {
            using (var dc = _movementVisual.RenderOpen())
            {
                if (SelectedToken == null) return;

                int startX = SelectedToken.GridX;
                int startY = SelectedToken.GridY;
                int maxSquares = SelectedToken.SpeedSquares;

                // Check if movement is blocked by walls
                Func<int, int, bool> isBlocked = (gx, gy) =>
                {
                    // Check if any wall blocks movement into this cell
                    var cellCenter = new Point(gx + 0.5, gy + 0.5);

                    foreach (var wall in _wallService.Walls)
                    {
                        if (!wall.BlocksMovement) continue;

                        // Simple check: is the cell center very close to the wall?
                        if (wall.IsPointNear(cellCenter, 0.6))
                            return true;
                    }
                    return false;
                };

                var reachable = MovementService.GetReachableSquares(
                    startX, startY, maxSquares, _gridWidth, _gridHeight, isBlocked);

                var brush = new SolidColorBrush(Color.FromArgb(80, 30, 144, 255));
                brush.Freeze();

                var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 30, 144, 255)), 1);
                borderPen.Freeze();

                foreach (var cell in reachable)
                {
                    var rect = new Rect(cell.x * GridCellSize, cell.y * GridCellSize, GridCellSize, GridCellSize);
                    dc.DrawRectangle(brush, borderPen, rect);
                }
            }
        }

        #endregion

        #region Path Preview

        private void ComputeAndDrawPathPreview((int x, int y) targetCell)
        {
            if (SelectedToken == null) return;

            var start = (SelectedToken.GridX, SelectedToken.GridY);
            var goal = targetCell;

            Func<int, int, bool> isWalkable = (gx, gy) =>
            {
                var cellCenter = new Point(gx + 0.5, gy + 0.5);

                // Check walls
                foreach (var wall in _wallService.Walls)
                {
                    if (!wall.BlocksMovement) continue;
                    if (wall.IsPointNear(cellCenter, 0.6))
                        return false;
                }
                return true;
            };

            var path = MovementService.FindPathAStar(start, goal, _gridWidth, _gridHeight, isWalkable);
            _lastPreviewPath = path;

            var enemies = new List<(int x, int y)>();
            if (Tokens != null)
            {
                foreach (var t in Tokens)
                {
                    if (t.Id == SelectedToken.Id) continue;
                    if (t.IsPlayer == SelectedToken.IsPlayer) continue;
                    enemies.Add((t.GridX, t.GridY));
                }
            }

            _lastAooIndices = MovementService.ComputeAOOIndices(path, enemies);
            RedrawPathVisual();
        }

        private void RedrawPathVisual()
        {
            using (var dc = _pathVisual.RenderOpen())
            {
                if (_lastPreviewPath == null || _lastPreviewPath.Count == 0) return;

                var pen = new Pen(Brushes.LightBlue, 2); pen.Freeze();
                var stepBrush = new SolidColorBrush(Color.FromArgb(200, 100, 180, 255)); stepBrush.Freeze();
                var aooBrush = new SolidColorBrush(Color.FromArgb(220, 220, 50, 50)); aooBrush.Freeze();

                for (int i = 0; i < _lastPreviewPath.Count; i++)
                {
                    var s = _lastPreviewPath[i];
                    var rect = new Rect(s.x * GridCellSize, s.y * GridCellSize, GridCellSize, GridCellSize);
                    dc.DrawRectangle(stepBrush, null, rect);

                    if (_lastAooIndices != null && _lastAooIndices.Contains(i))
                    {
                        var cx = rect.Left + rect.Width / 2;
                        var cy = rect.Top + rect.Height / 2;
                        dc.DrawEllipse(null, new Pen(aooBrush, 3), new Point(cx, cy), rect.Width * 0.35, rect.Height * 0.35);
                    }
                }

                var pts = _lastPreviewPath.Select(p => new Point(p.x * GridCellSize + GridCellSize / 2.0, p.y * GridCellSize + GridCellSize / 2.0)).ToArray();
                if (pts.Length >= 2)
                {
                    var pg = new PathGeometry();
                    var pf = new PathFigure { StartPoint = pts[0], IsClosed = false };
                    for (int i = 1; i < pts.Length; i++) pf.Segments.Add(new LineSegment(pts[i], true));
                    pg.Figures.Add(pf);
                    dc.DrawGeometry(null, pen, pg);
                }

                // Phase 5: AOO enemy warnings
                if (Options.EnableAOODetection && SelectedToken != null && Tokens != null)
                {
                    var enemies = Tokens
                        .Where(t => t.Id != SelectedToken.Id && t.IsPlayer != SelectedToken.IsPlayer && t.HP > 0)
                        .Select(t => (t.GridX, t.GridY));
                    var aooEnemies = MovementService.DetectAOOEnemies(_lastPreviewPath, enemies);
                    DrawAOOWarnings(dc, aooEnemies);
                }
            }
        }

        private void ClearPathVisual()
        {
            _lastPreviewPath = null;
            _lastAooIndices = null;
            using (var dc = _pathVisual.RenderOpen()) { }
        }

        #endregion

        #region Coordinate Rulers

        /// <summary>
        /// Draws coordinate rulers along the top and left edges of the screen
        /// </summary>
        private void DrawCoordinateRulers()
        {
            RulerCanvas.Children.Clear();

            if (!_showCoordinateRulers) return;
            if (ActualWidth <= 0 || ActualHeight <= 0) return;

            var rulerHeight = 20.0;
            var rulerWidth = 25.0;
            var bgColor = Color.FromRgb(37, 37, 38);
            var textColor = Color.FromRgb(200, 200, 200);
            var lineColor = Color.FromRgb(80, 80, 80);

            // Top ruler background
            var topRulerBg = new System.Windows.Shapes.Rectangle
            {
                Width = ActualWidth,
                Height = rulerHeight,
                Fill = new SolidColorBrush(bgColor)
            };
            Canvas.SetLeft(topRulerBg, 0);
            Canvas.SetTop(topRulerBg, 0);
            RulerCanvas.Children.Add(topRulerBg);

            // Left ruler background
            var leftRulerBg = new System.Windows.Shapes.Rectangle
            {
                Width = rulerWidth,
                Height = ActualHeight,
                Fill = new SolidColorBrush(bgColor)
            };
            Canvas.SetLeft(leftRulerBg, 0);
            Canvas.SetTop(leftRulerBg, 0);
            RulerCanvas.Children.Add(leftRulerBg);

            // Calculate visible grid range
            var topLeft = ScreenToWorld(new Point(rulerWidth, rulerHeight));
            var bottomRight = ScreenToWorld(new Point(ActualWidth, ActualHeight));

            int startCol = Math.Max(_gridMinX, (int)Math.Floor(topLeft.X / GridCellSize));
            int endCol = Math.Min(_gridMaxX, (int)Math.Ceiling(bottomRight.X / GridCellSize));
            int startRow = Math.Max(_gridMinY, (int)Math.Floor(topLeft.Y / GridCellSize));
            int endRow = Math.Min(_gridMaxY, (int)Math.Ceiling(bottomRight.Y / GridCellSize));

            // Draw column labels (A, B, C, ..., AA, AB, ...)
            for (int col = startCol; col <= endCol; col++)
            {
                if (col < _gridMinX) continue;

                var worldX = col * GridCellSize;
                var screenX = WorldToScreen(new Point(worldX, 0)).X;

                // Only draw if visible
                if (screenX < rulerWidth || screenX > ActualWidth) continue;

                // Draw tick mark
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = screenX,
                    Y1 = rulerHeight - 5,
                    X2 = screenX,
                    Y2 = rulerHeight,
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 1
                };
                RulerCanvas.Children.Add(tick);

                // Draw label
                string label = GetColumnLabel(col);
                var text = new TextBlock
                {
                    Text = label,
                    Foreground = new SolidColorBrush(textColor),
                    FontSize = 10,
                    FontFamily = new FontFamily("Segoe UI")
                };
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(text, screenX + (GridCellSize * _zoom.ScaleX / 2) - text.DesiredSize.Width / 2);
                Canvas.SetTop(text, 2);
                RulerCanvas.Children.Add(text);
            }

            // Draw row labels (1, 2, 3, ...)
            for (int row = startRow; row <= endRow; row++)
            {
                if (row < _gridMinY) continue;

                var worldY = row * GridCellSize;
                var screenY = WorldToScreen(new Point(0, worldY)).Y;

                // Only draw if visible
                if (screenY < rulerHeight || screenY > ActualHeight) continue;

                // Draw tick mark
                var tick = new System.Windows.Shapes.Line
                {
                    X1 = rulerWidth - 5,
                    Y1 = screenY,
                    X2 = rulerWidth,
                    Y2 = screenY,
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 1
                };
                RulerCanvas.Children.Add(tick);

                // Draw label
                string label = (row + 1).ToString(); // 1-based row numbers
                var text = new TextBlock
                {
                    Text = label,
                    Foreground = new SolidColorBrush(textColor),
                    FontSize = 10,
                    FontFamily = new FontFamily("Segoe UI")
                };
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(text, rulerWidth - text.DesiredSize.Width - 3);
                Canvas.SetTop(text, screenY + (GridCellSize * _zoom.ScaleY / 2) - text.DesiredSize.Height / 2);
                RulerCanvas.Children.Add(text);
            }
        }

        #endregion
    }
}
