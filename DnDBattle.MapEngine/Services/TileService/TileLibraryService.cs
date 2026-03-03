using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
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

namespace DnDBattle.Services.TileService
{
    public class TileLibraryService
    {
        private static readonly Lazy<TileLibraryService> _instance =
            new Lazy<TileLibraryService>(() => new TileLibraryService());

        public static TileLibraryService Instance => _instance.Value;

        public ObservableCollection<TileDefinition> AvailableTiles { get; private set; }

        private readonly string _tileDirectory;

        private TileLibraryService()
        {
            AvailableTiles = new ObservableCollection<TileDefinition>();
            _tileDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Tiles");

            Directory.CreateDirectory(_tileDirectory);
        }

        public void LoadTileLibrary()
        {
            AvailableTiles.Clear();

            if (!Directory.Exists(_tileDirectory))
            {
                // Addition: Maybe add some way for the user to manually set the folder?
                Debug.WriteLine($"[TileLibrary] Tiles directory not found: {_tileDirectory}");
                return;
            }

            // Addition: Maybe more file formats?
            var imageFiles = Directory.GetFiles(_tileDirectory, "*.png", SearchOption.AllDirectories);

            Debug.WriteLine($"[TileLibrary] Found {imageFiles.Length} tile images");

            foreach (var filePath in imageFiles)
            {
                try
                {
                    var relativePath = GetRelativePath(filePath);
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var category = GetCategoryFromPath(filePath);

                    var tileDef = new TileDefinition()
                    {
                        Id = GenerateDeterministicId(relativePath),
                        ImagePath = relativePath,
                        DisplayName = FormatDisplayName(fileName),
                        Category = category,
                        IsEnabled = true
                    };

                    AvailableTiles.Add(tileDef);

                    TileImageCacheService.Instance.GetOrLoadImage(relativePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TileLibrary] Error loading tile {filePath}: {ex.Message}");
                }
            }
            Debug.WriteLine($"[TileLibrary] Loaded {AvailableTiles.Count} tiles int library");
        }

        public void RefreshLibrary()
        {
            LoadTileLibrary();
        }

        public Dictionary<string, List<TileDefinition>> GetTilesByCategory()
        {
            return AvailableTiles
                .GroupBy(t => t.Category ?? "General")
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public TileDefinition GetTileById(string id)
        {
            return AvailableTiles.FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// Finds a tile definition by its image path.
        /// Used as a fallback when ID lookup fails (e.g., maps saved with non-deterministic IDs).
        /// </summary>
        public TileDefinition GetTileByImagePath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return null;
            return AvailableTiles.FirstOrDefault(t =>
                string.Equals(t.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Generates a deterministic ID from the relative image path so the same
        /// tile file always receives the same ID across library reloads.
        /// </summary>
        private static string GenerateDeterministicId(string relativePath)
        {
            var normalized = relativePath.Replace('\\', '/').ToLowerInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            // Use first 16 bytes to form a GUID-shaped string for compatibility
            var guid = new Guid(hash.Take(16).ToArray());
            return guid.ToString();
        }

        private string GetRelativePath(string absolutePath)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (absolutePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                return absolutePath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            return absolutePath;
        }

        private string GetCategoryFromPath(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (dir == null) return "General";

            var parts = dir.Split(Path.DirectorySeparatorChar);

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Equals("Tiles", StringComparison.OrdinalIgnoreCase))
                {
                    return parts[i + 1];
                }
            }
            return "General";
        }

        private string FormatDisplayName(string fileName)
        {
            return fileName
                .Replace("_", " ")
                .Replace("-", " ")
                .Trim();
        }
    }
}
