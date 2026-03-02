using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Controls;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Views.Effects
{
    //
    /// <summary>
    /// Phase 4: Lighting & Vision System window.
    /// Provides UI for managing light sources, shadow casting, token vision,
    /// vision mode rendering, automatic fog reveal, and directional lights.
    /// </summary>
    public partial class Phase4LightingWindow : Window
    {
        // Reference to the battle grid for light manipulation
        private readonly BattleGridControl _battleGrid;

        // Observable collection for displaying lights in the list
        private readonly ObservableCollection<LightSourceDisplay> _lightDisplays = new();

        public Phase4LightingWindow(BattleGridControl battleGrid)
        {
            InitializeComponent();
            _battleGrid = battleGrid;
            LightsList.ItemsSource = _lightDisplays;
            LoadCurrentValues();
            RefreshLightList();
        }

        // Load current option values into controls
        private void LoadCurrentValues()
        {
            ChkEnableLighting.IsChecked = Options.EnableLighting;
            SliderBrightRadius.Value = Options.DefaultBrightLightRadius;
            SliderDimRadius.Value = Options.DefaultDimLightRadius;
            SliderIntensity.Value = 1.0;

            ChkEnableShadowCasting.IsChecked = Options.EnableShadowCasting;
            SliderRayCount.Value = Options.ShadowCastRayCount;
            SliderShadowSoftness.Value = Options.ShadowSoftnessPx;

            ChkEnableTokenVision.IsChecked = Options.EnableTokenVision;
            SliderVisionRange.Value = Options.DefaultTokenVisionRange;

            ChkEnableVisionRendering.IsChecked = Options.EnableVisionModeRendering;

            ChkEnableAutoFogReveal.IsChecked = Options.EnableAutoFogReveal;
            CmbFogRevealMode.SelectedIndex = Options.FogRevealMode;

            ChkEnableDirectionalLights.IsChecked = Options.EnableDirectionalLights;
            SliderConeDirection.Value = 0;
            SliderConeWidth.Value = 60;

            UpdateLabels();
        }

        // Update label text for all sliders
        private void UpdateLabels()
        {
            if (TxtBrightRadius != null) TxtBrightRadius.Text = $"{SliderBrightRadius.Value:F0} sq";
            if (TxtDimRadius != null) TxtDimRadius.Text = $"{SliderDimRadius.Value:F0} sq";
            if (TxtIntensity != null) TxtIntensity.Text = $"{SliderIntensity.Value:F1}";
            if (TxtRayCount != null) TxtRayCount.Text = $"{SliderRayCount.Value:F0}";
            if (TxtShadowSoftness != null) TxtShadowSoftness.Text = $"{SliderShadowSoftness.Value:F0} px";
            if (TxtVisionRange != null) TxtVisionRange.Text = $"{SliderVisionRange.Value:F0} sq";
            if (TxtConeDirection != null) TxtConeDirection.Text = $"{SliderConeDirection.Value:F0}°";
            if (TxtConeWidth != null) TxtConeWidth.Text = $"{SliderConeWidth.Value:F0}°";
        }

        // Refresh the list of lights from the battle grid
        // VISUAL REFRESH - LIGHTING
        private void RefreshLightList()
        {
            _lightDisplays.Clear();
            if (_battleGrid?.Lights != null)
            {
                foreach (var light in _battleGrid.Lights)
                {
                    _lightDisplays.Add(new LightSourceDisplay
                    {
                        Label = string.IsNullOrEmpty(light.Label) ? $"Light at ({light.CenterGrid.X:F0},{light.CenterGrid.Y:F0})" : light.Label,
                        Type = light.Type.ToString(),
                        Brightness = $"B:{light.BrightRadius:F0} D:{light.DimRadius:F0}",
                        Source = light
                    });
                }
            }
        }

        // Add a new point light source to the battle grid
        private void AddPointLight_Click(object sender, RoutedEventArgs e)
        {
            if (!Options.EnableLighting)
            {
                MessageBox.Show("Please enable the lighting system first.", "Lighting Disabled", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var light = new LightSource
            {
                CenterGrid = new Point(10, 10),
                BrightRadius = SliderBrightRadius.Value,
                DimRadius = SliderDimRadius.Value,
                Intensity = SliderIntensity.Value,
                LightColor = GetSelectedColor(),
                IsEnabled = true,
                Type = LightType.Point,
                Label = string.IsNullOrWhiteSpace(TxtLightLabel.Text) ? "Point Light" : TxtLightLabel.Text
            };

            _battleGrid.AddLight(light);
            RefreshLightList();
            MessageBox.Show($"Point light '{light.Label}' added at (10,10).\nDrag it on the map to reposition.", "Light Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Add a directional/cone light source
        private void AddDirectionalLight_Click(object sender, RoutedEventArgs e)
        {
            if (!Options.EnableLighting || !Options.EnableDirectionalLights)
            {
                MessageBox.Show("Please enable both the lighting system and directional lights first.", "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var light = new LightSource
            {
                CenterGrid = new Point(10, 10),
                BrightRadius = SliderBrightRadius.Value,
                DimRadius = SliderDimRadius.Value,
                Intensity = SliderIntensity.Value,
                LightColor = GetSelectedColor(),
                IsEnabled = true,
                Type = LightType.Directional,
                Direction = SliderConeDirection.Value,
                ConeWidth = SliderConeWidth.Value,
                Label = string.IsNullOrWhiteSpace(TxtLightLabel.Text) ? "Directional Light" : TxtLightLabel.Text
            };

            _battleGrid.AddLight(light);
            RefreshLightList();
            MessageBox.Show($"Directional light '{light.Label}' added at (10,10).\nDirection: {light.Direction:F0}°, Cone: {light.ConeWidth:F0}°", "Light Added", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Remove the selected light from the battle grid
        private void RemoveSelectedLight_Click(object sender, RoutedEventArgs e)
        {
            if (LightsList.SelectedItem is LightSourceDisplay display)
            {
                _battleGrid.RemoveLightPublic(display.Source);
                RefreshLightList();
            }
            else
            {
                MessageBox.Show("Please select a light to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Clear the shadow cache to force recalculation
        private void ClearShadowCache_Click(object sender, RoutedEventArgs e)
        {
            _battleGrid.InvalidateShadowCache();
            MessageBox.Show("Shadow cache cleared. Shadows will be recalculated on next render.", "Cache Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Manually trigger fog update based on token vision
        private void UpdateFogFromVision_Click(object sender, RoutedEventArgs e)
        {
            if (!Options.EnableAutoFogReveal)
            {
                MessageBox.Show("Please enable Auto Fog Reveal first.", "Feature Disabled", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _battleGrid.UpdateFogFromTokenVision();
            MessageBox.Show("Fog updated based on current token vision.", "Fog Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Toggle vision overlay rendering on the battle grid
        private void ToggleVisionOverlay_Click(object sender, RoutedEventArgs e)
        {
            bool show = ChkEnableVisionRendering.IsChecked == true;
            _battleGrid.ToggleVisionOverlay(show);
        }

        // Handle feature toggle checkbox changes - sync to Options
        private void OnFeatureToggled(object sender, RoutedEventArgs e)
        {
            Options.EnableLighting = ChkEnableLighting.IsChecked == true;
            Options.EnableShadowCasting = ChkEnableShadowCasting.IsChecked == true;
            Options.EnableTokenVision = ChkEnableTokenVision.IsChecked == true;
            Options.EnableVisionModeRendering = ChkEnableVisionRendering.IsChecked == true;
            Options.EnableAutoFogReveal = ChkEnableAutoFogReveal.IsChecked == true;
            Options.EnableDirectionalLights = ChkEnableDirectionalLights.IsChecked == true;
        }

        // Handle slider value changes - sync to Options
        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Options.DefaultBrightLightRadius = SliderBrightRadius != null ? SliderBrightRadius.Value : Options.DefaultBrightLightRadius;
            Options.DefaultDimLightRadius = SliderDimRadius != null ? SliderDimRadius.Value : Options.DefaultDimLightRadius;
            Options.ShadowCastRayCount = SliderRayCount != null ? (int)SliderRayCount.Value : Options.ShadowCastRayCount;
            Options.ShadowSoftnessPx = SliderShadowSoftness != null ? SliderShadowSoftness.Value : Options.ShadowSoftnessPx;
            Options.DefaultTokenVisionRange = SliderVisionRange != null ? (int)SliderVisionRange.Value : Options.DefaultTokenVisionRange;
            UpdateLabels();
        }

        // Handle fog reveal mode dropdown change
        private void OnFogModeChanged(object sender, SelectionChangedEventArgs e)
        {
            Options.FogRevealMode = CmbFogRevealMode.SelectedIndex;
        }

        // Get the selected color from the color preset combo box
        private Color GetSelectedColor()
        {
            if (CmbLightColor.SelectedIndex <= 0) return Colors.White;
            return CmbLightColor.SelectedIndex switch
            {
                1 => Colors.LightYellow,    // Warm/Torch
                2 => Colors.LightBlue,       // Cool/Moonlight
                3 => Colors.OrangeRed,       // Fire
                4 => Colors.LightGreen,      // Magical Green
                5 => Colors.MediumPurple,    // Arcane Purple
                _ => Colors.White
            };
        }

        /// <summary>
        /// Display wrapper for light sources in the list
        /// </summary>
        private class LightSourceDisplay
        {
            public string Label { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Brightness { get; set; } = string.Empty;
            public LightSource Source { get; set; }
        }
    }
}
