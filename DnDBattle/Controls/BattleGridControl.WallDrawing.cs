using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing wall drawing and management functionality
    /// </summary>
    public partial class BattleGridControl
    {
        #region Wall Draw Mode

        public void SetWallDrawMode(bool enabled, WallType wallType = WallType.Solid)
        {
            _wallDrawMode = enabled;
            _currentWallType = wallType;
            _wallDrawStart = null;
            _selectedWall = null;

            RenderCanvas.Cursor = enabled ? Cursors.Cross : Cursors.Arrow;
            RedrawWalls();
        }

        public void SetRoomDrawMode(bool enabled, WallType wallType = WallType.Solid)
        {
            _roomDrawMode = enabled;
            _currentWallType = wallType;
            _roomVertices.Clear();
            _wallDrawMode = false;
            _wallDrawStart = null;

            RenderCanvas.Cursor = enabled ? Cursors.Cross : Cursors.Arrow;
            RedrawWalls();
        }

        public bool IsRoomDrawMode => _roomDrawMode;

        public void AddWall(Wall wall) =>
            _wallService.AddWall(wall);

        public void RemoveWall(Wall wall) =>
            _wallService.RemoveWall(wall);

        #endregion

        #region Wall Rendering

        public void RedrawWalls()
        {
            using (var dc = _wallVisual.RenderOpen())
            {
                var solidPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 139, 90, 43)), 6);
                var doorPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 101, 67, 33)), 6);
                var doorOpenPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 101, 67, 33)), 4);
                var windowPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 135, 206, 235)), 4);
                var halfWallPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 169, 169, 169)), 4);
                var selectedPen = new Pen(Brushes.Yellow, 3);

                solidPen.DashStyle = DashStyles.Solid;
                doorPen.DashStyle = DashStyles.Solid;
                doorOpenPen.DashStyle = DashStyles.Dash;
                windowPen.DashStyle = DashStyles.DashDot;
                halfWallPen.DashStyle = DashStyles.Dot;

                solidPen.Freeze();
                doorPen.Freeze();
                doorOpenPen.Freeze();
                windowPen.Freeze();
                halfWallPen.Freeze();
                selectedPen.Freeze();

                foreach (var wall in _wallService.Walls)
                {
                    var startPx = new Point(
                        wall.StartPoint.X * GridCellSize + GridCellSize / 2,
                        wall.StartPoint.Y * GridCellSize + GridCellSize / 2);
                    var endPx = new Point(
                        wall.EndPoint.X * GridCellSize + GridCellSize / 2,
                        wall.EndPoint.Y * GridCellSize + GridCellSize / 2);

                    Pen wallPen = wall.WallType switch
                    {
                        WallType.Solid => solidPen,
                        WallType.Door => wall.IsOpen ? doorOpenPen : doorPen,
                        WallType.Window => windowPen,
                        WallType.Halfwall => halfWallPen,
                        _ => solidPen
                    };

                    var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), wallPen.Thickness);
                    shadowPen.Freeze();
                    dc.DrawLine(shadowPen,
                        new Point(startPx.X + 2, startPx.Y + 2),
                        new Point(endPx.X + 2, endPx.Y + 2));

                    dc.DrawLine(wallPen, startPx, endPx);

                    if (wall == _selectedWall)
                    {
                        dc.DrawLine(selectedPen, startPx, endPx);

                        var handleBrush = Brushes.Yellow;
                        dc.DrawEllipse(handleBrush, new Pen(Brushes.Black, 2), startPx, 8, 8);
                        dc.DrawEllipse(handleBrush, new Pen(Brushes.Black, 2), endPx, 8, 8);
                    }

                    if (wall == _selectedWall || !string.IsNullOrEmpty(wall.Label))
                    {
                        var midPoint = new Point(
                            (startPx.X + endPx.X) / 2,
                            (startPx.Y + endPx.Y) / 2);

                        // Only show label if wall is selected or has a custom label
                        string labelText = wall.Label ?? $"{wall.WallType}";

                        if (wall == _selectedWall || wall.WallType == WallType.Door)
                        {
                            var labelFormatted = new FormattedText(
                                labelText,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                new Typeface("Segoe UI"),
                                10,
                                Brushes.White,
                                1.0);

                            var labelBg = new Rect(
                                midPoint.X - labelFormatted.Width / 2 - 4,
                                midPoint.Y - labelFormatted.Height / 2 - 15,
                                labelFormatted.Width + 8,
                                labelFormatted.Height + 4);

                            dc.DrawRoundedRectangle(
                                new SolidColorBrush(Color.FromArgb(200, 40, 40, 40)),
                                null, labelBg, 3, 3);

                            dc.DrawText(labelFormatted, new Point(
                                midPoint.X - labelFormatted.Width / 2,
                                midPoint.Y - labelFormatted.Height / 2 - 13));
                        }
                    }

                    if (wall.WallType == WallType.Door)
                    {
                        var midPoint = new Point((startPx.X + endPx.X) / 2, (startPx.Y + endPx.Y) / 2);
                        var doorBrush = wall.IsOpen
                            ? new SolidColorBrush(Color.FromArgb(200, 76, 175, 80)) // Green for open
                            : new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)); // Red for closed
                        doorBrush.Freeze();

                        dc.DrawEllipse(doorBrush, new Pen(Brushes.White, 2), midPoint, 10, 10);

                        var iconText = wall.IsOpen ? "🚪" : "🔒";
                        var formattedText = new FormattedText(
                            iconText,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI Emoji"),
                            14,
                            Brushes.White,
                            1.0);
                        dc.DrawText(formattedText, new Point(midPoint.X - 7, midPoint.Y - 10));
                    }
                }

                // Draw wall preview
                if (_wallDrawMode && _wallDrawStart.HasValue)
                {
                    var previewPen = new Pen(Brushes.LimeGreen, 4);
                    previewPen.DashStyle = DashStyles.Dash;
                    previewPen.Freeze();

                    var startPx = new Point(
                        _wallDrawStart.Value.X * GridCellSize + GridCellSize / 2,
                        _wallDrawStart.Value.Y * GridCellSize + GridCellSize / 2);
                    var endPx = new Point(
                        _wallDrawPreview.X * GridCellSize + GridCellSize / 2,
                        _wallDrawPreview.Y * GridCellSize + GridCellSize / 2);

                    dc.DrawLine(previewPen, startPx, endPx);

                    dc.DrawEllipse(Brushes.LimeGreen, null, startPx, 6, 6);
                    dc.DrawEllipse(Brushes.LimeGreen, null, endPx, 6, 6);

                    var length = Math.Sqrt(
                        Math.Pow(_wallDrawPreview.X - _wallDrawStart.Value.X, 2) +
                        Math.Pow(_wallDrawPreview.Y - _wallDrawStart.Value.Y, 2));

                    var midPoint = new Point((startPx.X + endPx.X) / 2, (startPx.Y + endPx.Y) / 2 - 20);
                    var lengthText = new FormattedText(
                        $"{length:F1} squares",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        12,
                        Brushes.White,
                        1.0);

                    var textBounds = new Rect(midPoint.X - 5, midPoint.Y - 2, lengthText.Width + 10, lengthText.Height + 4);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)), null, textBounds);
                    dc.DrawText(lengthText, midPoint);
                }

                // Draw wall mode indicator
                if (_wallDrawMode)
                {
                    var indicatorText = new FormattedText(
                        $"Wall Mode: {_currentWallType}\nClick to start, click again to place\nRight-click to cancel",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        12,
                        Brushes.White,
                        1.0);

                    var bgRect = new Rect(10, 10, indicatorText.Width + 20, indicatorText.Height + 10);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)), null, bgRect);
                    dc.DrawText(indicatorText, new Point(20, 15));
                }

                // Draw room preview
                if (_roomDrawMode && _roomVertices.Count > 0)
                {
                    var roomPen = new Pen(Brushes.Cyan, 3);
                    roomPen.DashStyle = DashStyles.Dash;
                    roomPen.Freeze();

                    // Draw existing vertices and lines
                    for (int i = 0; i < _roomVertices.Count; i++)
                    {
                        var vertexPx = new Point(
                            _roomVertices[i].X * GridCellSize + GridCellSize / 2,
                            _roomVertices[i].Y * GridCellSize + GridCellSize / 2);

                        // Draw vertex marker
                        dc.DrawEllipse(Brushes.Cyan, new Pen(Brushes.White, 2), vertexPx, 8, 8);

                        // Draw vertex number
                        var numText = new FormattedText(
                            (i + 1).ToString(),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            10,
                            Brushes.Black,
                            1.0);
                        dc.DrawText(numText, new Point(vertexPx.X - 4, vertexPx.Y - 6));

                        // Draw line to previous vertex
                        if (i > 0)
                        {
                            var prevPx = new Point(
                                _roomVertices[i - 1].X * GridCellSize + GridCellSize / 2,
                                _roomVertices[i - 1].Y * GridCellSize + GridCellSize / 2);
                            dc.DrawLine(roomPen, prevPx, vertexPx);
                        }
                    }

                    // Draw closing line preview (from last vertex back to first)
                    if (_roomVertices.Count >= 3)
                    {
                        var firstPx = new Point(
                            _roomVertices[0].X * GridCellSize + GridCellSize / 2,
                            _roomVertices[0].Y * GridCellSize + GridCellSize / 2);
                        var lastPx = new Point(
                            _roomVertices[^1].X * GridCellSize + GridCellSize / 2,
                            _roomVertices[^1].Y * GridCellSize + GridCellSize / 2);

                        var closingPen = new Pen(Brushes.Cyan, 2);
                        closingPen.DashStyle = DashStyles.Dot;
                        closingPen.Freeze();
                        dc.DrawLine(closingPen, lastPx, firstPx);
                    }

                    // Show instructions
                    var instructionText = _roomVertices.Count < 3
                        ? $"Click to add corners ({_roomVertices.Count}/3 min)\nDouble-click to finish"
                        : "Double-click to finish room\nRight-click to cancel";

                    var instructions = new FormattedText(
                        instructionText,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        12,
                        Brushes.White,
                        1.0);

                    var instrBg = new Rect(10, 10, instructions.Width + 20, instructions.Height + 10);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)), null, instrBg);
                    dc.DrawText(instructions, new Point(20, 15));
                }
            }
        }

        #endregion

        #region Wall Drawing Click Handlers

        private void HandleWallDrawClick(Point gridPoint, MouseButtonEventArgs e)
        {
            if (!_wallDrawMode) return;

            if (e.ChangedButton == MouseButton.Right)
            {
                _wallDrawStart = null;
                RedrawWalls();
                return;
            }

            if (_wallDrawStart == null)
            {
                _wallDrawStart = gridPoint;
                _wallDrawPreview = gridPoint;
            }
            else
            {
                if (_wallDrawStart.Value != gridPoint)
                {
                    var wall = new Wall()
                    {
                        StartPoint = _wallDrawStart.Value,
                        EndPoint = gridPoint,
                        WallType = _currentWallType,
                        Label = $"{_currentWallType} Wall"
                    };

                    _wallService.AddWall(wall);
                    AddToActionLog("Wall", $"Added {_currentWallType} wall from ({wall.StartPoint.X:F0},{wall.StartPoint.Y:F0}) to ({wall.EndPoint.X:F0},{wall.EndPoint.Y:F0})");
                }

                _wallDrawStart = null;
            }

            RedrawWalls();
            RedrawLighting();
        }

        private void HandleRoomDrawClick(Point gridPoint, MouseButtonEventArgs e)
        {
            if (!_roomDrawMode) return;

            if (e.ClickCount >= 2 && _roomVertices.Count >= 3)
            {
                FinishRoomDrawing();
                e.Handled = true;
                return;
            }

            _roomVertices.Add(gridPoint);
            RedrawWalls();
            e.Handled = true;
        }

        private void HandleWallSelection(Point gridPoint, bool isRightClick = false)
        {
            // Check for endpoint hit first (for dragging)
            var endPointWall = _wallService.HitTestEndPoint(gridPoint, out bool isStart, 0.5);
            if (endPointWall != null && !isRightClick)
            {
                _selectedWall = endPointWall;
                _isDraggingWallEndpoint = true;
                _draggingWallIsStart = isStart;
                RedrawWalls();
                return;
            }

            // Check for wall body hit
            var hitWall = _wallService.HitTest(gridPoint, 0.5);
            if (hitWall != null)
            {
                _selectedWall = hitWall;

                if (isRightClick)
                {
                    // Show context menu
                    var menu = CreateWallContextMenu(hitWall);
                    menu.IsOpen = true;
                }
                else if (hitWall.WallType == WallType.Door)
                {
                    // Left-click toggles doors
                    hitWall.IsOpen = !hitWall.IsOpen;
                    AddToActionLog("Door", $"{hitWall.Label} is now {(hitWall.IsOpen ? "OPEN" : "CLOSED")}");
                    RedrawLighting();
                }

                RedrawWalls();
                return;
            }

            _selectedWall = null;
            RedrawWalls();
        }

        #endregion

        #region Wall Context Menu

        private ContextMenu CreateWallContextMenu(Wall wall)
        {
            var menu = new ContextMenu();

            // Wall type submenu
            var typeMenu = new MenuItem { Header = "🔄 Change Type" };

            foreach (WallType wallType in Enum.GetValues(typeof(WallType)))
            {
                var typeItem = new MenuItem
                {
                    Header = wallType.ToString(),
                    IsChecked = wall.WallType == wallType,
                    Tag = wallType
                };
                typeItem.Click += (s, e) =>
                {
                    wall.WallType = (WallType)((MenuItem)s).Tag;
                    AddToActionLog("Wall", $"Changed wall to {wall.WallType}");
                    RedrawWalls();
                    RedrawLighting();
                };
                typeMenu.Items.Add(typeItem);
            }
            menu.Items.Add(typeMenu);

            // Toggle door state (only for doors)
            if (wall.WallType == WallType.Door)
            {
                var toggleItem = new MenuItem
                {
                    Header = wall.IsOpen ? "🔒 Close Door" : "🚪 Open Door"
                };
                toggleItem.Click += (s, e) =>
                {
                    wall.IsOpen = !wall.IsOpen;
                    AddToActionLog("Door", $"{wall.Label} is now {(wall.IsOpen ? "OPEN" : "CLOSED")}");
                    RedrawWalls();
                    RedrawLighting();
                };
                menu.Items.Add(toggleItem);
            }

            menu.Items.Add(new Separator());

            // Edit label
            var labelItem = new MenuItem { Header = "✏️ Edit Label..." };
            labelItem.Click += (s, e) =>
            {
                string newLabel = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter wall label:", "Edit Wall Label", wall.Label ?? "");
                if (!string.IsNullOrWhiteSpace(newLabel))
                {
                    wall.Label = newLabel;
                    RedrawWalls();
                }
            };
            menu.Items.Add(labelItem);

            menu.Items.Add(new Separator());

            // Delete wall
            var deleteItem = new MenuItem { Header = "🗑️ Delete Wall" };
            deleteItem.Click += (s, e) =>
            {
                _wallService.RemoveWall(wall);
                AddToActionLog("Wall", $"Deleted {wall.Label}");
                _selectedWall = null;
                RedrawWalls();
                RedrawLighting();
            };
            menu.Items.Add(deleteItem);

            return menu;
        }

        #endregion

        #region Room Drawing

        private void FinishRoomDrawing()
        {
            if (_roomVertices.Count < 3) return;

            for (int i = 0; i < _roomVertices.Count; i++)
            {
                var start = _roomVertices[i];
                var end = _roomVertices[(i + 1) % _roomVertices.Count];

                var wall = new Wall()
                {
                    StartPoint = start,
                    EndPoint = end,
                    WallType = _currentWallType,
                    Label = $"Room Wall {i + 1}"
                };
                _wallService.AddWall(wall);
            }

            AddToActionLog("Room", $"Created room with {_roomVertices.Count} walls");

            _roomVertices.Clear();
            _roomDrawMode = false;
            RenderCanvas.Cursor = Cursors.Arrow;
            RedrawWalls();
            RedrawLighting();
        }

        #endregion
    }
}
