using System;
using System.Collections.Generic;
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
using DnDBattle.Services.TileService;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.UI;

namespace DnDBattle.Services.UI
{
    /// <summary>
    /// Generates procedural maps using BSP (dungeons) and cellular automata (caves).
    /// Operates on flat byte arrays for cache-friendly memory access.
    /// </summary>
    public sealed class ProceduralMapService
    {
        // Cell types for the internal generation grid
        private const byte CellEmpty = 0;
        private const byte CellFloor = 1;
        private const byte CellWall = 2;
        private const byte CellCorridor = 3;
        private const byte CellDoor = 4;

        /// <summary>
        /// Generates a procedural map and returns the cell grid + room info.
        /// The grid is a flat byte array of width×height with cell types.
        /// Returns an empty grid when <see cref="Options.EnableProceduralMapGeneration"/> is false.
        /// </summary>
        public ProceduralMapResult Generate(ProceduralMapConfig config)
        {
            if (!Options.EnableProceduralMapGeneration)
                return new ProceduralMapResult(config.Width, config.Height);

            return config.Type switch
            {
                MapGenerationType.Dungeon => GenerateDungeon(config),
                MapGenerationType.Cave => GenerateCave(config),
                MapGenerationType.Arena => GenerateArena(config),
                _ => new ProceduralMapResult(config.Width, config.Height)
            };
        }

        #region BSP Dungeon Generation

        private ProceduralMapResult GenerateDungeon(ProceduralMapConfig config)
        {
            var rng = config.Seed.HasValue ? new Random(config.Seed.Value) : new Random();
            int w = config.Width, h = config.Height;
            byte[] grid = new byte[w * h];

            // Fill with walls
            Array.Fill(grid, CellWall);

            // BSP split
            var root = new BspNode(1, 1, w - 2, h - 2);
            SplitBsp(root, config.MinRoomSize, rng);

            // Collect leaf rooms
            var rooms = new List<RoomInfo>();
            CollectLeafRooms(root, rooms, config, rng);

            // Carve rooms into grid
            foreach (var room in rooms)
            {
                for (int ry = room.Y; ry < room.Y + room.Height; ry++)
                    for (int rx = room.X; rx < room.X + room.Width; rx++)
                        grid[ry * w + rx] = CellFloor;
            }

            // Connect rooms with corridors
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                var a = rooms[i];
                var b = rooms[i + 1];
                int ax = a.X + a.Width / 2, ay = a.Y + a.Height / 2;
                int bx = b.X + b.Width / 2, by = b.Y + b.Height / 2;

                // L-shaped corridor
                CarveHorizontalCorridor(grid, w, ax, bx, ay, config.CorridorWidth);
                CarveVerticalCorridor(grid, w, h, bx, ay, by, config.CorridorWidth);
            }

            // Place doors at room entries if configured
            if (config.AddDoors)
                PlaceDoors(grid, w, h, rooms);

            return new ProceduralMapResult(w, h) { Grid = grid, Rooms = rooms };
        }

        private void SplitBsp(BspNode node, int minSize, Random rng)
        {
            if (node.Width < minSize * 2 + 2 && node.Height < minSize * 2 + 2)
                return;

            bool splitH;
            if (node.Width < minSize * 2 + 2) splitH = true;
            else if (node.Height < minSize * 2 + 2) splitH = false;
            else if (node.Width > node.Height * 1.25) splitH = false;
            else if (node.Height > node.Width * 1.25) splitH = true;
            else splitH = rng.NextDouble() > 0.5;

            if (splitH)
            {
                int splitY = rng.Next(minSize, node.Height - minSize);
                node.Left = new BspNode(node.X, node.Y, node.Width, splitY);
                node.Right = new BspNode(node.X, node.Y + splitY, node.Width, node.Height - splitY);
            }
            else
            {
                int splitX = rng.Next(minSize, node.Width - minSize);
                node.Left = new BspNode(node.X, node.Y, splitX, node.Height);
                node.Right = new BspNode(node.X + splitX, node.Y, node.Width - splitX, node.Height);
            }

            SplitBsp(node.Left, minSize, rng);
            SplitBsp(node.Right, minSize, rng);
        }

