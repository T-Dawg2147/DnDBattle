using DnDBattle.Services;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    public class BattleGridFogOfWarManager
    {
        #region Fields

        private byte[,] _fogData;
        private int _gridWidth;
        private int _gridHeight;
        private bool _isEnabled;
        private bool _isPlayerView = false;

        // Brush settings
        private FogBrushMode _brushMode = FogBrushMode.Reveal;
        private int _brushSize = 3;

        #endregion

        #region Events

        public event Action FogChanged;

        #endregion

        #region Properties

        public bool IsEnabled => _isEnabled;
        public bool IsPlayerView => _isPlayerView;
        public FogBrushMode BrushMode => _brushMode;
        public int BrushSize => _brushSize;
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;

        #endregion

        #region Constructor

        public BattleGridFogOfWarManager()
        {
            Debug.WriteLine("[FogManager] Initialized");
        }

        #endregion

        #region Public Methods - Initialization

        public void Initialize(int width, int height)
        {
            _gridWidth = width;
            _gridHeight = height;
            _fogData = new byte[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _fogData[x, y] = 0;
                }
            }

            Debug.WriteLine($"[FogManager] Initialized {width}x{height} fog grid");
            FogChanged?.Invoke();
        }

        public void SetEnabled(bool enabled)
        {
            if (_isEnabled != enabled)
            {
                _isEnabled = enabled;
                Debug.WriteLine($"[FogManager] Fog enabled: {enabled}");
                FogChanged?.Invoke();
            }
        }

        public void SetPlayerView(bool isPlayerView)
        {
            if (_isPlayerView != isPlayerView)
            {
                _isPlayerView = isPlayerView;
                Debug.WriteLine($"[FogManager] Player view: {isPlayerView}");
                FogChanged?.Invoke();
            }
        }

        #endregion

        #region Public Methods - Brush Settings

        public void SetBrushMode(FogBrushMode mode)
        {
            _brushMode = mode;
            Debug.WriteLine($"[FogManager] Brush mode: {mode}");
        }

        public void SetBrushSize(int size)
        {
            _brushSize = size;
            Debug.WriteLine($"[FogManager] Brush size: {size}");
        }

        #endregion

        #region Public Methods - Fog Queries

        public bool IsCellRevealed(int gridX, int gridY)
        {
            if (!_isEnabled || _fogData == null)
                return true; // No fog, everything visible

            if (gridX < 0 || gridX >= _gridWidth || gridY < 0 || gridY >= _gridHeight)
                return false;

            return _fogData[gridX, gridY] > 127;
        }

        public byte[,] GetFogData()
        {
            return _fogData;
        }

        public void SetFogData(byte[,] fogData)
        {
            if (fogData != null)
            {
                _fogData = fogData;
                _gridWidth = fogData.GetLength(0);
                _gridHeight = fogData.GetLength(1);
                Debug.WriteLine($"[FogManager] Loaded fog data: {_gridWidth}x{_gridHeight}");
                FogChanged?.Invoke();
            }
        }

        #endregion

        #region Public Methods - Fog Manipulation

        public void ApplyBrush(int centerX, int centerY)
        {
            if (_fogData == null) return;

            byte targetValue = _brushMode == FogBrushMode.Reveal ? (byte)255 : (byte)0;

            for (int x = centerX - _brushSize; x <= centerX + _brushSize; x++)
            {
                for (int y = centerY - _brushSize; y <= centerY + _brushSize; y++)
                {
                    if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridWidth)
                        continue;

                    double dx = x - centerX;
                    double dy = y - centerY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= _brushSize)
                    {
                        _fogData[x, y] = targetValue;
                    }
                }
            }
            FogChanged?.Invoke();
        }

        /// <summary>
        /// Reveal a rectangular area
        /// </summary>
        public void RevealRectangle(int x1, int y1, int x2, int y2)
        {
            if (_fogData == null) return;

            int minX = Math.Max(0, Math.Min(x1, x2));
            int maxX = Math.Min(_gridWidth - 1, Math.Max(x1, x2));
            int minY = Math.Max(0, Math.Min(y1, y2));
            int maxY = Math.Min(_gridHeight - 1, Math.Max(y1, y2));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    _fogData[x, y] = 255;
                }
            }

            Debug.WriteLine($"[FogManager] Revealed rectangle ({minX},{minY}) to ({maxX},{maxY})");
            FogChanged?.Invoke();
        }

        /// <summary>
        /// Reveal a circular area
        /// </summary>
        public void RevealCircle(int centerX, int centerY, int radius)
        {
            if (_fogData == null) return;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                        continue;

                    double dx = x - centerX;
                    double dy = y - centerY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= radius)
                    {
                        _fogData[x, y] = 255;
                    }
                }
            }

            Debug.WriteLine($"[FogManager] Revealed circle at ({centerX},{centerY}) radius {radius}");
            FogChanged?.Invoke();
        }

        /// <summary>
        /// Reveal area around player tokens
        /// </summary>
        public void RevealAroundTokens(System.Collections.ObjectModel.ObservableCollection<DnDBattle.Models.Token> tokens, int visionRange = 6)
        {
            if (_fogData == null || tokens == null) return;

            foreach (var token in tokens)
            {
                if (token.IsPlayer)
                {
                    RevealCircle(token.GridX, token.GridY, visionRange);
                }
            }

            Debug.WriteLine($"[FogManager] Revealed around {tokens.Count(t => t.IsPlayer)} player tokens");
        }

        /// <summary>
        /// Reveal all fog
        /// </summary>
        public void RevealAll()
        {
            if (_fogData == null) return;

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _fogData[x, y] = 255;
                }
            }

            Debug.WriteLine($"[FogManager] Revealed all fog");
            FogChanged?.Invoke();
        }

        /// <summary>
        /// Hide all fog (reset)
        /// </summary>
        public void HideAll()
        {
            if (_fogData == null) return;

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _fogData[x, y] = 0;
                }
            }

            Debug.WriteLine($"[FogManager] Reset all fog to hidden");
            FogChanged?.Invoke();
        }

        #endregion
    }
}
