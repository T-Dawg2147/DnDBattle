using System.Windows;
using System.Windows.Controls;

namespace DnDBattle.Views
{
    /// <summary>
    /// Developer Settings window for enabling/disabling Phase 4 features.
    /// All toggles read and write from the Options static class.
    /// </summary>
    public partial class DeveloperWindow : Window
    {
        private bool _initializing = true;

        public DeveloperWindow()
        {
            InitializeComponent();
            LoadCurrentValues();
            _initializing = false;
        }

        private void LoadCurrentValues()
        {
            // 4.1
            ChkEnableLighting.IsChecked = Options.EnableLighting;
            SliderBrightRadius.Value = Options.DefaultBrightLightRadius;
            SliderDimRadius.Value = Options.DefaultDimLightRadius;

            // 4.2
            ChkEnableShadowCasting.IsChecked = Options.EnableShadowCasting;
            SliderRayCount.Value = Options.ShadowCastRayCount;
            SliderShadowSoftness.Value = Options.ShadowSoftnessPx;

            // 4.3
            ChkEnableTokenVision.IsChecked = Options.EnableTokenVision;
            SliderVisionRange.Value = Options.DefaultTokenVisionRange;

            // 4.4
            ChkEnableVisionRendering.IsChecked = Options.EnableVisionModeRendering;

            // 4.5
            ChkEnableAutoFogReveal.IsChecked = Options.EnableAutoFogReveal;
            CmbFogRevealMode.SelectedIndex = Options.FogRevealMode;

            // 4.6
            ChkEnableDirectionalLights.IsChecked = Options.EnableDirectionalLights;

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            TxtBrightRadius.Text = $"{SliderBrightRadius.Value:F0} sq";
            TxtDimRadius.Text = $"{SliderDimRadius.Value:F0} sq";
            TxtRayCount.Text = $"{SliderRayCount.Value:F0}";
            TxtShadowSoftness.Text = $"{SliderShadowSoftness.Value:F0} px";
            TxtVisionRange.Text = $"{SliderVisionRange.Value:F0} sq";
        }

        private void OnFeatureToggled(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;

            Options.EnableLighting = ChkEnableLighting.IsChecked == true;
            Options.EnableShadowCasting = ChkEnableShadowCasting.IsChecked == true;
            Options.EnableTokenVision = ChkEnableTokenVision.IsChecked == true;
            Options.EnableVisionModeRendering = ChkEnableVisionRendering.IsChecked == true;
            Options.EnableAutoFogReveal = ChkEnableAutoFogReveal.IsChecked == true;
            Options.EnableDirectionalLights = ChkEnableDirectionalLights.IsChecked == true;
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_initializing) return;

            Options.DefaultBrightLightRadius = SliderBrightRadius.Value;
            Options.DefaultDimLightRadius = SliderDimRadius.Value;
            Options.ShadowCastRayCount = (int)SliderRayCount.Value;
            Options.ShadowSoftnessPx = SliderShadowSoftness.Value;
            Options.DefaultTokenVisionRange = (int)SliderVisionRange.Value;

            UpdateLabels();
        }

        private void OnComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing) return;
            Options.FogRevealMode = CmbFogRevealMode.SelectedIndex;
        }

        private void OnResetDefaults(object sender, RoutedEventArgs e)
        {
            Options.EnableLighting = true;
            Options.EnableShadowCasting = true;
            Options.EnableTokenVision = true;
            Options.EnableVisionModeRendering = true;
            Options.EnableAutoFogReveal = true;
            Options.EnableDirectionalLights = true;
            Options.DefaultBrightLightRadius = 4.0;
            Options.DefaultDimLightRadius = 8.0;
            Options.ShadowCastRayCount = 180;
            Options.ShadowSoftnessPx = 6.0;
            Options.DefaultTokenVisionRange = 12;
            Options.FogRevealMode = 0;

            _initializing = true;
            LoadCurrentValues();
            _initializing = false;
        }
    }
}
