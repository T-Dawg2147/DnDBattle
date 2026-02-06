using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services.Persistence;

namespace DnDBattle.Services.Persistence
{
    /// <summary>
    /// Manages a library of maps and supports quick-switching, map linking,
    /// and token transfer between maps.
    /// </summary>
    public class MapLibraryService
    {
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>All known map references.</summary>
        public List<TileMapReference> Maps { get; set; } = new();

        /// <summary>The currently loaded map.</summary>
        public TileMap CurrentMap { get; private set; }

        /// <summary>Recently used maps (most recent first).</summary>
        public IReadOnlyList<TileMapReference> RecentMaps =>
            Maps.OrderByDescending(m => m.LastUsed).Take(Options.MapLibraryMaxRecent).ToList();

        /// <summary>Raised when the active map changes.</summary>
        public event Action<TileMap> MapChanged;

        // ── Map lifecycle ──

        /// <summary>Add a new map reference to the library.</summary>
        public void AddMap(TileMapReference mapRef)
        {
            if (mapRef == null) return;
            if (Maps.Any(m => m.Id == mapRef.Id)) return;
            Maps.Add(mapRef);
        }

        /// <summary>Remove a map reference from the library.</summary>
        public void RemoveMap(string mapId)
        {
            Maps.RemoveAll(m => m.Id == mapId);
        }

        /// <summary>
        /// Load a map by its library ID. Saves the current map state first.
        /// </summary>
        public void LoadMap(string mapId)
        {
            if (!Options.EnableMultiMapManagement) return;

            var mapRef = Maps.FirstOrDefault(m => m.Id == mapId);
            if (mapRef == null) return;

            // Save current state
            if (CurrentMap != null)
                SaveCurrentMapState();

            // Load new map from file
            if (!string.IsNullOrEmpty(mapRef.FilePath) && File.Exists(mapRef.FilePath))
            {
                var json = File.ReadAllText(mapRef.FilePath);
                CurrentMap = JsonSerializer.Deserialize<TileMap>(json, _jsonOpts) ?? new TileMap();
            }
            else
            {
                CurrentMap = new TileMap { Name = mapRef.Name };
            }

            mapRef.LastUsed = DateTime.Now;
            MapChanged?.Invoke(CurrentMap);
        }

        /// <summary>Save the current map's JSON to its reference path.</summary>
        public void SaveCurrentMapState()
        {
            if (CurrentMap == null) return;

            var mapRef = Maps.FirstOrDefault(m => m.Id == CurrentMap.Id.ToString());
            if (mapRef == null) return;

            if (string.IsNullOrEmpty(mapRef.FilePath)) return;

            var dir = Path.GetDirectoryName(mapRef.FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(CurrentMap, _jsonOpts);
            File.WriteAllText(mapRef.FilePath, json);
        }

        /// <summary>Set the current map without file I/O (in-memory switch).</summary>
        public void SetCurrentMap(TileMap map)
        {
            CurrentMap = map;
        }

        // ── Map link helpers ──

        /// <summary>Create a link on a tile pointing to a target map and position.</summary>
        public static MapLink CreateMapLink(string targetMapId, int targetX, int targetY)
        {
            return new MapLink
            {
                TargetMapId = targetMapId,
                TargetX = targetX,
                TargetY = targetY
            };
        }

        // ── Search / filter ──

        /// <summary>Search maps by name or tags.</summary>
        public List<TileMapReference> SearchMaps(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Maps;

            var lower = query.ToLowerInvariant();
            return Maps.Where(m =>
                m.Name.ToLowerInvariant().Contains(lower) ||
                m.Tags.Any(t => t.ToLowerInvariant().Contains(lower))
            ).ToList();
        }

        // ── Persistence of the library index ──

        /// <summary>Save the library index to a JSON file.</summary>
        public void SaveLibraryIndex(string filePath)
        {
            var json = JsonSerializer.Serialize(Maps, _jsonOpts);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, json);
        }

        /// <summary>Load the library index from a JSON file.</summary>
        public void LoadLibraryIndex(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var json = File.ReadAllText(filePath);
            Maps = JsonSerializer.Deserialize<List<TileMapReference>>(json, _jsonOpts) ?? new();
        }
    }
}
