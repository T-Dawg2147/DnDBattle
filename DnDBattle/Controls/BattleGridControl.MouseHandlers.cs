using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing mouse event handlers
    /// </summary>
    public partial class BattleGridControl
    {
        #region Token Mouse Event Handlers

        private void Token_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var visual = sender as FrameworkElement;
            if (visual?.Tag is Token clickedToken)
            {
                // Check if we're in targeting mode
                if (_isInTargetingMode)
                {
                    // Fire the target selected event
                    TargetSelected?.Invoke(clickedToken);
                    e.Handled = true;
                    return;
                }

                // Normal selection/drag behavior
                _draggingVisual = visual;
                _dragStartGridX = clickedToken.GridX;
                _dragStartGridY = clickedToken.GridY;

                // Set the selected token
                SelectedToken = clickedToken;

                _isDraggingToken = true;
                _dragOrigin = e.GetPosition(RenderCanvas);
                _draggingVisual?.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Token_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingToken && _draggingVisual != null)
            {
                var pos = e.GetPosition(RenderCanvas);
                var dx = pos.X - _dragOrigin.X;
                var dy = pos.Y - _dragOrigin.Y;
                var left = Canvas.GetLeft(_draggingVisual) + dx;
                var top = Canvas.GetTop(_draggingVisual) + dy;
                Canvas.SetLeft(_draggingVisual, left);
                Canvas.SetTop(_draggingVisual, top);
                _dragOrigin = pos;
            }
        }

        private void Token_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingToken && _draggingVisual != null)
            {
                _draggingVisual.ReleaseMouseCapture();
                var left = Canvas.GetLeft(_draggingVisual);
                var top = Canvas.GetTop(_draggingVisual);
                int newGridX, newGridY;

                if (LockToGrid)
                {
                    newGridX = (int)Math.Round(left / GridCellSize);
                    newGridY = (int)Math.Round(top / GridCellSize);
                }
                else
                {
                    newGridX = (int)Math.Floor(left / GridCellSize);
                    newGridY = (int)Math.Floor(top / GridCellSize);
                }

                if (_draggingVisual.Tag is Token token)
                {
                    int oldX = _dragStartGridX;
                    int oldY = _dragStartGridY;

                    // Calculate Manhattan distance moved
                    int distanceMoved = Math.Abs(newGridX - oldX) + Math.Abs(newGridY - oldY);

                    // Check if we're in combat and it's this token's turn
                    bool isInCombat = false;
                    bool isTokensTurn = false;
                    MainViewModel vm = null;

                    if (Application.Current?.MainWindow?.DataContext is MainViewModel mainVm)
                    {
                        vm = mainVm;
                        isInCombat = vm.IsInCombat;
                        isTokensTurn = token.IsCurrentTurn;
                    }

                    // Enforce movement limits during combat on the token's turn
                    if (isInCombat && isTokensTurn && distanceMoved > 0)
                    {
                        if (distanceMoved > token.MovementRemainingThisTurn)
                        {
                            // Can't move that far - snap back! 
                            Canvas.SetLeft(_draggingVisual, oldX * GridCellSize);
                            Canvas.SetTop(_draggingVisual, oldY * GridCellSize);

                            MessageBox.Show(
                                $"{token.Name} can only move {token.MovementRemainingThisTurn} more squares this turn!\n\n" +
                                $"Speed: {token.SpeedSquares} squares\n" +
                                $"Already moved: {token.MovementUsedThisTurn} squares\n" +
                                $"Remaining: {token.MovementRemainingThisTurn} squares\n" +
                                $"Attempted: {distanceMoved} squares",
                                "Movement Limit Exceeded",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            _isDraggingToken = false;
                            _draggingVisual = null;
                            e.Handled = true;
                            return;
                        }

                        // Use the movement
                        token.MovementUsedThisTurn += distanceMoved;

                        // Update fog when player tokens move
                        if (token.IsPlayer && _fogOfWar.IsEnabled)
                        {
                            UpdateFogVisibility();
                        }

                        // Log the movement
                        AddToActionLog("Movement", $"{token.Name} moved {distanceMoved} squares ({token.MovementRemainingThisTurn} remaining)");
                    }

                    // Snap to grid if enabled
                    if (LockToGrid)
                    {
                        Canvas.SetLeft(_draggingVisual, newGridX * GridCellSize);
                        Canvas.SetTop(_draggingVisual, newGridY * GridCellSize);
                    }

                    // Update token position
                    token.GridX = newGridX;
                    token.GridY = newGridY;

                    // Check for tile metadata at new position
                    if (newGridX != oldX || newGridY != oldY)
                    {
                        CheckForAllMetadataAtPosition(token, newGridX, newGridY);
                    }

                    // Fog of war 
                    if (_loadedTileMap != null && newGridX != oldX || newGridY != oldY)
                    {
                        if (token.IsPlayer)
                        {
                            // TODO: Implement fog reveal
                        }
                    }

                    // Record undo action if position changed
                    if (newGridX != oldX || newGridY != oldY)
                    {
                        if (vm != null)
                        {
                            var act = new TokenMoveAction(vm, token, oldX, oldY, newGridX, newGridY);
                            UndoManager.Record(act, performNow: false);
                        }
                    }
                }

                _isDraggingToken = false;
                _draggingVisual = null;
                e.Handled = true;

                RedrawMovementOverlay();
                ClearPathVisual();
            }
        }

        #endregion

        #region Grid Background Mouse Event Handlers

        private void GridBackground_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();

            var pos = e.GetPosition(RenderCanvas);
            var worldPt = ScreenToWorld(pos);
            var gridPoint = new Point(worldPt.X / GridCellSize, worldPt.Y / GridCellSize);

            // Area Effect Placement
            if (_isPlacingAreaEffect && _previewEffect != null)
            {
                if (_previewEffect.Shape == AreaEffectShape.Cone || _previewEffect.Shape == AreaEffectShape.Line)
                {
                    if (_previewEffect.Origin == default)
                    {
                        _previewEffect.Origin = gridPoint;
                        RedrawAreaEffects();
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        _previewEffect.DirectionAngle = CalculateAngle(_previewEffect.Origin, gridPoint);
                        PlaceAreaEffect();
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    _previewEffect.Origin = gridPoint;
                    PlaceAreaEffect();
                    e.Handled = true;
                    return;
                }
            }

            // Measurement mode
            if (_measureMode)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));

                _measureStart = gridPoint;
                _measureEnd = gridPoint;
                RedrawMeasureVisual();
                e.Handled = true;
                return;
            }

            // Wall drawing mode
            if (_wallDrawMode)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));

                HandleWallDrawClick(gridPoint, e);
                e.Handled = true;
                return;
            }

            // Room drawing mode
            if (_roomDrawMode)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                HandleRoomDrawClick(gridPoint, e);
                e.Handled = true;
                return;
            }

            // Check for wall selection/interaction
            HandleWallSelection(gridPoint);
            if (_selectedWall != null)
            {
                e.Handled = true;
                return;
            }

            // Ctrl+Click for path preview
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (SelectedToken != null)
                {
                    var targetCell = (x: (int)Math.Floor(gridPoint.X), y: (int)Math.Floor(gridPoint.Y));
                    ComputeAndDrawPathPreview(targetCell);
                }
                e.Handled = true;
                return;
            }

            // Start panning (on empty space)
            _isPanning = true;
            _lastPanPoint = e.GetPosition(GridBackground);
            Cursor = Cursors.Hand;
            GridBackground.CaptureMouse();
            e.Handled = true;
        }

        private void GridBackground_MouseLeave(object sender, MouseEventArgs e)
        {
            ClearMovementCostPreview();
        }

        private void GridBackground_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingWallEndpoint)
            {
                _isDraggingWallEndpoint = false;
                AddToActionLog("Wall", "Moved wall endpoint");
                return;
            }

            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Arrow;
                GridBackground.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void GridBackground_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(RenderCanvas);
            var worldPt = ScreenToWorld(pos);
            var gridPoint = new Point(worldPt.X / GridCellSize, worldPt.Y / GridCellSize);

            // Update status bar with current cell
            UpdateCurrentCellDisplay(gridPoint);

            // Phase 5: Movement cost preview
            if (SelectedToken != null && !_isDraggingToken && !_measureMode && !_wallDrawMode)
            {
                int cellX = (int)Math.Floor(worldPt.X / GridCellSize);
                int cellY = (int)Math.Floor(worldPt.Y / GridCellSize);
                UpdateMovementCostPreview(cellX, cellY);
            }

            // AOE preview
            if (_isPlacingAreaEffect && _previewEffect != null)
            {
                if (_previewEffect.Shape == AreaEffectShape.Cone || _previewEffect.Shape == AreaEffectShape.Line)
                {
                    if (_previewEffect.Origin != default)
                    {
                        _previewEffect.DirectionAngle = CalculateAngle(_previewEffect.Origin, gridPoint);
                    }
                    else
                    {
                        _previewEffect.Origin = gridPoint;
                    }
                }
                else
                {
                    _previewEffect.Origin = gridPoint;
                }

                RedrawAreaEffects();
            }

            // Measurement preview
            if (_measureMode && _measureStart.HasValue)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                _measureEnd = gridPoint;
                RedrawMeasureVisual();
            }

            // Wall drawing preview
            if (_wallDrawMode && _wallDrawStart.HasValue)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));
                _wallDrawPreview = gridPoint;
                RedrawWalls();
            }

            // Wall endpoint dragging
            if (_isDraggingWallEndpoint && _selectedWall != null)
            {
                if (LockToGrid)
                    gridPoint = new Point(Math.Round(gridPoint.X), Math.Round(gridPoint.Y));

                if (_draggingWallIsStart)
                    _selectedWall.StartPoint = gridPoint;
                else
                    _selectedWall.EndPoint = gridPoint;

                RedrawWalls();
                RedrawLighting();
                return;
            }

            // Panning
            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                var pt = e.GetPosition(GridBackground);
                var dx = pt.X - _lastPanPoint.X;
                var dy = pt.Y - _lastPanPoint.Y;
                _pan.X += dx;
                _pan.Y += dy;
                _lastPanPoint = pt;

                ClampPanToBoundaries();
                RefreshAllVisuals();
                e.Handled = true;
            }

            // Middle mouse panning
            if (_isMiddlePanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                var pt = e.GetPosition(GridBackground);
                var dx = pt.X - _middlePanLast.X;
                var dy = pt.Y - _middlePanLast.Y;
                _pan.X += dx;
                _pan.Y += dy;
                _middlePanLast = pt;

                ClampPanToBoundaries();
                RefreshAllVisuals();
                e.Handled = true;
            }
        }

        private void GridBackground_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = (e.Delta > 0) ? 1.15 : 1.0 / 1.15;
            var pos = e.GetPosition(RenderCanvas);

            double absX = pos.X * _zoom.ScaleX + _pan.X;
            double absY = pos.Y * _zoom.ScaleY + _pan.Y;

            _zoom.ScaleX *= zoomFactor;
            _zoom.ScaleY *= zoomFactor;

            _zoom.ScaleX = Math.Max(0.2, Math.Min(4.0, _zoom.ScaleX));
            _zoom.ScaleY = Math.Max(0.2, Math.Min(4.0, _zoom.ScaleY));

            _pan.X = absX - pos.X * _zoom.ScaleX;
            _pan.Y = absY - pos.Y * _zoom.ScaleY;

            ClampPanToBoundaries();
            RefreshAllVisuals();
            e.Handled = true;
        }

        private void GridBackground_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(RenderCanvas);
            var worldPt = ScreenToWorld(pos);
            var gridPoint = new Point(worldPt.X / GridCellSize, worldPt.Y / GridCellSize);

            // Cancel AOE placement
            if (_isPlacingAreaEffect)
            {
                CancelAreaEffectPlacement();
                e.Handled = true;
                return;
            }

            // Cancel wall drawing
            if (_wallDrawMode)
            {
                _wallDrawStart = null;
                RedrawWalls();
                e.Handled = true;
                return;
            }

            // Cancel room drawing
            if (_roomDrawMode)
            {
                _roomVertices.Clear();
                _roomDrawMode = false;
                Cursor = Cursors.Arrow;
                RedrawWalls();
                e.Handled = true;
                return;
            }

            // Delete selected wall
            if (_selectedWall != null)
            {
                var result = MessageBox.Show(
                    $"Delete wall '{_selectedWall.Label}'?",
                    "Delete Wall",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _wallService.RemoveWall(_selectedWall);
                    AddToActionLog("Wall", $"Deleted {_selectedWall.Label}");
                    _selectedWall = null;
                    RedrawWalls();
                    RedrawLighting();
                }
                e.Handled = true;
                return;
            }

            // Cancel measurement
            if (_measureMode && _measureStart.HasValue)
            {
                _measureStart = null;
                _measureEnd = new Point();
                RedrawMeasureVisual();
                e.Handled = true;
                return;
            }
        }

        private void GridBackground_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isMiddlePanning = true;
                _middlePanLast = e.GetPosition(GridBackground);
                Cursor = Cursors.Hand;
                GridBackground.CaptureMouse();
                e.Handled = true;
            }
        }

        private void GridBackground_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && _isMiddlePanning)
            {
                _isMiddlePanning = false;
                Cursor = Cursors.Arrow;
                GridBackground.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion

        #region RenderCanvas Mouse Event Handlers

        private void RenderCanvas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var worldPt = ScreenToWorld(e.GetPosition(RenderCanvas));
            var gridPoint = new Point(
                worldPt.X / GridCellSize,
                worldPt.Y / GridCellSize);

            var hitWall = _wallService.HitTest(gridPoint, 0.5);
            if (hitWall != null && hitWall.WallType == WallType.Door)
            {
                hitWall.IsOpen = !hitWall.IsOpen;
                AddToActionLog("Door", $"{hitWall.Label} is now {(hitWall.IsOpen ? "OPEN" : "CLOSED")}");
                RedrawWalls();
                RedrawLighting();
                e.Handled = true;
            }
        }

        private void RenderCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMiddlePanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                var pt = e.GetPosition(this);
                var dx = pt.X - _middlePanLast.X;
                var dy = pt.Y - _middlePanLast.Y;
                _pan.X += dx;
                _pan.Y += dy;
                _middlePanLast = pt;

                UpdateGridVisual();
                RedrawLighting();
                RedrawMovementOverlay();
                RedrawPathVisual();

                e.Handled = true;
                return;
            }
        }

        private void RenderCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var imageFile = files.FirstOrDefault(f =>
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(imageFile))
                    {
                        MapImage.Source = new BitmapImage(new Uri(imageFile));
                        MapImage.SetValue(Canvas.ZIndexProperty, -100);
                        e.Handled = true;
                        return;
                    }
                }

                if (e.Data.GetDataPresent("DnDBattle.Token"))
                {
                    var proto = e.Data.GetData("DnDBattle.Token") as Token;
                    if (proto != null)
                    {
                        var protoId = proto.Id.ToString();
                        var now = DateTime.UtcNow;
                        if (_lastDropPrototypeId == protoId && (now - _lastDropTime) < _duplicateDropThreshold)
                        {
                            e.Handled = true;
                            return;
                        }
                        _lastDropPrototypeId = protoId;
                        _lastDropTime = now;

                        var dropPt = e.GetPosition(RenderCanvas);
                        var world = ScreenToWorld(dropPt);
                        int gx = (int)Math.Floor(world.X / GridCellSize);
                        int gy = (int)Math.Floor(world.Y / GridCellSize);

                        // Create a FULL copy of the token with ALL properties
                        var newToken = new Token
                        {
                            Id = Guid.NewGuid(),
                            Name = proto.Name,
                            Size = proto.Size,
                            Type = proto.Type,
                            Alignment = proto.Alignment,
                            ChallengeRating = proto.ChallengeRating,
                            Image = proto.Image,
                            IconPath = proto.IconPath,
                            HP = proto.MaxHP,
                            MaxHP = proto.MaxHP,
                            HitDice = proto.HitDice,
                            ArmorClass = proto.ArmorClass,
                            InitiativeModifier = proto.InitiativeModifier,
                            IsPlayer = proto.IsPlayer,
                            Speed = proto.Speed,
                            GridX = gx,
                            GridY = gy,
                            SizeInSquares = proto.SizeInSquares > 0 ? proto.SizeInSquares : 1,

                            // Ability Scores
                            Str = proto.Str,
                            Dex = proto.Dex,
                            Con = proto.Con,
                            Int = proto.Int,
                            Wis = proto.Wis,
                            Cha = proto.Cha,

                            // Extra info
                            Skills = proto.Skills?.ToList() ?? new System.Collections.Generic.List<string>(),
                            Senses = proto.Senses,
                            Languages = proto.Languages,
                            Immunities = proto.Immunities,
                            Resistances = proto.Resistances,
                            Vulnerabilities = proto.Vulnerabilities,
                            Traits = proto.Traits,
                            Notes = proto.Notes,

                            // ACTIONS
                            Actions = proto.Actions?.Select(a => new Models.Action
                            {
                                Name = a.Name,
                                AttackBonus = a.AttackBonus,
                                DamageExpression = a.DamageExpression,
                                Range = a.Range,
                                Description = a.Description
                            }).ToList() ?? new System.Collections.Generic.List<Models.Action>(),

                            BonusActions = proto.BonusActions?.Select(a => new Models.Action
                            {
                                Name = a.Name,
                                AttackBonus = a.AttackBonus,
                                DamageExpression = a.DamageExpression,
                                Range = a.Range,
                                Description = a.Description
                            }).ToList() ?? new System.Collections.Generic.List<Models.Action>(),

                            Reactions = proto.Reactions?.Select(a => new Models.Action
                            {
                                Name = a.Name,
                                AttackBonus = a.AttackBonus,
                                DamageExpression = a.DamageExpression,
                                Range = a.Range,
                                Description = a.Description
                            }).ToList() ?? new System.Collections.Generic.List<Models.Action>(),

                            LegendaryActions = proto.LegendaryActions?.Select(a => new Models.Action
                            {
                                Name = a.Name,
                                AttackBonus = a.AttackBonus,
                                DamageExpression = a.DamageExpression,
                                Range = a.Range,
                                Description = a.Description
                            }).ToList() ?? new System.Collections.Generic.List<Models.Action>(),

                            Tags = proto.Tags?.ToList() ?? new System.Collections.Generic.List<string>()
                        };

                        Tokens?.Add(newToken);
                        SelectedToken = newToken;
                        TokenAddedToMap?.Invoke(newToken);
                        RedrawMovementOverlay();
                        RedrawPathVisual();
                        e.Handled = true;
                        return;
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Drop handler error: {ex}"); }
        }

        #endregion
    }
}