        private void CollectLeafRooms(BspNode node, List<RoomInfo> rooms, ProceduralMapConfig config, Random rng)
        {
            if (node.Left == null && node.Right == null)
            {
                // Leaf node – create a room within this partition
                int padding = 1;
                int rw = rng.Next(config.MinRoomSize, Math.Min(config.MaxRoomSize, node.Width - padding * 2) + 1);
                int rh = rng.Next(config.MinRoomSize, Math.Min(config.MaxRoomSize, node.Height - padding * 2) + 1);
                int rx = node.X + rng.Next(0, Math.Max(1, node.Width - rw - padding));
                int ry = node.Y + rng.Next(0, Math.Max(1, node.Height - rh - padding));

                rooms.Add(new RoomInfo { X = rx, Y = ry, Width = rw, Height = rh, Name = $"Room {rooms.Count + 1}" });
                return;
            }

            if (node.Left != null) CollectLeafRooms(node.Left, rooms, config, rng);
            if (node.Right != null) CollectLeafRooms(node.Right, rooms, config, rng);
        }

        private void CarveHorizontalCorridor(byte[] grid, int mapW, int x1, int x2, int y, int corridorWidth)
        {
            int start = Math.Min(x1, x2);
            int end = Math.Max(x1, x2);
            for (int x = start; x <= end; x++)
                for (int dy = 0; dy < corridorWidth; dy++)
                {
                    int idx = (y + dy) * mapW + x;
                    if (idx >= 0 && idx < grid.Length && grid[idx] != CellFloor)
                        grid[idx] = CellCorridor;
                }
        }

        private void CarveVerticalCorridor(byte[] grid, int mapW, int mapH, int x, int y1, int y2, int corridorWidth)
        {
            int start = Math.Min(y1, y2);
            int end = Math.Max(y1, y2);
            for (int y = start; y <= end; y++)
                for (int dx = 0; dx < corridorWidth; dx++)
                {
                    int idx = y * mapW + (x + dx);
                    if (idx >= 0 && idx < grid.Length && grid[idx] != CellFloor)
                        grid[idx] = CellCorridor;
                }
        }

