using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using System.Windows.Input;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing keyboard event handlers
    /// </summary>
    public partial class BattleGridControl
    {
        #region Keyboard Event Handlers

        private void BattleGridControl_KeyDown(object sender, KeyEventArgs e)
        {
            // Calculate pan amount based on current zoom level
            double basePanAmount = GridCellSize * 2; // Pan by 2 grid cells

            // Hold Shift for faster panning (5 cells)
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                basePanAmount = GridCellSize * 5;
            }

            double panAmount = basePanAmount * _zoom.ScaleX;
            bool handled = true;

            switch (e.Key)
            {
                case Key.Left:
                    _pan.X += panAmount;
                    break;
                case Key.Right:
                    _pan.X -= panAmount;
                    break;
                case Key.Up:
                    _pan.Y += panAmount;
                    break;
                case Key.Down:
                    _pan.Y -= panAmount;
                    break;
                case Key.Home:
                    // Reset view to origin (cell A1 at top-left)
                    _pan.X = 0;
                    _pan.Y = 0;
                    _zoom.ScaleX = 1;
                    _zoom.ScaleY = 1;
                    break;
                case Key.Add:
                case Key.OemPlus:
                    ZoomAtCenter(1.2);
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    ZoomAtCenter(1.0 / 1.2);
                    break;
                case Key.S: // S = Search for secrets
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && SelectedToken != null)
                    {
                        SearchForSecrets(SelectedToken);
                        e.Handled = true;
                        return;
                    }
                    break;

                case Key.E: // E = Interact with object
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && SelectedToken != null)
                    {
                        InteractWithObject(SelectedToken);
                        e.Handled = true;
                        return;
                    }
                    break;

                case Key.H: // H = Use healing zone
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && SelectedToken != null)
                    {
                        ActivateHealingZone(SelectedToken);
                        e.Handled = true;
                        return;
                    }
                    break;
                case Key.F: // F = Find/Search for traps
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        TriggerTrapDetection(SelectedToken);
                        e.Handled = true;
                        return;
                    }
                    break;

                case Key.T: // T = Trap disarm
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        AttemptDisarmTrap(SelectedToken);
                        e.Handled = true;
                        return;
                    }
                    break;
                case Key.Delete:
                    // Delete selected token
                    if (SelectedToken != null)
                    {
                        RequestDeleteToken?.Invoke(SelectedToken);
                    }
                    break;
                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                ClampPanToBoundaries();
                RefreshAllVisuals();
                e.Handled = true;
            }
        }

        public void HandleKeyDown(Key key)
        {
            if (key == Key.Escape && _isInTargetingMode)
            {
                ExitTargetingMode();
            }
        }

        #endregion
    }
}
