using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DnDBattle.Services.Mapping_Services
{
    public class TileImageCacheService
    {
        private static readonly Lazy<TileImageCacheService> _instance =
            new Lazy<TileImageCacheService>(() => new TileImageCacheService());

        public static TileImageCacheService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, BitmapImage> _cache =
            new ConcurrentDictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);

        private TileImageCacheService() { }

        public BitmapImage GetImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            return _cache.GetOrAdd(filePath, path => LoadAndFreezeImage(path));
        }

        public async Task<BitmapImage> GetImageAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            if (_cache.TryGetValue(filePath, out var cached))
                return cached;

            return await Task.Run(() => GetImage(filePath));
        }

        public async Task PreloadImagesAsync(IEnumerable<string> filePaths)
        {
            var tasks = new List<Task>();
            foreach (var path in filePaths)
                tasks.Add(GetImageAsync(path));

            await Task.WhenAll(tasks);
        }

        public void Invalidate(string filePath)
        {
            _cache.TryRemove(filePath, out _);
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public int CacheSize => _cache.Count;

        private BitmapImage LoadAndFreezeImage(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"[TileImageCache] File not found: {filePath}");
                    return null;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();

                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                bitmap.StreamSource = new MemoryStream(File.ReadAllBytes(filePath));

                bitmap.DecodePixelWidth = 96;
                bitmap.DecodePixelHeight = 96;

                bitmap.EndInit();

                bitmap.Freeze();

                Debug.WriteLine($"[TileImageCache] Loaded and cached: {filePath}");
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileImageCache] Error loading {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}
