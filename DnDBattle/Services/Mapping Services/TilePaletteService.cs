using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DnDBattle.Services.Mapping_Services
{
    public class TilePaletteService
    {
        private readonly string _tilesFolder;
        private readonly string _metadataPath;
        private readonly TileImageCacheService _imageCache;

        private List<TileDefinition> _tileDefinitions = new List<TileDefinition>();
        private TileMetadata _metadata;

        public event Action TilesReloaded;

        public TilePaletteService()
        {
            _tilesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Tiles");
            _metadataPath = Path.Combine(_tilesFolder, "tiles_metadata.json");
            _imageCache = TileImageCacheService.Instance;

            EnsureTilesFolderExists();
        }

        public TilePaletteService(string customTilesFolder) : this()
        {
            if (!string.IsNullOrEmpty(customTilesFolder) && Directory.Exists(customTilesFolder))
            {
                _tilesFolder = customTilesFolder;
                _metadataPath = Path.Combine(_tilesFolder, "tiles.metadata.json");
            }
        }

        public IReadOnlyList<TileDefinition> TileDefinitions => _tileDefinitions.AsReadOnly();

        public ILookup<string, TileDefinition> TileByCategory =>
            _tileDefinitions.ToLookup(t => t.Category ?? "Uncategorized");

        public IEnumerable<string> Categories =>
            _tileDefinitions.Select(t => t.Category).Distinct().OrderBy(c => c);

        public async Task LoadTilesAsync()
        {
            _tileDefinitions.Clear();

            await LoadMetadataAsync();

            if (!Directory.Exists(_tilesFolder))
            {
                Debug.WriteLine($"[TilePalette] Tiles folder does not exist: {_tilesFolder}");
                return;
            }

            var pngFiles = Directory.GetFiles(_tilesFolder, "*.png", SearchOption.AllDirectories);
            Debug.WriteLine($"[TilePalette] Found {pngFiles.Length} PNG files");

            foreach (var filePath in pngFiles)
            {
                var definition = CreateTileDefinition(filePath);
                _tileDefinitions.Add(definition);
            }

            await _imageCache.PreloadImagesAsync(_tileDefinitions.Select(t => t.FilePath));

            foreach (var def in _tileDefinitions)
            {
                def.CachedImage = _imageCache.GetImage(def.FilePath);
            }

            TilesReloaded?.Invoke();
        }

        public async Task RefreshAsync()
        {
            await LoadTilesAsync();
        }

        public TileDefinition GetTileDefinition(string id)
        {
            return _tileDefinitions.FirstOrDefault(t =>
                t.Id == id || t.FilePath.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public void OpenTilesFolder()
        {
            EnsureTilesFolderExists();
            Process.Start("explorer.exe", _tilesFolder);
        }

        private TileDefinition CreateTileDefinition(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var relativePath = Path.GetRelativePath(_tilesFolder, filePath);
            var folderName = Path.GetDirectoryName(relativePath);

            var metaEntry = _metadata?.Tiles?.FirstOrDefault(t =>
                t.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                t.FileName.Equals(relativePath, StringComparison.OrdinalIgnoreCase));

            return new TileDefinition()
            {
                Id = filePath,
                FilePath = filePath,
                Name = metaEntry?.DisplayName ?? FormatDisplayName(fileName),
                Category = metaEntry?.Category ?? InferCategory(folderName, fileName),
                WidthInSquares = metaEntry?.WidthInSquares ?? 1,
                HeightInSquares = metaEntry?.HeightInSquares ?? 1,
                BlockMovement = metaEntry?.BlocksMovement ?? false,
                BlockLineOfSight = metaEntry?.BlocksLineOfSight ?? false
            };
        }

        public string FormatDisplayName(string fileName)
        {
            return string.Join(" ", fileName
                .Replace("_", " ")
                .Replace("-", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }

        private string InferCategory(string folderName, string fileName)
        {
            var name = (folderName + " " + fileName).ToLowerInvariant();

            if (name.Contains("floor") || name.Contains("ground")) return "Floor";
            if (name.Contains("wall")) return "Wall";
            if (name.Contains("door")) return "Door";
            if (name.Contains("window")) return "Window";
            if (name.Contains("water") || name.Contains("river") || name.Contains("lake")) return "Water";
            if (name.Contains("tree") || name.Contains("bush") || name.Contains("plant")) return "Vegetation";
            if (name.Contains("furniture") || name.Contains("table") || name.Contains("chair")) return "Furniture";
            if (name.Contains("decoration") || name.Contains("decor")) return "Decoration";

            return string.IsNullOrEmpty(folderName) ? "Uncategorized" : FormatDisplayName(folderName);
        }

        private async Task LoadMetadataAsync()
        {
            if (!File.Exists(_metadataPath))
            {
                _metadata = null;
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_metadataPath);
                _metadata = JsonSerializer.Deserialize<TileMetadata>(json, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TilePalette] Error loading metadata: {ex.Message}");
                _metadata = null;
            }
        }

        private void EnsureTilesFolderExists()
        {
            if (!Directory.Exists(_tilesFolder))
            {
                Directory.CreateDirectory(_tilesFolder);
                Debug.WriteLine($"[TilePalette] Create tiles folder: {_tilesFolder}");
            }
        }
    }

    #region Metadata DTOs

    public class TileMetadata
    {
        public List<TileMetadataEntry> Tiles { get; set; } = new List<TileMetadataEntry>();
    }

    public class TileMetadataEntry
    {
        public string FileName { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public int WidthInSquares { get; set; } = 1;
        public int HeightInSquares { get; set; } = 1;
        public bool BlocksMovement { get; set; } = false;
        public bool BlocksLineOfSight { get; set; } = false;
    }

    #endregion
}
