using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                Debug.WriteLine($"[TileLibrary] Tiles directory not found: {_tileDirectory}");
                return;
            }

            // Load all image files
            var imageFiles = Directory.GetFiles(_tileDirectory, "*.png", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(_tileDirectory, "*.jpg", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(_tileDirectory, "*.jpeg", SearchOption.AllDirectories))
                .ToArray();

            Debug.WriteLine($"[TileLibrary] Found {imageFiles.Length} tile images");

            foreach (var filePath in imageFiles)
            {
                try
                {
                    var relativePath = GetRelativePath(filePath);
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var category = GetCategoryFromPath(filePath);

                    // CREATE DETERMINISTIC ID from relative path
                    var tileDef = new TileDefinition()
                    {
                        Id = GenerateDeterministicId(relativePath), // ← CONSISTENT ID!
                        ImagePath = relativePath,
                        Name = FormatDisplayName(fileName),
                        Category = category,
                        IsEnabled = true
                    };

                    AvailableTiles.Add(tileDef);

                    Debug.WriteLine($"[TileLibrary] Loaded: {tileDef.Id} → {relativePath}");

                    // Pre-load image into cache
                    TileImageCacheService.Instance.GetOrLoadImage(relativePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TileLibrary] Error loading tile {filePath}: {ex.Message}");
                }
            }

            Debug.WriteLine($"[TileLibrary] Loaded {AvailableTiles.Count} tiles into library");
        }

        public void RefreshLibrary()
        {
            LoadTileLibrary();
        }

        #region Helpers

        /// <summary>
        /// Generates a deterministic GUID from a string (file path)
        /// Same input always produces the same output
        /// </summary>
        private string GenerateDeterministicId(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                // Normalize path separators
                var normalized = input.Replace("\\", "/").ToLowerInvariant();

                // Generate hash
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(normalized);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert to GUID format
                var guid = new Guid(hashBytes);
                return guid.ToString();
            }
        }

        public Dictionary<string, List<TileDefinition>> GetTilesByCategory()
        {
            return AvailableTiles
                .GroupBy(t => t.Category ?? "General")
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public TileDefinition GetTileById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return AvailableTiles.FirstOrDefault(t => t.Id.ToString() == id || t.Id == id);
        }

        public TileDefinition GetTileById(Guid id)
        {
            return GetTileById(id.ToString());
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

        #endregion
    }
}
