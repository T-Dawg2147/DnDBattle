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

        public void StopPanning()
        {
            if (_isPanning || _isMiddleMousePanning)
            {
                _isPanning = false;
                _isMiddleMousePanning = false;
                Debug.WriteLine("[InputManager] Panning stopped externally");
            }
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
            double panAmount = cellSize * 1;

            // Faster panning with Shift: 3 cells
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                panAmount = cellSize * 3; 
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