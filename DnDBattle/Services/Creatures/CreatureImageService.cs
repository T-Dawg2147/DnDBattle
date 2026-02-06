using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Services.Creatures;

namespace DnDBattle.Services.Creatures
{
    public class CreatureImageService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _cacheFolder;
        private static readonly Dictionary<string, ImageSource> _memoryCache = new Dictionary<string, ImageSource>();

        static CreatureImageService()
        {
            _cacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DnDBattle",
                "ImageCache");

            if (!Directory.Exists(_cacheFolder))
                Directory.CreateDirectory(_cacheFolder);

            _httpClient.DefaultRequestHeaders.Add("User_Agent", "DnDBattle/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public static async Task<ImageSource> GetCreatureImageAsync(
            string creatureName,
            string creatureType,
            string size,
            string challengeRating,
            string existingIconPath = null)
        {
            if (!string.IsNullOrEmpty(existingIconPath) && File.Exists(existingIconPath))
            {
                try
                {
                    return LoadImageFromFile(existingIconPath);
                }
                catch { /* Fall through to other methods */ }
            }

            // 2. Check memory cache
            string cacheKey = GetCacheKey(creatureName);
            if (_memoryCache.TryGetValue(cacheKey, out var cachedImage))
            {
                return cachedImage;
            }

            // 3. Check disk cache
            string diskCachePath = GetDiskCachePath(creatureName);
            if (File.Exists(diskCachePath))
            {
                try
                {
                    var image = LoadImageFromFile(diskCachePath);
                    _memoryCache[cacheKey] = image;
                    return image;
                }
                catch { /* Fall through */ }
            }

            // 4. Try Open5e API
            try
            {
                var open5eImage = await FetchFromOpen5eAsync(creatureName);
                if (open5eImage != null)
                {
                    _memoryCache[cacheKey] = open5eImage;
                    return open5eImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Open5e fetch failed for {creatureName}: {ex.Message}");
            }

            // 5. Generate placeholder
            var placeholder = GeneratePlaceholderToken(creatureName, creatureType, size, challengeRating);
            _memoryCache[cacheKey] = placeholder;
            return placeholder;
        }

        public static ImageSource GetCreatureImageSync(
            string creatureName,
            string creatureType,
            string size,
            string challengeRating,
            string existingIconPath = null)
        {
            if (!string.IsNullOrEmpty(existingIconPath) && File.Exists(existingIconPath))
            {
                try { return LoadImageFromFile(existingIconPath); }
                catch { }
            }

            string cacheKey = GetCacheKey(creatureName);
            if (_memoryCache.TryGetValue(cacheKey, out var cachedImage))
                return cachedImage;

            string diskCachePath = GetDiskCachePath(creatureName);
            if (File.Exists(diskCachePath))
            {
                try
                {
                    var image = LoadImageFromFile(diskCachePath);
                    _memoryCache[cacheKey] = image;
                    return image;
                }
                catch { }
            }

            // Return placeholder immediately, fetch in background
            var placeholder = GeneratePlaceholderToken(creatureName, creatureType, size, challengeRating);
            _memoryCache[cacheKey] = placeholder;

            // Fire and forget - fetch real image in background
            _ = Task.Run(async () =>
            {
                try
                {
                    var open5eImage = await FetchFromOpen5eAsync(creatureName);
                    if (open5eImage != null)
                    {
                        _memoryCache[cacheKey] = open5eImage;
                        // Notify UI to refresh if needed
                        ImageFetched?.Invoke(creatureName, open5eImage);
                    }
                }
                catch { }
            });

            return placeholder;
        }

        public static event Action<string, ImageSource> ImageFetched;

        #region Open5e API

        private static async Task<ImageSource> FetchFromOpen5eAsync(string creatureName)
        {
            string searchName = NormalizeNameForApi(creatureName);

            string searchUrl = $"https://api.open5e.com/v1/monsters/?search={Uri.EscapeDataString(searchName)}&limit=5";

            var response = await _httpClient.GetAsync(searchUrl);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<Open5eSearchResult>(json);

            if (searchResult?.Results == null || searchResult.Results.Count == 0)
                return null;

            var monster = FindBestMatch(searchResult.Results, creatureName);
            if (monster == null)
                return null;

            string imageUrl = monster.ImageUrl;
            if (string.IsNullOrEmpty(imageUrl))
                imageUrl = monster.ImgMain;

            if (string.IsNullOrEmpty(imageUrl))
                return null;

            return await DownloadAndCacheImageAsync(creatureName, imageUrl);
        }

        private static Open5eMonster FindBestMatch(List<Open5eMonster> monsters, string creatureName)
        {
            string normalizedSearch = creatureName.ToLower().Trim();

            foreach (var monster in monsters)
            {
                if (monster.Name?.ToLower().Trim() == normalizedSearch)
                    return monster;
            }

            foreach (var monster in monsters)
            {
                if (monster.Name?.ToLower().Contains(normalizedSearch) == true ||
                    normalizedSearch.Contains(monster.Name?.ToLower() ?? ""))
                    return monster;
            }

            return monsters.Count > 0 ? monsters[0] : null;
        }

        private static async Task<ImageSource> DownloadAndCacheImageAsync(string creatureName, string imageUrl)
        {
            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);

                string cachePath = GetDiskCachePath(creatureName);
                await File.WriteAllBytesAsync(cachePath, imageBytes);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to download image: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Placeholder Generation
        public static ImageSource GeneratePlaceholderToken(
            string creatureName,
            string creatureType,
            string size,
            string challengeRating)
        {
            int imageSize = 128;
            var visual = new DrawingVisual();
            
            using (var dc = visual.RenderOpen())
            {
                var (bgColor, accentColor, icon) = GetTypeStyle(creatureType);

                var bgBrush = new RadialGradientBrush(
                    Color.FromArgb(255, bgColor.R, bgColor.G, bgColor.B),
                    Color.FromArgb(255,
                        (byte)Math.Max(0, bgColor.R - 40),
                        (byte)Math.Max(0, bgColor.G - 40),
                        (byte)Math.Max(0, bgColor.B - 40)));
                bgBrush.Freeze();

                // Draw circular background
                dc.DrawEllipse(bgBrush, null, new System.Windows.Point(imageSize / 2, imageSize / 2), imageSize / 2 - 4, imageSize / 2 - 4);

                // Border base on CR
                double crValue = ParseCR(challengeRating);
                var borderBrush = GetCRBorderBrush(crValue);
                var borderPen = new Pen(borderBrush, crValue >= 10 ? 4 : 3);
                borderPen.Freeze();
                dc.DrawEllipse(null, borderPen, new System.Windows.Point(imageSize / 2, imageSize / 2), imageSize / 2 - 4, imageSize / 2 - 4);

                // Inner glow for legendary creatures (CR 17+)
                if (crValue >= 17)
                {
                    var glowBrush = new RadialGradientBrush(
                        Color.FromArgb(60, 255, 215, 0),
                        Color.FromArgb(0, 255, 215, 0));
                    glowBrush.Freeze();
                    dc.DrawEllipse(glowBrush, null, new System.Windows.Point(imageSize / 2, imageSize / 2), imageSize / 2 - 8, imageSize / 2 - 8);
                }

                // Type icon at top
                var iconText = new FormattedText(
                    icon,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI Emoji"),
                    20, Brushes.White,
                    1.0);
                dc.DrawText(iconText, new System.Windows.Point((imageSize - iconText.Width) / 2, 12));

                // Creature initial(s) in center
                string initials = GetInitials(creatureName);
                var initialsText = new FormattedText(
                    initials,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    initials.Length > 2 ? 28 : 36,
                    Brushes.White,
                    1.0);
                dc.DrawText(initialsText, new Point(
                    (imageSize - initialsText.Width) / 2,
                    (imageSize - initialsText.Height) / 2 + 5));

                string sizeIndicator = GetSizeIndicator(size);
                var sizeText = new FormattedText(
                    sizeIndicator,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    10, new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    1.0);
                dc.DrawText(sizeText, new Point((imageSize - sizeText.Width) / 2, imageSize - 24));
            }

            var rtb = new RenderTargetBitmap(imageSize, imageSize, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();

            return rtb;
        }

        private static (Color bg, Color accent, string icon) GetTypeStyle(string creatureType)
        {
            string type = creatureType?.ToLower() ?? "";

            return type switch
            {
                var t when t.Contains("aberration") => (Color.FromRgb(75, 0, 130), Color.FromRgb(138, 43, 226), "👁️"),
                var t when t.Contains("beast") => (Color.FromRgb(34, 139, 34), Color.FromRgb(50, 205, 50), "🐾"),
                var t when t.Contains("celestial") => (Color.FromRgb(255, 215, 0), Color.FromRgb(255, 255, 200), "✨"),
                var t when t.Contains("construct") => (Color.FromRgb(119, 136, 153), Color.FromRgb(176, 196, 222), "⚙️"),
                var t when t.Contains("dragon") => (Color.FromRgb(139, 0, 0), Color.FromRgb(255, 69, 0), "🐲"),
                var t when t.Contains("elemental") => (Color.FromRgb(255, 140, 0), Color.FromRgb(255, 200, 100), "🔥"),
                var t when t.Contains("fey") => (Color.FromRgb(219, 112, 147), Color.FromRgb(255, 182, 193), "🦋"),
                var t when t.Contains("fiend") => (Color.FromRgb(139, 0, 0), Color.FromRgb(255, 0, 0), "😈"),
                var t when t.Contains("giant") => (Color.FromRgb(139, 90, 43), Color.FromRgb(210, 180, 140), "👊"),
                var t when t.Contains("humanoid") => (Color.FromRgb(70, 130, 180), Color.FromRgb(135, 206, 250), "👤"),
                var t when t.Contains("monstrosity") => (Color.FromRgb(85, 107, 47), Color.FromRgb(107, 142, 35), "🦎"),
                var t when t.Contains("ooze") => (Color.FromRgb(50, 205, 50), Color.FromRgb(173, 255, 47), "💧"),
                var t when t.Contains("plant") => (Color.FromRgb(0, 100, 0), Color.FromRgb(34, 139, 34), "🌿"),
                var t when t.Contains("undead") => (Color.FromRgb(47, 79, 79), Color.FromRgb(72, 61, 139), "💀"),
                var t when t.Contains("swarm") => (Color.FromRgb(60, 60, 60), Color.FromRgb(100, 100, 100), "🐀"),
                _ => (Color.FromRgb(80, 80, 80), Color.FromRgb(150, 150, 150), "❓")
            };
        }

        private static Brush GetCRBorderBrush(double cr)
        {
            if (cr >= 20) return new SolidColorBrush(Color.FromRgb(255, 215, 0));   // Gold - Legendary
            if (cr >= 15) return new SolidColorBrush(Color.FromRgb(255, 140, 0));   // Orange - Very High
            if (cr >= 10) return new SolidColorBrush(Color.FromRgb(186, 85, 211));  // Purple - High
            if (cr >= 5) return new SolidColorBrush(Color.FromRgb(65, 105, 225));   //Blue - Medium
            if (cr >= 1) return new SolidColorBrush(Color.FromRgb(46, 139, 87));    // Green - Low
            return new SolidColorBrush(Color.FromRgb(128, 128, 128));   // Grey - Very Low
        }

        private static double ParseCR(string cr)
        {
            if (string.IsNullOrEmpty(cr)) return 0;
            cr = cr.Trim();
            if (cr == "1/8") return 0.125;
            if (cr == "1/4") return 0.25;
            if (cr == "1/2") return 0.5;
            if (double.TryParse(cr, out double result)) return result;
            return 0;

        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";

            var words = name.Split(new[] { ' ', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1)
            {
                return words[0].Length >= 2
                    ? words[0].Substring(0, 2).ToUpper()
                    : words[0].ToUpper();
            }

            string initials = "";
            for (int i = 0; i < Math.Min(2, words.Length); i++)
            {
                if (words[i].Length > 0)
                    initials += char.ToUpper(words[i][0]);
            }
            return initials;
        }

        private static string GetSizeIndicator(string size)
        {
            return size?.ToLower() switch
            {
                "tiny" => "T",
                "small" => "S",
                "medium" => "M",
                "large" => "L",
                "huge" => "H",
                "gargantuan" => "G",
                _ => "M"
            };
        }

        #endregion

        #region Helpers

        private static string GetCacheKey(string creatureName) =>
            creatureName?.ToLower().Trim() ?? "unknown";

        private static string GetDiskCachePath(string creatureName)
        {
            string safeName = string.Join("_", creatureName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_cacheFolder, $"{safeName}.png");
        }

        private static string NormalizeNameForApi(string name)
        {
            name = name.Trim();
            return name.ToLower();
        }

        private static ImageSource LoadImageFromFile(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        public static string SaveUploadedImage(string creatureName, string sourceImagePath)
        {
            try
            {
                string destPath = GetDiskCachePath(creatureName);
                File.Copy(sourceImagePath, destPath, true);

                string cacheKey = GetCacheKey(creatureName);
                _memoryCache.Remove(cacheKey);

                return destPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save uploaded image: {ex.Message}");
                return null;
            }
        }

        public static void ClearCache(string creatureName)
        {
            string cacheKey = GetCacheKey(creatureName);
            _memoryCache.Remove(cacheKey);

            string diskPath = GetDiskCachePath(creatureName);
            if (File.Exists(diskPath))
            {
                try { File.Delete(diskPath); }
                catch { }
            }
        }

        public static async Task PreloadImagesAsync(IEnumerable<(string name, string type, string size, string cr)> creatures)
        {
            foreach (var creature in creatures)
            {
                await GetCreatureImageAsync(creature.name, creature.type, creature.size, creature.cr);
                await Task.Delay(100);
            }
        }
        #endregion
    }

    #region Open5e API Models
    
    public class Open5eSearchResult
    {
        public int Count { get; set; }
        public List<Open5eMonster> Results { get; set; }
    }

    public class Open5eMonster
    {
        public string Slug { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string Alignment { get; set; }
        public int ArmorClass { get; set; }
        public int HitPoints { get; set; }
        public string ChallengeRating { get; set; }

        // Image fields - Open5e uses different field names
        public string ImageUrl { get; set; }
        public string ImgMain { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("img_main")]
        public string ImgMainAlt { get => ImgMain; set => ImgMain = value; }
    }
    #endregion
}
