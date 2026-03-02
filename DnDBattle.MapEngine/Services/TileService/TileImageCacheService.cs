using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;

namespace DnDBattle.Services.TileService
{
    public class TileImageCacheService
    {
        private static readonly Lazy<TileImageCacheService> _instance =
            new Lazy<TileImageCacheService>(() => new TileImageCacheService());

        public static TileImageCacheService Instance = _instance.Value;

        private readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        private readonly object _cacheLock = new object();

        private TileImageCacheService() { }

        public BitmapImage GetOrLoadImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return null;

            lock (_cacheLock)
            {
                if (_imageCache.TryGetValue(imagePath, out var cachedImage))
                    return cachedImage;

                try
                {
                    var fullPath = GetFullPath(imagePath);
                    if (!File.Exists(fullPath))
                    {
                        Debug.WriteLine($"[TileImageCache] Image not found: {fullPath}");
                        return null;
                    }

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    _imageCache[imagePath] = bitmap;
                    Debug.WriteLine($"[TileImageCache] Loaded and cached: {imagePath}");
                    return bitmap;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TileImageCache] Failed to load {imagePath}: {ex.Message}");
                    return null;
                }
            }
        }

        public void PreloadImages(IEnumerable<string> imagePaths)
        {
            foreach (var path in imagePaths)
            {
                GetOrLoadImage(path);
            }
        }

        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _imageCache.Clear();
                Debug.WriteLine("[TileImageCache] Cache cleared");
            }
        }

        public void RemoveFromCache(string imagePath)
        {
            lock (_cacheLock)
            {
                if (_imageCache.Remove(imagePath))
                {
                    Debug.WriteLine($"[TileImageCache] Removed from cache: {imagePath}");
                }
            }
        }

        public (int Count, long EstimatedMemoryBytes) GetCacheStats()
        {
            lock (_cacheLock)
            {
                long memoryEstimate = 0;
                foreach (var img in _imageCache.Values)
                {
                    if (img != null)
                    {
                        memoryEstimate += img.PixelWidth * img.PixelHeight * 4;
                    }
                }
                return (_imageCache.Count, memoryEstimate);
            }
        }

        private string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }
    }
}
