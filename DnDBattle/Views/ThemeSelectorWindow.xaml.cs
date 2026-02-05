using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DnDBattle.Views
{
    /// <summary>
    /// Theme Selector window for customizing the application appearance
    /// </summary>
    public partial class ThemeSelectorWindow : Window
    {
        public ThemeSelectorWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Load saved theme settings
            var currentTheme = Services.ThemeService.CurrentTheme;
            var currentAccent = Services.ThemeService.CurrentAccentColor;

            // Set theme radio button
            switch (currentTheme)
            {
                case "Darker":
                    RbDarkerTheme.IsChecked = true;
                    break;
                default:
                    RbDarkTheme.IsChecked = true;
                    break;
            }

            // Set accent color radio button
            SetAccentColorSelection(currentAccent);
            UpdatePreview();
        }

        private void SetAccentColorSelection(string colorHex)
        {
            foreach (var child in ((WrapPanel)RbAccentBlue.Parent).Children)
            {
                if (child is RadioButton rb && rb.Tag?.ToString() == colorHex)
                {
                    rb.IsChecked = true;
                    break;
                }
            }
        }

        private void Theme_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            string theme = RbDarkerTheme.IsChecked == true ? "Darker" : "Dark";
            Services.ThemeService.ApplyTheme(theme);
            UpdatePreview();
        }

        private void AccentColor_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (sender is RadioButton rb && rb.Tag is string colorHex)
            {
                Services.ThemeService.ApplyAccentColor(colorHex);
                UpdatePreviewAccent(colorHex);
            }
        }

        private void UpdatePreview()
        {
            if (PreviewBorder == null) return;

            if (RbDarkerTheme.IsChecked == true)
            {
                PreviewBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D0D"));
            }
            else
            {
                PreviewBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            }
        }

        private void UpdatePreviewAccent(string colorHex)
        {
            if (PreviewPrimaryButton == null) return;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                PreviewPrimaryButton.Background = new SolidColorBrush(color);
            }
            catch
            {
                // Fallback to default blue
                PreviewPrimaryButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            RbDarkTheme.IsChecked = true;
            RbAccentBlue.IsChecked = true;
            Services.ThemeService.ResetToDefaults();
            UpdatePreview();
            UpdatePreviewAccent("#007ACC");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Services.ThemeService.SaveSettings();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Services.ThemeService.SaveSettings();
        }
    }
}
