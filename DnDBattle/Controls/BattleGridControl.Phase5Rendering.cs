using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
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
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Phase 5 rendering: Auras, Elevation badges, Facing arrows, Movement cost preview, AOO warnings.
    /// </summary>
    public partial class BattleGridControl
    {
        // ── Phase 5 DrawingVisuals ──
        private readonly DrawingVisual _auraVisual = new DrawingVisual();
        private readonly DrawingVisual _movementCostVisual = new DrawingVisual();

        // ── Phase 5 State ──
        private (int x, int y)? _hoverCell = null;

        /// <summary>
        /// Call this once during initialization to add Phase 5 visual overlays.
        /// </summary>
        // VISUAL REFRESH - AURAS
        public void InitializePhase5Visuals()
        {
            AddVisualOverlay(_auraVisual, 55);        // Below movement overlay (60)
            AddVisualOverlay(_movementCostVisual, 62); // Between movement and path
        }

        #region Token Aura Rendering

        /// <summary>
        /// Redraws all token auras on the map.
        /// </summary>
        // VISUAL REFRESH - AURAS
        public void RedrawAuras()
        {
            using var dc = _auraVisual.RenderOpen();
            if (!Options.EnableTokenAuras || Tokens == null) return;

            foreach (var token in Tokens)
            {
                if (token.Auras == null) continue;
                foreach (var aura in token.Auras)
                {
                    if (!aura.IsVisible) continue;
                    RenderAura(dc, token, aura);
                }
            }
        }

        // VISUAL REFRESH - AURAS
        private void RenderAura(DrawingContext dc, Token token, TokenAura aura)
        {
            var center = new Point(
                token.GridX * GridCellSize + GridCellSize / 2.0,
                token.GridY * GridCellSize + GridCellSize / 2.0);

            double radius = aura.RadiusSquares * GridCellSize;

            // Radial gradient fill
            var gradient = new RadialGradientBrush();
            gradient.GradientStops.Add(new GradientStop(
                Color.FromArgb((byte)(aura.Opacity * 255), aura.Color.R, aura.Color.G, aura.Color.B), 0.0));
            gradient.GradientStops.Add(new GradientStop(
                Color.FromArgb((byte)(aura.Opacity * 80), aura.Color.R, aura.Color.G, aura.Color.B), 0.7));
            gradient.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
            gradient.Freeze();

            dc.DrawEllipse(gradient, null, center, radius, radius);

            // Dashed border
            var pen = new Pen(new SolidColorBrush(
                Color.FromArgb((byte)(aura.Opacity * 200), aura.Color.R, aura.Color.G, aura.Color.B)), 2);
            pen.DashStyle = DashStyles.Dash;
            pen.Freeze();
            dc.DrawEllipse(null, pen, center, radius, radius);

            // Label
            var labelText = new FormattedText(
                aura.Name,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                new SolidColorBrush(Color.FromArgb(180, aura.Color.R, aura.Color.G, aura.Color.B)),
                1.0);
            dc.DrawText(labelText, new Point(center.X - labelText.Width / 2, center.Y - radius - 14));
        }

        #endregion

        #region Elevation Badge Rendering

        /// <summary>
        /// Draws an elevation badge on a token container during RebuildTokenVisuals.
        /// Call this when building token visuals if EnableTokenElevation is true.
        /// </summary>
        // VISUAL REFRESH - TOKEN_RENDERING
        public FrameworkElement? CreateElevationBadge(Token token)
        {
            if (!Options.EnableTokenElevation || token.Elevation == 0) return null;

            var badge = new Border
            {
                Width = 28,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1.5),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 2, 0),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = $"{token.Elevation}ft",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            return badge;
        }

        #endregion

        #region Facing Arrow Rendering

        /// <summary>
        /// Creates a facing direction arrow overlay for a token.
        /// Call this during RebuildTokenVisuals if EnableTokenFacing is true.
        /// </summary>
        // VISUAL REFRESH - TOKEN_RENDERING
        public FrameworkElement? CreateFacingArrow(Token token)
        {
            if (!Options.EnableTokenFacing) return null;

            double size = GridCellSize * token.SizeInSquares;
            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            double centerX = size / 2;
            double centerY = size / 2;
            double angleRad = token.FacingAngle * Math.PI / 180.0;
            double arrowLength = size * 0.4;

            double tipX = centerX + Math.Cos(angleRad) * arrowLength;
            double tipY = centerY + Math.Sin(angleRad) * arrowLength;

            var line = new System.Windows.Shapes.Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = tipX,
                Y2 = tipY,
                Stroke = Brushes.Yellow,
                StrokeThickness = 3,
                StrokeEndLineCap = PenLineCap.Triangle,
                Opacity = 0.8
            };

            // Arrowhead
            double headLen = 8;
            double headAngle1 = angleRad + Math.PI * 0.8;
            double headAngle2 = angleRad - Math.PI * 0.8;
            var head1 = new System.Windows.Shapes.Line
            {
                X1 = tipX, Y1 = tipY,
                X2 = tipX + Math.Cos(headAngle1) * headLen,
                Y2 = tipY + Math.Sin(headAngle1) * headLen,
                Stroke = Brushes.Yellow, StrokeThickness = 2.5, Opacity = 0.8
            };
            var head2 = new System.Windows.Shapes.Line
            {
                X1 = tipX, Y1 = tipY,
                X2 = tipX + Math.Cos(headAngle2) * headLen,
                Y2 = tipY + Math.Sin(headAngle2) * headLen,
                Stroke = Brushes.Yellow, StrokeThickness = 2.5, Opacity = 0.8
            };

            canvas.Children.Add(line);
            canvas.Children.Add(head1);
            canvas.Children.Add(head2);
            return canvas;
        }

        #endregion

        #region Movement Cost Preview

        /// <summary>
        /// Updates the movement cost preview at the hovered cell.
        /// </summary>
        // VISUAL REFRESH - MOVEMENT
        public void UpdateMovementCostPreview(int cellX, int cellY)
        {
            if (!Options.EnableMovementCostPreview)
            {
                _hoverCell = null;
                ClearMovementCostPreview();
                return;
            }

            _hoverCell = (cellX, cellY);
            RedrawMovementCostPreview();
        }

        // VISUAL REFRESH - MOVEMENT
        public void ClearMovementCostPreview()
        {
            _hoverCell = null;
            using var dc = _movementCostVisual.RenderOpen();
        }

        // VISUAL REFRESH - MOVEMENT
        private void RedrawMovementCostPreview()
        {
            using var dc = _movementCostVisual.RenderOpen();
            if (!Options.EnableMovementCostPreview || SelectedToken == null || !_hoverCell.HasValue) return;

            var token = SelectedToken;
            var (hx, hy) = _hoverCell.Value;
            if (hx == token.GridX && hy == token.GridY) return;

            Func<int, int, bool> isWalkable = (gx, gy) =>
            {
                var cellCenter = new Point(gx + 0.5, gy + 0.5);
                foreach (var wall in _wallService.Walls)
                {
                    if (!wall.BlocksMovement) continue;
                    if (wall.IsPointNear(cellCenter, 0.6)) return false;
                }
                return true;
            };

            var path = MovementService.FindPathAStar(
                (token.GridX, token.GridY), (hx, hy),
                _gridWidth, _gridHeight, isWalkable);

            if (path == null || path.Count == 0) return;

            // Calculate total cost (each step is 1 or 1.5 for diagonal)
            double totalCost = 0;
            for (int i = 1; i < path.Count; i++)
            {
                int dx = Math.Abs(path[i].x - path[i - 1].x);
                int dy = Math.Abs(path[i].y - path[i - 1].y);
                totalCost += (dx + dy > 1) ? 1.5 : 1.0;
            }

            double remaining = token.SpeedSquares - totalCost;

            // Color based on remaining movement
            Color indicatorColor;
            if (remaining >= 0)
                indicatorColor = Color.FromRgb(76, 175, 80); // Green - can reach
            else if (remaining >= -token.SpeedSquares * 0.5)
                indicatorColor = Color.FromRgb(255, 193, 7); // Yellow - uses dash
            else
                indicatorColor = Color.FromRgb(244, 67, 54); // Red - too far

            // Highlight cell
            var rect = new Rect(hx * GridCellSize, hy * GridCellSize, GridCellSize, GridCellSize);
            var brush = new SolidColorBrush(Color.FromArgb(80,
                indicatorColor.R, indicatorColor.G, indicatorColor.B));
            brush.Freeze();
            dc.DrawRectangle(brush, null, rect);

            // Cost text
            string costText = $"{totalCost:F1}/{token.SpeedSquares}";
            var text = new FormattedText(
                costText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                11,
                new SolidColorBrush(indicatorColor),
                1.0);

            // Background for text readability
            var textRect = new Rect(
                hx * GridCellSize + GridCellSize / 2 - text.Width / 2 - 3,
                hy * GridCellSize + GridCellSize / 2 - text.Height / 2 - 2,
                text.Width + 6, text.Height + 4);
            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromArgb(180, 20, 20, 20)), null, textRect, 3, 3);

            dc.DrawText(text, new Point(
                hx * GridCellSize + GridCellSize / 2 - text.Width / 2,
                hy * GridCellSize + GridCellSize / 2 - text.Height / 2));
        }

        #endregion

        #region AOO Warning Rendering

        /// <summary>
        /// Draws AOO warning indicators around enemies that would get AOO during path preview.
        /// This is called during RedrawPathVisual when AOO detection is enabled.
        /// </summary>
        // VISUAL REFRESH - MOVEMENT
        public void DrawAOOWarnings(DrawingContext dc, List<(int x, int y)> aooEnemyPositions)
        {
            if (!Options.EnableAOODetection || aooEnemyPositions == null) return;

            foreach (var enemy in aooEnemyPositions)
            {
                var center = new Point(
                    enemy.x * GridCellSize + GridCellSize / 2.0,
                    enemy.y * GridCellSize + GridCellSize / 2.0);

                // Red warning circle
                var brush = new SolidColorBrush(Color.FromArgb(100, 244, 67, 54));
                brush.Freeze();
                var pen = new Pen(Brushes.Red, 3);
                pen.Freeze();
                dc.DrawEllipse(brush, pen, center, GridCellSize * 0.6, GridCellSize * 0.6);

                // Warning text
                var warningText = new FormattedText(
                    "AOO",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    10,
                    Brushes.Red,
                    1.0);
                dc.DrawText(warningText, new Point(
                    center.X - warningText.Width / 2,
                    center.Y - GridCellSize * 0.6 - 14));
            }
        }

        #endregion

        #region Flanking Indicator

        /// <summary>
        /// Checks and shows flanking status for the selected token attacking a target.
        /// </summary>
        public bool CheckFlanking(Token attacker, Token target)
        {
            if (!Options.EnableFlankingDetection || Tokens == null) return false;

            var allyPositions = Tokens
                .Where(t => t.Id != attacker.Id && t.IsPlayer == attacker.IsPlayer && t.HP > 0)
                .Where(t => MovementService.IsAdjacent((t.GridX, t.GridY), (target.GridX, target.GridY)))
                .Select(t => (t.GridX, t.GridY));

            return MovementService.IsFlanking(
                (attacker.GridX, attacker.GridY),
                (target.GridX, target.GridY),
                allyPositions);
        }

        #endregion
    }
}
