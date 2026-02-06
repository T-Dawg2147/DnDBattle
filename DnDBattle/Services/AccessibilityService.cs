using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Services
{
    /// <summary>
    /// Manages accessibility features including high contrast, colorblind modes,
    /// UI scaling, dyslexia-friendly fonts, and keyboard navigation.
    /// </summary>
    public sealed class AccessibilityService
    {
        // ── Colorblind palette cache (pre-computed, frozen brushes) ──
        private static readonly Dictionary<ColorblindMode, ColorPalette> _palettes = new()
        {
            [ColorblindMode.None] = new ColorPalette
            {
                Danger = "#F44336",
                Safe = "#4CAF50",
                Warning = "#FFB74D",
                Info = "#4FC3F7",
                Accent = "#BA68C8",
                Highlight = "#FFD700"
            },
            [ColorblindMode.Protanopia] = new ColorPalette
            {
                Danger = "#D4A017",  // Yellow-orange instead of red
                Safe = "#4682B4",    // Steel blue instead of green
                Warning = "#FFD700", // Gold
                Info = "#87CEEB",    // Light blue
                Accent = "#9370DB",  // Medium purple
                Highlight = "#FFA500"
            },
            [ColorblindMode.Deuteranopia] = new ColorPalette
            {
                Danger = "#CC6600",  // Dark orange instead of red
                Safe = "#3399FF",    // Bright blue instead of green
                Warning = "#FFCC00", // Yellow
                Info = "#66B2FF",    // Light blue
                Accent = "#CC66FF",  // Light purple
                Highlight = "#FF9933"
            },
            [ColorblindMode.Tritanopia] = new ColorPalette
            {
                Danger = "#FF4444",  // Red (preserved)
                Safe = "#FF8888",    // Light red/pink instead of green
                Warning = "#FF6666", // Pinkish red
                Info = "#CCCCCC",    // Gray instead of blue
                Accent = "#FF44FF",  // Magenta
                Highlight = "#FFAAAA"
            }
        };

        /// <summary>Gets the current colorblind mode.</summary>
        public ColorblindMode CurrentColorblindMode { get; private set; } = ColorblindMode.None;

        /// <summary>Gets the current UI scale factor (1.0 = 100%).</summary>
        public double UIScaleFactor => Options.AccessibilityUIScale / 100.0;

        /// <summary>
        /// Gets the color palette for the current colorblind mode.
        /// Palettes are pre-computed and cached.
        /// </summary>
        public ColorPalette GetCurrentPalette()
        {
            var mode = Options.EnableColorblindMode ? CurrentColorblindMode : ColorblindMode.None;
            return _palettes.TryGetValue(mode, out var p) ? p : _palettes[ColorblindMode.None];
        }

        /// <summary>
        /// Sets the colorblind mode.
        /// </summary>
        public void SetColorblindMode(ColorblindMode mode)
        {
            CurrentColorblindMode = mode;
        }

        /// <summary>
        /// Gets the appropriate font family for accessibility settings.
        /// Uses dyslexia-friendly font if enabled, otherwise system default.
        /// </summary>
        public FontFamily GetAccessibleFontFamily()
        {
            if (Options.EnableDyslexiaFont)
            {
                // OpenDyslexic is the standard dyslexia-friendly font
                // Falls back to Verdana (widely available, good readability)
                return new FontFamily("OpenDyslexic, Verdana, Segoe UI");
            }
            return new FontFamily("Segoe UI");
        }

        /// <summary>
        /// Gets the adjusted font size based on UI scale.
        /// </summary>
        public double GetScaledFontSize(double baseFontSize)
        {
            return baseFontSize * UIScaleFactor;
        }

        /// <summary>
        /// Gets high-contrast color variants.
        /// In high contrast mode, colors are replaced with maximum contrast alternatives.
        /// </summary>
        public Color GetHighContrastColor(Color normalColor)
        {
            if (!Options.EnableHighContrast) return normalColor;

            // In high contrast, map to either black or white based on luminance
            double luminance = 0.299 * normalColor.R + 0.587 * normalColor.G + 0.114 * normalColor.B;
            return luminance > 128 ? Colors.White : Colors.Black;
        }

        /// <summary>
        /// Gets the background color for high contrast mode.
        /// </summary>
        public Color GetHighContrastBackground()
        {
            return Options.EnableHighContrast ? Colors.Black : Color.FromRgb(0x1E, 0x1E, 0x2E);
        }

        /// <summary>
        /// Gets the foreground (text) color for high contrast mode.
        /// </summary>
        public Color GetHighContrastForeground()
        {
            return Options.EnableHighContrast ? Colors.White : Color.FromRgb(0xCC, 0xCC, 0xDD);
        }

        /// <summary>
        /// Creates an accessible ScaleTransform for the UI based on current scale settings.
        /// </summary>
        public ScaleTransform GetUIScaleTransform()
        {
            double scale = UIScaleFactor;
            return new ScaleTransform(scale, scale);
        }

        /// <summary>
        /// Generates a screen-reader-friendly text description for a token.
        /// </summary>
        public static string DescribeToken(string name, int hp, int maxHp, int gridX, int gridY, IEnumerable<string>? conditions = null)
        {
            var desc = $"{name}, {hp} of {maxHp} hit points, at grid position {gridX},{gridY}";
            if (conditions != null)
            {
                string condList = string.Join(", ", conditions);
                if (!string.IsNullOrEmpty(condList))
                    desc += $", conditions: {condList}";
            }
            return desc;
        }

        /// <summary>
        /// Generates a screen-reader-friendly text description for a dice roll.
        /// </summary>
        public static string DescribeRoll(string expression, int result, bool isCritical = false)
        {
            string desc = $"Rolled {expression}, result: {result}";
            if (isCritical) desc += ", critical!";
            return desc;
        }

        /// <summary>
        /// Gets all available colorblind modes for UI population.
        /// </summary>
        public static IReadOnlyList<(ColorblindMode Mode, string DisplayName)> GetColorblindModes()
        {
            return new[]
            {
                (ColorblindMode.None, "Normal Vision"),
                (ColorblindMode.Protanopia, "Protanopia (Red-Blind)"),
                (ColorblindMode.Deuteranopia, "Deuteranopia (Green-Blind)"),
                (ColorblindMode.Tritanopia, "Tritanopia (Blue-Blind)")
            };
        }
    }

    /// <summary>
    /// Colorblind vision simulation modes.
    /// </summary>
    public enum ColorblindMode
    {
        None,
        Protanopia,   // Red-blind
        Deuteranopia, // Green-blind
        Tritanopia    // Blue-blind
    }

    /// <summary>
    /// Pre-computed color palette for a colorblind mode.
    /// All colors stored as hex strings for easy serialization.
    /// </summary>
    public class ColorPalette
    {
        public string Danger { get; set; } = "#F44336";
        public string Safe { get; set; } = "#4CAF50";
        public string Warning { get; set; } = "#FFB74D";
        public string Info { get; set; } = "#4FC3F7";
        public string Accent { get; set; } = "#BA68C8";
        public string Highlight { get; set; } = "#FFD700";

        /// <summary>Creates a frozen SolidColorBrush from a hex color.</summary>
        public SolidColorBrush ToBrush(string hex)
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                brush.Freeze();
                return brush;
            }
            catch
            {
                var brush = new SolidColorBrush(Colors.White);
                brush.Freeze();
                return brush;
            }
        }
    }
}
