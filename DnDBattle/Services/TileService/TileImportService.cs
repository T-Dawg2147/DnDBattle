using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DnDBattle.Services.TileService
{
    public class TileImportService
    {
        private readonly string _tilesDirectory;

        public TileImportService()
        {
            _tilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Tiles");
            Directory.CreateDirectory(_tilesDirectory);
        }

        public async Task<TileDefinition> ImportTileAsync(string sourceFilePath, string category = "Custom", string customName = null)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    Debug.WriteLine($"[TileImport] File not found: {sourceFilePath}");
                    return null;
                }

                var extension = Path.GetExtension(sourceFilePath).ToLower();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    Debug.WriteLine($"[TileImport] Unsupported format: {extension}");
                    return null;
                }

                var categoryPath = Path.Combine(_tilesDirectory, category);
                Directory.CreateDirectory(categoryPath);

                var originalName = Path.GetFileNameWithoutExtension(sourceFilePath);
                var displayName = customName ?? originalName;
                var fileName = SanitizeFileName(displayName) + ".png";
                var destPath = Path.Combine(categoryPath, fileName);

                if (extension == ".png")
                {
                    File.Copy(sourceFilePath, destPath, overwrite: true);
                }
                else
                {
                    await ConvertToPngAsync(sourceFilePath, destPath);
                }

                var relativePath = GetRelativePath(destPath);
                var tileDef = new TileDefinition()
                {
                    Id = Guid.NewGuid().ToString(),
                    ImagePath = relativePath,
                    DisplayName = displayName,
                    Category = category,
                    IsEnabled = true
                };

                TileImageCacheService.Instance.GetOrLoadImage(relativePath);

                Debug.WriteLine($"[TileImport] Imported: {displayName} -> {relativePath}");
                return tileDef;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileImport] Error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<TileDefinition>> ImportMultipleTilesAsync(IEnumerable<string> filePaths, string category = "Custom")
        {
            var results = new List<TileDefinition>();

            foreach (var path in filePaths)
            {
                var tile = await ImportTileAsync(path, category);
                if (tile != null)
                {
                    results.Add(tile);
                }
            }
            return results;
        }

        private async Task ConvertToPngAsync(string sourcePath, string destPath)
        {
            await Task.Run(() =>
            {
                var decoder = BitmapDecoder.Create(
                    new Uri(sourcePath),
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0]));

                using (var stream = File.Create(destPath))
                {
                    encoder.Save(stream);
                }
            });
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
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

        public List<string> GetAvailableCategories()
        {
            var categories = new List<string> { "Floor", "Wall", "Door", "Furniture", "Props", "Terrain", "Custom" };

            if (Directory.Exists(_tilesDirectory))
            {
                var existing = Directory.GetDirectories(_tilesDirectory)
                    .Select(Path.GetFileName)
                    .Where(name => !categories.Contains(name));
                categories.AddRange(existing);
            }

            return categories.Distinct().OrderBy(c => c).ToList();
        }
    }
}
