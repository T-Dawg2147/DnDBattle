using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Services
{
    /// <summary>
    /// Service for managing application theming
    /// </summary>
    public static class ThemeService
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DnDBattle",
            "theme.settings"
        );

        public static string CurrentTheme { get; private set; } = "Dark";
        public static string CurrentAccentColor { get; private set; } = "#007ACC";

        static ThemeService()
        {
            EnsureDirectoryExists();
            LoadSettings();
        }

        private static void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var lines = File.ReadAllLines(SettingsFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            switch (parts[0].Trim())
                            {
                                case "Theme":
                                    CurrentTheme = parts[1].Trim();
                                    break;
                                case "AccentColor":
                                    CurrentAccentColor = parts[1].Trim();
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Use defaults if settings can't be loaded
                CurrentTheme = "Dark";
                CurrentAccentColor = "#007ACC";
            }
        }

        public static void SaveSettings()
        {
            try
            {
                EnsureDirectoryExists();
                var content = $"Theme={CurrentTheme}\nAccentColor={CurrentAccentColor}";
                File.WriteAllText(SettingsFilePath, content);
            }
            catch (Exception)
            {
                // Silently fail if settings can't be saved
            }
        }

        public static void ApplyTheme(string theme)
        {
            CurrentTheme = theme;
            UpdateApplicationResources();
        }

        public static void ApplyAccentColor(string colorHex)
        {
            CurrentAccentColor = colorHex;
            UpdateAccentColorResources(colorHex);
        }

        public static void ResetToDefaults()
        {
            CurrentTheme = "Dark";
            CurrentAccentColor = "#007ACC";
            UpdateApplicationResources();
            UpdateAccentColorResources(CurrentAccentColor);
        }

        public static void InitializeTheme()
        {
            UpdateApplicationResources();
            UpdateAccentColorResources(CurrentAccentColor);
        }

        private static void UpdateApplicationResources()
        {
            var app = Application.Current;
            if (app == null) return;

            try
            {
                Color darkest, dark, medium, control;

                if (CurrentTheme == "Darker")
                {
                    darkest = (Color)ColorConverter.ConvertFromString("#FF000000");
                    dark = (Color)ColorConverter.ConvertFromString("#FF0D0D0D");
                    medium = (Color)ColorConverter.ConvertFromString("#FF1A1A1A");
                    control = (Color)ColorConverter.ConvertFromString("#FF252526");
                }
                else // Default Dark theme
                {
                    darkest = (Color)ColorConverter.ConvertFromString("#FF0D0D0D");
                    dark = (Color)ColorConverter.ConvertFromString("#FF1A1A1A");
                    medium = (Color)ColorConverter.ConvertFromString("#FF1E1E1E");
                    control = (Color)ColorConverter.ConvertFromString("#FF2D2D30");
                }

                // Update color resources
                app.Resources["Color_Background_Darkest"] = darkest;
                app.Resources["Color_Background_Dark"] = dark;
                app.Resources["Color_Background_Medium"] = medium;
                app.Resources["Color_Background_Control"] = control;

                // Update brush resources
                app.Resources["Brush_Background_Darkest"] = new SolidColorBrush(darkest);
                app.Resources["Brush_Background_Dark"] = new SolidColorBrush(dark);
                app.Resources["Brush_Background_Medium"] = new SolidColorBrush(medium);
                app.Resources["Brush_Background_Control"] = new SolidColorBrush(control);
            }
            catch (Exception)
            {
                // Silently fail if resources can't be updated
            }
        }

        private static void UpdateAccentColorResources(string colorHex)
        {
            var app = Application.Current;
            if (app == null) return;

            try
            {
                var accentColor = (Color)ColorConverter.ConvertFromString(colorHex);
                var hoverColor = LightenColor(accentColor, 0.15);
                var pressedColor = DarkenColor(accentColor, 0.15);

                app.Resources["Color_Accent_Primary"] = accentColor;
                app.Resources["Color_Accent_Hover"] = hoverColor;
                app.Resources["Color_Accent_Pressed"] = pressedColor;

                app.Resources["Brush_Accent_Primary"] = new SolidColorBrush(accentColor);
                app.Resources["Brush_Accent_Hover"] = new SolidColorBrush(hoverColor);
                app.Resources["Brush_Accent_Pressed"] = new SolidColorBrush(pressedColor);
            }
            catch (Exception)
            {
                // Silently fail if resources can't be updated
            }
        }

        private static Color LightenColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Min(255, color.R + (255 - color.R) * factor),
                (byte)Math.Min(255, color.G + (255 - color.G) * factor),
                (byte)Math.Min(255, color.B + (255 - color.B) * factor)
            );
        }

        private static Color DarkenColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Max(0, color.R * (1 - factor)),
                (byte)Math.Max(0, color.G * (1 - factor)),
                (byte)Math.Max(0, color.B * (1 - factor))
            );
        }
    }
}
