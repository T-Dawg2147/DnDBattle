using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    /// <summary>
    /// Manages all input handling for the battle grid
    /// Handles: Mouse panning, zooming, keyboard shortcuts
    /// </summary>
    public class BattleGridInputManager
    {
        #region Fields

        // Panning state
        private bool _isPanning;
        private Point _lastPanPoint;
        private bool _isMiddleMousePanning;
        private Point _lastMiddleMousePoint;

        #endregion

        #region Events

        public event Action<double, double> PanChanged; // deltaX, deltaY
        public event Action<double, Point> ZoomChanged; // zoomFactor, zoomCenter
        public event Action<Point> GridPositionChanged; // current grid position
        public event Action ResetViewRequested;
        public event Action<double> ZoomAtCenterRequested;

        #endregion

        #region Properties

        public bool IsPanning => _isPanning || _isMiddleMousePanning;

        #endregion

        #region Constructor

        public BattleGridInputManager()
        {
            System.Diagnostics.Debug.WriteLine("[InputManager] Initialized");
        }

        #endregion

        #region Public Methods - Mouse Handling

        /// <summary>
        /// Handles mouse down events
        /// </summary>
        public void HandleMouseDown(MouseButtonEventArgs e, Point position, Transform transform, double cellSize)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Start left-click panning (on empty space)
                _isPanning = true;
                _lastPanPoint = position;
                System.Diagnostics.Debug.WriteLine("[InputManager] Started panning (left mouse)");
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                // Start middle-click panning
                _isMiddleMousePanning = true;
                _lastMiddleMousePoint = position;
                System.Diagnostics.Debug.WriteLine("[InputManager] Started panning (middle mouse)");
            }
        }

        /// <summary>
        /// Handles mouse move events
        /// </summary>
        public void HandleMouseMove(MouseEventArgs e, Point position, Transform transform, double cellSize, double viewWidth, double viewHeight)
        {
            // ALWAYS update grid position
            UpdateGridPosition(position, transform, cellSize);

            // Check if we should still be panning
            if (_isPanning)
            {
                // If left button is released, stop panning immediately
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    _isPanning = false;
                    Debug.WriteLine("[InputManager] Panning stopped - button released");
                    return;
                }

                var delta = position - _lastPanPoint;

                // Only pan if moved a reasonable distance
                if (Math.Abs(delta.X) > 0.5 || Math.Abs(delta.Y) > 0.5)
                {
                    PanChanged?.Invoke(delta.X, delta.Y);
                    _lastPanPoint = position;
                }
            }

            // Middle mouse panning
            if (_isMiddleMousePanning)
            {
                // If middle button released, stop
                if (e.MiddleButton != MouseButtonState.Pressed)
                {
                    _isMiddleMousePanning = false;
                    Debug.WriteLine("[InputManager] Middle panning stopped");
                    return;
                }

                var delta = position - _lastMiddleMousePoint;

                if (Math.Abs(delta.X) > 0.5 || Math.Abs(delta.Y) > 0.5)
                {
                    PanChanged?.Invoke(delta.X, delta.Y);
                    _lastMiddleMousePoint = position;
                }
            }
        }

        /// <summary>
        /// Handles mouse up events
        /// </summary>
        public void HandleMouseUp(MouseButtonEventArgs e, Point position)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (_isPanning)
                {
                    _isPanning = false;
                    System.Diagnostics.Debug.WriteLine("[InputManager] Stopped panning (left mouse)");
                }
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                if (_isMiddleMousePanning)
                {
                    _isMiddleMousePanning = false;
                    System.Diagnostics.Debug.WriteLine("[InputManager] Stopped panning (middle mouse)");
                }
            }
        }

        /// <summary>
        /// Handles mouse wheel events (zooming)
        /// </summary>
        public void HandleMouseWheel(MouseWheelEventArgs e, Point position, double viewWidth, double viewHeight)
        {
            double zoomFactor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;

            ZoomChanged?.Invoke(zoomFactor, position);

            System.Diagnostics.Debug.WriteLine($"[InputManager] Zoom: {(e.Delta > 0 ? "In" : "Out")}");
            e.Handled = true;
        }

        public void StopPanning()
        {
            if (_isPanning || _isMiddleMousePanning)
            {
                _isPanning = false;
                _isMiddleMousePanning = false;
                Debug.WriteLine("[InputManager] Panning stopped externally");
            }
        }

        /// <summary>
        /// Updates the current grid position (public so it can be called from MouseMove)
        /// </summary>
        public void UpdateGridPosition(Point screenPosition, Transform transform, double cellSize)
        {
            var worldPosition = transform.Inverse?.Transform(screenPosition) ?? screenPosition;
            var gridPosition = new Point(worldPosition.X / cellSize, worldPosition.Y / cellSize);

            GridPositionChanged?.Invoke(gridPosition);
        }

        #endregion

        #region Public Methods - Keyboard Handling

        /// <summary>
        /// Handles keyboard shortcuts
        /// </summary>
        public void HandleKeyDown(KeyEventArgs e, double cellSize)
        {
            bool handled = false;

            // BASE pan amount: 1 cell
            double panAmount = cellSize * 1; // ← Changed from 2 to 1

            // Faster panning with Shift: 3 cells
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                panAmount = cellSize * 3; // ← Changed from 5 to 3
            }

            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    PanChanged?.Invoke(panAmount, 0);
                    handled = true;
                    break;

                case Key.Right:
                case Key.D:
                    PanChanged?.Invoke(-panAmount, 0);
                    handled = true;
                    break;

                case Key.Up:
                case Key.W:
                    PanChanged?.Invoke(0, panAmount);
                    handled = true;
                    break;

                case Key.Down:
                case Key.S:
                    PanChanged?.Invoke(0, -panAmount);
                    handled = true;
                    break;

                case Key.Home:
                    // Reset view to origin
                    System.Diagnostics.Debug.WriteLine("[InputManager] Reset view (Home key)");
                    // Fire an event to trigger ResetView
                    ResetViewRequested?.Invoke();
                    handled = true;
                    break;

                case Key.Add:
                case Key.OemPlus:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        // Zoom in at center
                        System.Diagnostics.Debug.WriteLine("[InputManager] Zoom in (Ctrl++)");
                        ZoomAtCenterRequested?.Invoke(1.15); // Zoom in
                        handled = true;
                    }
                    break;

                case Key.Subtract:
                case Key.OemMinus:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        // Zoom out at center
                        System.Diagnostics.Debug.WriteLine("[InputManager] Zoom out (Ctrl+-)");
                        ZoomAtCenterRequested?.Invoke(1.0 / 1.15); // Zoom out
                        handled = true;
                    }
                    break;
            }

            if (handled)
            {
                e.Handled = true;
            }
        }

        #endregion

        #region Private Methods



        #endregion
    }
}