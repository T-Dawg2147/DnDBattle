using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DnDBattle.Services
{
    /// <summary>
    /// Manages fog of war state and rendering
    /// </summary>
    public class FogOfWarService
    {
        // Fog state - true = revealed, false = hidden
        private bool[,] _fogGrid;
        private int _gridWidth;
        private int _gridHeight;
        private double _cellSize;

        // Settings
        public bool IsEnabled { get; set; } = false;
        public bool ShowPlayerView { get; set; } = false;
        public double DmFogOpacity { get; set; } = 0.5;
        public double PlayerFogOpacity { get; set; } = 1.0;
        public Color FogColor { get; set; } = Color.FromRgb(20, 20, 25);

        // Brush settings
        public int BrushSize { get; set; } = 3; // In grid squares
        public FogBrushMode BrushMode { get; set; } = FogBrushMode.Reveal;

        // Events
        public event Action FogChanged;
        public event Action<string> LogMessage;

        public FogOfWarService()
        {
            // Initialize with default size
            Initialize(50, 50, 48);
        }

        /// <summary>
        /// Initializes the fog grid with specified dimensions
        /// </summary>
        public void Initialize(int gridWidth, int gridHeight, double cellSize)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _cellSize = cellSize;
            _fogGrid = new bool[gridWidth, gridHeight];

            // Start fully hidden
            ClearAll();
        }

        /// <summary>
        /// Resizes the fog grid (preserves existing revealed areas where possible)
        /// </summary>
        public void Resize(int newWidth, int newHeight)
        {
            var oldGrid = _fogGrid;
            int oldWidth = _gridWidth;
            int oldHeight = _gridHeight;

            _gridWidth = newWidth;
            _gridHeight = newHeight;
            _fogGrid = new bool[newWidth, newHeight];

            // Copy over existing data
            for (int x = 0; x < Math.Min(oldWidth, newWidth); x++)
            {
                for (int y = 0; y < Math.Min(oldHeight, newHeight); y++)
                {
                    _fogGrid[x, y] = oldGrid[x, y];
                }
            }

            FogChanged?.Invoke();
        }

        #region Reveal/Hide Methods

        /// <summary>
        /// Reveals a single cell
        /// </summary>
        public void RevealCell(int x, int y)
        {
            if (IsValidCell(x, y) && !_fogGrid[x, y])
            {
                _fogGrid[x, y] = true;
                FogChanged?.Invoke();
            }
        }

        /// <summary>
        /// Hides a single cell
        /// </summary>
        public void HideCell(int x, int y)
        {
            if (IsValidCell(x, y) && _fogGrid[x, y])
            {
                _fogGrid[x, y] = false;
                FogChanged?.Invoke();
            }
        }

        /// <summary>
        /// Reveals cells in a circular area around a point
        /// </summary>
        public void RevealCircle(int centerX, int centerY, int radius)
        {
            bool changed = false;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (IsValidCell(x, y))
                    {
                        // Check if within circle
                        double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                        if (distance <= radius && !_fogGrid[x, y])
                        {
                            _fogGrid[x, y] = true;
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
                FogChanged?.Invoke();
        }

        /// <summary>
        /// Hides cells in a circular area
        /// </summary>
        public void HideCircle(int centerX, int centerY, int radius)
        {
            bool changed = false;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (IsValidCell(x, y))
                    {
                        double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                        if (distance <= radius && _fogGrid[x, y])
                        {
                            _fogGrid[x, y] = false;
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
                FogChanged?.Invoke();
        }

        /// <summary>
        /// Reveals cells in a rectangular area
        /// </summary>
        public void RevealRectangle(int x1, int y1, int x2, int y2)
        {
            bool changed = false;
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            int minY = Math.Min(y1, y2);
            int maxY = Math.Max(y1, y2);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (IsValidCell(x, y) && !_fogGrid[x, y])
                    {
                        _fogGrid[x, y] = true;
                        changed = true;
                    }
                }
            }

            if (changed)
                FogChanged?.Invoke();
        }

        /// <summary>
        /// Hides cells in a rectangular area
        /// </summary>
        public void HideRectangle(int x1, int y1, int x2, int y2)
        {
            bool changed = false;
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            int minY = Math.Min(y1, y2);
            int maxY = Math.Max(y1, y2);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (IsValidCell(x, y) && _fogGrid[x, y])
                    {
                        _fogGrid[x, y] = false;
                        changed = true;
                    }
                }
            }

            if (changed)
                FogChanged?.Invoke();
        }

        /// <summary>
        /// Applies brush at a position (for click-drag revealing)
        /// </summary>
        public void ApplyBrush(int centerX, int centerY)
        {
            if (BrushMode == FogBrushMode.Reveal)
            {
                RevealCircle(centerX, centerY, BrushSize);
            }
            else
            {
                HideCircle(centerX, centerY, BrushSize);
            }
        }

        /// <summary>
        /// Reveals all cells
        /// </summary>
        public void RevealAll()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _fogGrid[x, y] = true;
                }
            }
            FogChanged?.Invoke();
            LogMessage?.Invoke("🌅 All fog cleared - entire map revealed");
        }

        /// <summary>
        /// Hides all cells (resets fog)
        /// </summary>
        public void ClearAll()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _fogGrid[x, y] = false;
                }
            }
            FogChanged?.Invoke();
            LogMessage?.Invoke("🌫️ Fog reset - entire map hidden");
        }

        #endregion

        #region Token-Based Vision

        /// <summary>
        /// Reveals area around player tokens based on their vision
        /// </summary>
        public void RevealAroundTokens(IEnumerable<Models.Token> tokens, bool playersOnly = true)
        {
            foreach (var token in tokens)
            {
                if (playersOnly && !token.IsPlayer)
                    continue;

                int visionRadius = GetTokenVisionRadius(token);
                RevealCircle(token.GridX, token.GridY, visionRadius);
            }
        }

        /// <summary>
        /// Gets the vision radius for a token in grid squares
        /// </summary>
        private int GetTokenVisionRadius(Models.Token token)
        {
            // Base vision: 60 feet = 12 squares in normal light
            // Darkvision: typically 60 feet = 12 squares
            // Can be customized based on token properties

            // Check for darkvision in senses
            bool hasDarkvision = token.Senses?.ToLower().Contains("darkvision") ?? false;

            // Parse darkvision distance if present
            if (hasDarkvision)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    token.Senses ?? "",
                    @"darkvision\s*(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success && int.TryParse(match.Groups[1].Value, out int feet))
                {
                    return feet / 5; // Convert feet to squares
                }
                return 12; // Default 60 ft darkvision
            }

            // Normal vision in lit areas
            return 12; // 60 feet
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Checks if a cell is revealed
        /// </summary>
        public bool IsCellRevealed(int x, int y)
        {
            if (!IsValidCell(x, y))
                return false;
            return _fogGrid[x, y];
        }

        /// <summary>
        /// Checks if a cell is valid
        /// </summary>
        public bool IsValidCell(int x, int y)
        {
            return x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;
        }

        /// <summary>
        /// Checks if a token is visible (in revealed area)
        /// </summary>
        public bool IsTokenVisible(Models.Token token)
        {
            if (!IsEnabled)
                return true;

            // Check if any cell the token occupies is revealed
            for (int dx = 0; dx < token.SizeInSquares; dx++)
            {
                for (int dy = 0; dy < token.SizeInSquares; dy++)
                {
                    if (IsCellRevealed(token.GridX + dx, token.GridY + dy))
                        return true;
                }
            }
            return false;
        }

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;

        #endregion

        #region Rendering

        /// <summary>
        /// Renders the fog overlay to a DrawingVisual
        /// </summary>
        public DrawingVisual RenderFog(double canvasWidth, double canvasHeight, bool isPlayerView)
        {
            var visual = new DrawingVisual();

            if (!IsEnabled)
                return visual;

            using (var dc = visual.RenderOpen())
            {
                double opacity = isPlayerView ? PlayerFogOpacity : DmFogOpacity;
                var fogBrush = new SolidColorBrush(FogColor);
                fogBrush.Opacity = opacity;

                // Draw fog for each hidden cell
                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int y = 0; y < _gridHeight; y++)
                    {
                        if (!_fogGrid[x, y]) // Cell is hidden
                        {
                            var rect = new Rect(
                                x * _cellSize,
                                y * _cellSize,
                                _cellSize,
                                _cellSize);

                            dc.DrawRectangle(fogBrush, null, rect);
                        }
                    }
                }

                // For DM view, draw a subtle grid over revealed areas to show fog boundaries
                if (!isPlayerView && DmFogOpacity < 1.0)
                {
                    var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 100, 100, 255)), 1);

                    for (int x = 0; x < _gridWidth; x++)
                    {
                        for (int y = 0; y < _gridHeight; y++)
                        {
                            if (!_fogGrid[x, y])
                            {
                                var rect = new Rect(
                                    x * _cellSize,
                                    y * _cellSize,
                                    _cellSize,
                                    _cellSize);

                                dc.DrawRectangle(null, borderPen, rect);
                            }
                        }
                    }
                }
            }

            return visual;
        }

        /// <summary>
        /// Gets the fog state as a serializable array for saving
        /// </summary>
        public bool[,] GetFogState()
        {
            var copy = new bool[_gridWidth, _gridHeight];
            Array.Copy(_fogGrid, copy, _fogGrid.Length);
            return copy;
        }

        /// <summary>
        /// Loads fog state from a saved array
        /// </summary>
        public void LoadFogState(bool[,] state)
        {
            if (state == null) return;

            int width = Math.Min(state.GetLength(0), _gridWidth);
            int height = Math.Min(state.GetLength(1), _gridHeight);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _fogGrid[x, y] = state[x, y];
                }
            }

            FogChanged?.Invoke();
        }

        #endregion
    }

    public enum FogBrushMode
    {
        Reveal,
        Hide
    }
}