        private void PlaceDoors(byte[] grid, int w, int h, List<RoomInfo> rooms)
        {
            foreach (var room in rooms)
            {
                // Check edges for corridor connections → place doors
                for (int x = room.X; x < room.X + room.Width; x++)
                {
                    TryPlaceDoor(grid, w, h, x, room.Y - 1, x, room.Y);
                    TryPlaceDoor(grid, w, h, x, room.Y + room.Height, x, room.Y + room.Height - 1);
                }
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    TryPlaceDoor(grid, w, h, room.X - 1, y, room.X, y);
                    TryPlaceDoor(grid, w, h, room.X + room.Width, y, room.X + room.Width - 1, y);
                }
            }
        }

        private void TryPlaceDoor(byte[] grid, int w, int h, int corridorX, int corridorY, int roomX, int roomY)
        {
            if (corridorX < 0 || corridorY < 0 || corridorX >= w || corridorY >= h) return;
            if (roomX < 0 || roomY < 0 || roomX >= w || roomY >= h) return;

            int ci = corridorY * w + corridorX;
            int ri = roomY * w + roomX;

            if (grid[ci] == CellCorridor && grid[ri] == CellFloor)
                grid[ci] = CellDoor;
        }

        #endregion

        #region Cellular Automata Cave Generation

        private ProceduralMapResult GenerateCave(ProceduralMapConfig config)
        {
            var rng = config.Seed.HasValue ? new Random(config.Seed.Value) : new Random();
            int w = config.Width, h = config.Height;

            // Use two buffers to avoid allocating new arrays each iteration
            byte[] grid = new byte[w * h];
            byte[] buffer = new byte[w * h];

            // Random fill
            for (int i = 0; i < grid.Length; i++)
                grid[i] = rng.NextDouble() < config.CaveFillProbability ? CellWall : CellFloor;

            // Force edges to be walls
            for (int x = 0; x < w; x++)
            {
                grid[x] = CellWall;
                grid[(h - 1) * w + x] = CellWall;
            }
            for (int y = 0; y < h; y++)
            {
                grid[y * w] = CellWall;
                grid[y * w + (w - 1)] = CellWall;
            }

            // Apply cellular automata iterations
            for (int iter = 0; iter < config.CaveIterations; iter++)
            {
                for (int y = 1; y < h - 1; y++)
                {
                    for (int x = 1; x < w - 1; x++)
                    {
                        int walls = CountAdjacentWalls(grid, w, h, x, y);
                        buffer[y * w + x] = walls >= 5 ? CellWall : CellFloor;
                    }
                }

                // Force edges
                for (int x = 0; x < w; x++)
                {
                    buffer[x] = CellWall;
                    buffer[(h - 1) * w + x] = CellWall;
                }
                for (int y = 0; y < h; y++)
                {
                    buffer[y * w] = CellWall;
                    buffer[y * w + (w - 1)] = CellWall;
                }

                // Swap buffers (no allocation)
                (grid, buffer) = (buffer, grid);
            }

            return new ProceduralMapResult(w, h) { Grid = grid, Rooms = new List<RoomInfo>() };
        }

        private static int CountAdjacentWalls(byte[] grid, int w, int h, int x, int y)
        {
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                        count++; // out of bounds = wall
                    else if (grid[ny * w + nx] == CellWall)
                        count++;
                }
            }
            return count;
        }

        #endregion

        #region Arena Generation

        private ProceduralMapResult GenerateArena(ProceduralMapConfig config)
        {
            int w = config.Width, h = config.Height;
            byte[] grid = new byte[w * h];

            // Fill with floor
            Array.Fill(grid, CellFloor);

            // Surround with walls
            for (int x = 0; x < w; x++)
            {
                grid[x] = CellWall;
                grid[(h - 1) * w + x] = CellWall;
            }
            for (int y = 0; y < h; y++)
            {
                grid[y * w] = CellWall;
                grid[y * w + (w - 1)] = CellWall;
            }

            var rooms = new List<RoomInfo>
            {
                new RoomInfo { X = 1, Y = 1, Width = w - 2, Height = h - 2, Name = "Arena" }
            };

            return new ProceduralMapResult(w, h) { Grid = grid, Rooms = rooms };
        }

        #endregion
    }

    /// <summary>
    /// Result of procedural map generation.
    /// </summary>
    public class ProceduralMapResult
    {
        public int Width { get; }
        public int Height { get; }

        /// <summary>Flat byte array: 0=empty, 1=floor, 2=wall, 3=corridor, 4=door.</summary>
        public byte[] Grid { get; set; }

        /// <summary>List of generated rooms with metadata.</summary>
        public List<RoomInfo> Rooms { get; set; } = new();

        public ProceduralMapResult(int width, int height)
        {
            Width = width;
            Height = height;
            Grid = new byte[width * height];
        }

        /// <summary>Gets the cell type at a position. O(1) flat array lookup.</summary>
        public byte GetCell(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return 0;
            return Grid[y * Width + x];
        }
    }

    /// <summary>
    /// Information about a generated room.
    /// </summary>
    public class RoomInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; } = string.Empty;
        public (int x, int y) Center => (X + Width / 2, Y + Height / 2);
    }

    /// <summary>
    /// Internal BSP tree node for dungeon partitioning.
    /// </summary>
    internal class BspNode
    {
        public int X, Y, Width, Height;
        public BspNode? Left, Right;

        public BspNode(int x, int y, int w, int h)
        {
            X = x; Y = y; Width = w; Height = h;
        }
    }
}
