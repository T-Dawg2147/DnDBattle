using System;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.Models.Environment;

namespace DnDBattle.Models.Environment
{
    /// <summary>
    /// Configuration for procedural map generation.
    /// </summary>
    public class ProceduralMapConfig
    {
        /// <summary>Map generation algorithm type.</summary>
        public MapGenerationType Type { get; set; } = MapGenerationType.Dungeon;

        /// <summary>Map width in grid cells.</summary>
        public int Width { get; set; } = 50;

        /// <summary>Map height in grid cells.</summary>
        public int Height { get; set; } = 50;

        /// <summary>Minimum room size in cells (for dungeon generation).</summary>
        public int MinRoomSize { get; set; } = 5;

        /// <summary>Maximum room size in cells.</summary>
        public int MaxRoomSize { get; set; } = 12;

        /// <summary>Target number of rooms (for dungeon generation).</summary>
        public int TargetRoomCount { get; set; } = 15;

        /// <summary>Corridor width in cells.</summary>
        public int CorridorWidth { get; set; } = 2;

        /// <summary>Whether to add doors at room entrances.</summary>
        public bool AddDoors { get; set; } = true;

        /// <summary>Whether to place furniture in rooms.</summary>
        public bool PlaceFurniture { get; set; } = false;

        /// <summary>Fill probability for cave generation (0.0-1.0).</summary>
        public double CaveFillProbability { get; set; } = 0.45;

        /// <summary>Number of cellular automata iterations for cave smoothing.</summary>
        public int CaveIterations { get; set; } = 5;

        /// <summary>Optional seed for reproducible generation. Null = random.</summary>
        public int? Seed { get; set; } = null;

        /// <summary>Tile definition ID for floor tiles.</summary>
        public string FloorTileId { get; set; } = "floor_stone";

        /// <summary>Tile definition ID for wall tiles.</summary>
        public string WallTileId { get; set; } = "wall_stone";

        /// <summary>Tile definition ID for corridor floor.</summary>
        public string CorridorTileId { get; set; } = "floor_corridor";

        /// <summary>Tile definition ID for door tiles.</summary>
        public string DoorTileId { get; set; } = "door_wooden";
    }

    /// <summary>
    /// Types of procedural map generation algorithms.
    /// </summary>
    public enum MapGenerationType
    {
        /// <summary>BSP-based dungeon with rooms and corridors.</summary>
        Dungeon,

        /// <summary>Cellular automata cave system.</summary>
        Cave,

        /// <summary>Open arena-style map.</summary>
        Arena
    }
}
