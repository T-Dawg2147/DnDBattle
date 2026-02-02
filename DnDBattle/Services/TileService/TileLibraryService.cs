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
                        Id = Guid.NewGuid().ToString(),
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
