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

            // Phase 5
            ChkEnablePathfinding.IsChecked = Options.EnablePathfinding;
            ChkAllowDiagonal.IsChecked = Options.AllowDiagonalMovement;
            SliderPathDepth.Value = Options.PathfindingMaxDepth;
            ChkEnableMovementCost.IsChecked = Options.EnableMovementCostPreview;
            ChkEnablePathAnimation.IsChecked = Options.EnablePathAnimation;
            SliderAnimSpeed.Value = Options.PathAnimationSecondsPerSquare;
            ChkEnableAOO.IsChecked = Options.EnableAOODetection;
            ChkAutoResolveAOO.IsChecked = Options.AutoResolveAOOs;
            ChkEnableAuras.IsChecked = Options.EnableTokenAuras;
            ChkEnableElevation.IsChecked = Options.EnableTokenElevation;
            ChkEnableFacing.IsChecked = Options.EnableTokenFacing;
            ChkAutoFace.IsChecked = Options.AutoFaceMovementDirection;
            ChkEnableFlanking.IsChecked = Options.EnableFlankingDetection;

            // Phase 6
            ChkEnableSpellLibrary.IsChecked = Options.EnableSpellLibrary;
            ChkEnableDurationTracking.IsChecked = Options.EnableDurationTracking;
            ChkEnableDamageOverTime.IsChecked = Options.EnableDamageOverTime;
            ChkAutoApplyDot.IsChecked = Options.AutoApplyDotDamage;
            ChkEnablePolygonEffects.IsChecked = Options.EnablePolygonEffects;
            ChkEnableEffectAnimations.IsChecked = Options.EnableEffectAnimations;
            SliderMaxParticles.Value = Options.MaxParticlesPerEffect;
            CmbDefaultAnimation.SelectedIndex = Options.DefaultAnimationType;

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            TxtBrightRadius.Text = $"{SliderBrightRadius.Value:F0} sq";
            TxtDimRadius.Text = $"{SliderDimRadius.Value:F0} sq";
            TxtRayCount.Text = $"{SliderRayCount.Value:F0}";
            TxtShadowSoftness.Text = $"{SliderShadowSoftness.Value:F0} px";
            TxtVisionRange.Text = $"{SliderVisionRange.Value:F0} sq";
            TxtPathDepth.Text = $"{SliderPathDepth.Value:F0} sq";
            TxtAnimSpeed.Text = $"{SliderAnimSpeed.Value:F1} s";
            TxtMaxParticles.Text = $"{SliderMaxParticles.Value:F0}";
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

            // Phase 5
            Options.EnablePathfinding = ChkEnablePathfinding.IsChecked == true;
            Options.AllowDiagonalMovement = ChkAllowDiagonal.IsChecked == true;
            Options.EnableMovementCostPreview = ChkEnableMovementCost.IsChecked == true;
            Options.EnablePathAnimation = ChkEnablePathAnimation.IsChecked == true;
            Options.EnableAOODetection = ChkEnableAOO.IsChecked == true;
            Options.AutoResolveAOOs = ChkAutoResolveAOO.IsChecked == true;
            Options.EnableTokenAuras = ChkEnableAuras.IsChecked == true;
            Options.EnableTokenElevation = ChkEnableElevation.IsChecked == true;
            Options.EnableTokenFacing = ChkEnableFacing.IsChecked == true;
            Options.AutoFaceMovementDirection = ChkAutoFace.IsChecked == true;
            Options.EnableFlankingDetection = ChkEnableFlanking.IsChecked == true;

            // Phase 6
            Options.EnableSpellLibrary = ChkEnableSpellLibrary.IsChecked == true;
            Options.EnableDurationTracking = ChkEnableDurationTracking.IsChecked == true;
            Options.EnableDamageOverTime = ChkEnableDamageOverTime.IsChecked == true;
            Options.AutoApplyDotDamage = ChkAutoApplyDot.IsChecked == true;
            Options.EnablePolygonEffects = ChkEnablePolygonEffects.IsChecked == true;
            Options.EnableEffectAnimations = ChkEnableEffectAnimations.IsChecked == true;
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_initializing) return;

            Options.DefaultBrightLightRadius = SliderBrightRadius.Value;
            Options.DefaultDimLightRadius = SliderDimRadius.Value;
            Options.ShadowCastRayCount = (int)SliderRayCount.Value;
            Options.ShadowSoftnessPx = SliderShadowSoftness.Value;
            Options.DefaultTokenVisionRange = (int)SliderVisionRange.Value;

            // Phase 5
            Options.PathfindingMaxDepth = (int)SliderPathDepth.Value;
            Options.PathAnimationSecondsPerSquare = SliderAnimSpeed.Value;

            // Phase 6
            Options.MaxParticlesPerEffect = (int)SliderMaxParticles.Value;

            UpdateLabels();
        }

        private void OnComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing) return;
            Options.FogRevealMode = CmbFogRevealMode.SelectedIndex;
        }

        private void OnAnimationComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing) return;
            Options.DefaultAnimationType = CmbDefaultAnimation.SelectedIndex;
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

            // Phase 5
            Options.EnablePathfinding = true;
            Options.AllowDiagonalMovement = true;
            Options.PathfindingMaxDepth = 60;
            Options.EnableMovementCostPreview = true;
            Options.EnablePathAnimation = true;
            Options.PathAnimationSecondsPerSquare = 0.3;
            Options.EnableAOODetection = true;
            Options.AutoResolveAOOs = true;
            Options.EnableTokenAuras = true;
            Options.EnableTokenElevation = true;
            Options.EnableTokenFacing = true;
            Options.AutoFaceMovementDirection = true;
            Options.EnableFlankingDetection = true;

            // Phase 6
            Options.EnableSpellLibrary = true;
            Options.EnableDurationTracking = true;
            Options.EnableDamageOverTime = true;
            Options.AutoApplyDotDamage = true;
            Options.EnablePolygonEffects = true;
            Options.EnableEffectAnimations = true;
            Options.MaxParticlesPerEffect = 50;
            Options.DefaultAnimationType = 1;

            _initializing = true;
            LoadCurrentValues();
            _initializing = false;
        }
    }
}
