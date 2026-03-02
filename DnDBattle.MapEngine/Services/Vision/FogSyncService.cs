using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Vision
{
    /// <summary>
    /// Server-side fog of war synchronization for Phase 9 multiplayer.
    /// Manages per-player revealed areas and broadcasts fog updates
    /// using delta encoding for bandwidth efficiency.
    /// </summary>
    public sealed class FogSyncService
    {
        private bool[,]? _fogGrid;
        private int _width;
        private int _height;

        private readonly ConcurrentDictionary<string, HashSet<(int x, int y)>> _playerRevealedCells = new();

        /// <summary>
        /// Initialize the fog grid with given dimensions. All cells start hidden.
        /// </summary>
        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;
            _fogGrid = new bool[width, height];
            _playerRevealedCells.Clear();
        }

        /// <summary>
        /// Reveal cells in a radius. If playerId is null, it's a global DM reveal.
        /// Returns the list of newly revealed cells (delta).
        /// </summary>
        public FogUpdateData RevealArea(int centerX, int centerY, int radius, string? playerId = null)
        {
            var cellsToReveal = GetCellsInRadius(centerX, centerY, radius);
            var newlyRevealed = new List<CellCoord>();

            if (playerId == null)
            {
                // Global DM reveal
                foreach (var cell in cellsToReveal)
                {
                    if (_fogGrid != null && !_fogGrid[cell.X, cell.Y])
                    {
                        _fogGrid[cell.X, cell.Y] = true;
                        newlyRevealed.Add(cell);
                    }
                }
            }
            else
            {
                // Per-player reveal
                var revealed = _playerRevealedCells.GetOrAdd(playerId, _ => new HashSet<(int, int)>());
                foreach (var cell in cellsToReveal)
                {
                    if (revealed.Add((cell.X, cell.Y)))
                        newlyRevealed.Add(cell);
                }
            }

            return new FogUpdateData
            {
                UpdateType = FogUpdateType.Reveal,
                Cells = newlyRevealed,
                IsFullSync = false
            };
        }

        /// <summary>
        /// Hide cells in a radius (DM-only operation).
        /// Returns the list of newly hidden cells (delta).
        /// </summary>
        public FogUpdateData HideArea(int centerX, int centerY, int radius)
        {
            var cellsToHide = GetCellsInRadius(centerX, centerY, radius);
            var newlyHidden = new List<CellCoord>();

            foreach (var cell in cellsToHide)
            {
                if (_fogGrid != null && _fogGrid[cell.X, cell.Y])
                {
                    _fogGrid[cell.X, cell.Y] = false;
                    newlyHidden.Add(cell);
                }
            }

            return new FogUpdateData
            {
                UpdateType = FogUpdateType.Hide,
                Cells = newlyHidden,
                IsFullSync = false
            };
        }

        /// <summary>
        /// Get the full fog state for a player joining mid-session.
        /// Uses RLE compression for bandwidth efficiency.
        /// </summary>
        public FogUpdateData GetFullSync(string? playerId = null)
        {
            if (_fogGrid == null)
                return new FogUpdateData { IsFullSync = true };

            bool[,] effectiveGrid;

            if (playerId != null && _playerRevealedCells.TryGetValue(playerId, out var playerCells))
            {
                effectiveGrid = new bool[_width, _height];
                foreach (var (x, y) in playerCells)
                {
                    if (x >= 0 && x < _width && y >= 0 && y < _height)
                        effectiveGrid[x, y] = true;
                }

                // Merge with global fog
                for (int gx = 0; gx < _width; gx++)
                    for (int gy = 0; gy < _height; gy++)
                        if (_fogGrid[gx, gy]) effectiveGrid[gx, gy] = true;
            }
            else
            {
                effectiveGrid = _fogGrid;
            }

            return new FogUpdateData
            {
                IsFullSync = true,
                UpdateType = FogUpdateType.Reveal,
                CompressedData = CompressFogState(effectiveGrid),
                Cells = new List<CellCoord>()
            };
        }

        /// <summary>
        /// Check if a cell is revealed (globally or for a specific player).
        /// </summary>
        public bool IsCellRevealed(int x, int y, string? playerId = null)
        {
            if (_fogGrid == null || x < 0 || x >= _width || y < 0 || y >= _height)
                return false;

            if (_fogGrid[x, y]) return true;

            if (playerId != null && _playerRevealedCells.TryGetValue(playerId, out var cells))
                return cells.Contains((x, y));

            return false;
        }

        /// <summary>
        /// Run-length encode a fog grid for efficient network transfer.
        /// Typical dungeon: ~200-500 bytes for a 50×50 grid.
        /// </summary>
        public static byte[] CompressFogState(bool[,] fogGrid)
        {
            int width = fogGrid.GetLength(0);
            int height = fogGrid.GetLength(1);
            var rle = new List<byte>();

            // Header: width and height as 2-byte big-endian
            rle.Add((byte)(width >> 8));
            rle.Add((byte)(width & 0xFF));
            rle.Add((byte)(height >> 8));
            rle.Add((byte)(height & 0xFF));

            bool currentValue = fogGrid[0, 0];
            int runLength = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (fogGrid[x, y] == currentValue && runLength < 255)
                    {
                        runLength++;
                    }
                    else
                    {
                        rle.Add((byte)(currentValue ? 1 : 0));
                        rle.Add((byte)runLength);
                        currentValue = fogGrid[x, y];
                        runLength = 1;
                    }
                }
            }

            // Final run
            rle.Add((byte)(currentValue ? 1 : 0));
            rle.Add((byte)runLength);

            return rle.ToArray();
        }

        /// <summary>
        /// Decompress an RLE-encoded fog grid.
        /// </summary>
        public static bool[,] DecompressFogState(byte[] data)
        {
            int width = (data[0] << 8) | data[1];
            int height = (data[2] << 8) | data[3];
            var grid = new bool[width, height];

            int x = 0, y = 0;

            for (int i = 4; i + 1 < data.Length; i += 2)
            {
                bool value = data[i] != 0;
                int count = data[i + 1];

                for (int j = 0; j < count; j++)
                {
                    if (x < width && y < height)
                        grid[x, y] = value;

                    x++;
                    if (x >= width) { x = 0; y++; }
                }
            }

            return grid;
        }

        // ── Private helpers ──

        private List<CellCoord> GetCellsInRadius(int cx, int cy, int radius)
        {
            int estimatedCapacity = (2 * radius + 1) * (2 * radius + 1);
            var cells = new List<CellCoord>(estimatedCapacity);
            int r2 = radius * radius;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx * dx + dy * dy <= r2)
                    {
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                            cells.Add(new CellCoord(nx, ny));
                    }
                }
            }

            return cells;
        }
    }
}
