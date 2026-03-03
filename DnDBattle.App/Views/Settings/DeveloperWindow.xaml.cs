using System.Windows;
using System.Windows.Controls;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;

namespace DnDBattle.Views.Settings
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

            // Phase 7
            ChkEnableAttackRolls.IsChecked = Options.EnableAttackRollSystem;
            ChkAutoApplyDamage.IsChecked = Options.AutoApplyDamage;
            ChkEnableSavingThrows.IsChecked = Options.EnableSavingThrowAutomation;
            ChkAutoRollMonsterSaves.IsChecked = Options.AutoRollMonsterSaves;
            ChkEnableSpellSlotTracking.IsChecked = Options.EnableSpellSlotTracking;
            ChkEnableConcentration.IsChecked = Options.EnableConcentrationTracking;
            ChkAutoPromptConcentration.IsChecked = Options.AutoPromptConcentrationCheck;
            ChkEnableConditionAutomation.IsChecked = Options.EnableConditionAutomation;
            ChkEnableCoverSystem.IsChecked = Options.EnableCoverSystem;

            // Phase 8
            ChkEnableMultiMap.IsChecked = Options.EnableMultiMapManagement;
            SliderMaxRecentMaps.Value = Options.MapLibraryMaxRecent;
            ChkEnableBackgroundLayers.IsChecked = Options.EnableBackgroundLayers;
            ChkEnableHexGrid.IsChecked = Options.EnableHexGrid;
            ChkEnableGridlessMode.IsChecked = Options.EnableGridlessMode;
            ChkEnableCustomGridSizes.IsChecked = Options.EnableCustomGridSizes;
            SliderDefaultFeetPerSquare.Value = Options.DefaultFeetPerSquare;
            ChkEnableMapNotes.IsChecked = Options.EnableMapNotes;
            ChkShowDMOnlyNotes.IsChecked = Options.ShowDMOnlyNotes;
            SliderMapNoteFontSize.Value = Options.MapNoteDefaultFontSize;

            // Undecided Features
            ChkEnableWeatherEffects.IsChecked = Options.EnableWeatherEffects;
            ChkEnableDayNightCycle.IsChecked = Options.EnableDayNightCycle;
            SliderWeatherMaxParticles.Value = Options.WeatherMaxParticles;
            ChkEnableElevationSystem.IsChecked = Options.EnableElevationSystem;
            ChkEnableAnimatedTiles.IsChecked = Options.EnableAnimatedTiles;
            ChkEnableProceduralMapGen.IsChecked = Options.EnableProceduralMapGeneration;
            ChkEnableTokenCustomization.IsChecked = Options.EnableTokenCustomization;
            ChkEnableMeasurements.IsChecked = Options.EnableMeasurements;
            ChkEnableDiceRoller3D.IsChecked = Options.EnableDiceRoller3D;
            ChkEnableDiceStatistics.IsChecked = Options.EnableDiceStatistics;
            ChkEnableHighContrast.IsChecked = Options.EnableHighContrast;
            ChkEnableColorblindMode.IsChecked = Options.EnableColorblindMode;
            ChkEnableDyslexiaFont.IsChecked = Options.EnableDyslexiaFont;
            ChkEnableKeyboardNav.IsChecked = Options.EnableKeyboardNavigation;
            SliderUIScale.Value = Options.AccessibilityUIScale;

            // Phase 9
            ChkEnableNetworking.IsChecked = Options.EnableNetworking;
            SliderNetworkPort.Value = Options.NetworkDefaultPort;
            SliderNetworkTimeout.Value = Options.NetworkConnectionTimeoutSeconds;
            ChkEnableClientPrediction.IsChecked = Options.EnableClientPrediction;
            ChkEnableMultiplayerChat.IsChecked = Options.EnableMultiplayerChat;
            ChkEnableFogSync.IsChecked = Options.EnableFogSync;
            ChkEnableVoiceChat.IsChecked = Options.EnableVoiceChat;
            ChkEnableCloudSave.IsChecked = Options.EnableCloudSave;

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
            TxtMaxRecentMaps.Text = $"{SliderMaxRecentMaps.Value:F0}";
            TxtDefaultFeetPerSquare.Text = $"{SliderDefaultFeetPerSquare.Value:F0} ft";
            TxtMapNoteFontSize.Text = $"{SliderMapNoteFontSize.Value:F0} pt";
            TxtWeatherMaxParticles.Text = $"{SliderWeatherMaxParticles.Value:F0}";
            TxtUIScale.Text = $"{SliderUIScale.Value:F0}%";
            TxtNetworkPort.Text = $"{SliderNetworkPort.Value:F0}";
            TxtNetworkTimeout.Text = $"{SliderNetworkTimeout.Value:F0}s";
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

            // Phase 7
            Options.EnableAttackRollSystem = ChkEnableAttackRolls.IsChecked == true;
            Options.AutoApplyDamage = ChkAutoApplyDamage.IsChecked == true;
            Options.EnableSavingThrowAutomation = ChkEnableSavingThrows.IsChecked == true;
            Options.AutoRollMonsterSaves = ChkAutoRollMonsterSaves.IsChecked == true;
            Options.EnableSpellSlotTracking = ChkEnableSpellSlotTracking.IsChecked == true;
            Options.EnableConcentrationTracking = ChkEnableConcentration.IsChecked == true;
            Options.AutoPromptConcentrationCheck = ChkAutoPromptConcentration.IsChecked == true;
            Options.EnableConditionAutomation = ChkEnableConditionAutomation.IsChecked == true;
            Options.EnableCoverSystem = ChkEnableCoverSystem.IsChecked == true;

            // Phase 8
            Options.EnableMultiMapManagement = ChkEnableMultiMap.IsChecked == true;
            Options.EnableBackgroundLayers = ChkEnableBackgroundLayers.IsChecked == true;
            Options.EnableHexGrid = ChkEnableHexGrid.IsChecked == true;
            Options.EnableGridlessMode = ChkEnableGridlessMode.IsChecked == true;
            Options.EnableCustomGridSizes = ChkEnableCustomGridSizes.IsChecked == true;
            Options.EnableMapNotes = ChkEnableMapNotes.IsChecked == true;
            Options.ShowDMOnlyNotes = ChkShowDMOnlyNotes.IsChecked == true;

            // Undecided Features
            Options.EnableWeatherEffects = ChkEnableWeatherEffects.IsChecked == true;
            Options.EnableDayNightCycle = ChkEnableDayNightCycle.IsChecked == true;
            Options.EnableElevationSystem = ChkEnableElevationSystem.IsChecked == true;
            Options.EnableAnimatedTiles = ChkEnableAnimatedTiles.IsChecked == true;
            Options.EnableProceduralMapGeneration = ChkEnableProceduralMapGen.IsChecked == true;
            Options.EnableTokenCustomization = ChkEnableTokenCustomization.IsChecked == true;
            Options.EnableMeasurements = ChkEnableMeasurements.IsChecked == true;
            Options.EnableDiceRoller3D = ChkEnableDiceRoller3D.IsChecked == true;
            Options.EnableDiceStatistics = ChkEnableDiceStatistics.IsChecked == true;
            Options.EnableHighContrast = ChkEnableHighContrast.IsChecked == true;
            Options.EnableColorblindMode = ChkEnableColorblindMode.IsChecked == true;
            Options.EnableDyslexiaFont = ChkEnableDyslexiaFont.IsChecked == true;
            Options.EnableKeyboardNavigation = ChkEnableKeyboardNav.IsChecked == true;

            // Phase 9
            Options.EnableNetworking = ChkEnableNetworking.IsChecked == true;
            Options.EnableClientPrediction = ChkEnableClientPrediction.IsChecked == true;
            Options.EnableMultiplayerChat = ChkEnableMultiplayerChat.IsChecked == true;
            Options.EnableFogSync = ChkEnableFogSync.IsChecked == true;
            Options.EnableVoiceChat = ChkEnableVoiceChat.IsChecked == true;
            Options.EnableCloudSave = ChkEnableCloudSave.IsChecked == true;
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_initializing) return;

            Options.DefaultBrightLightRadius = SliderBrightRadius != null ? SliderBrightRadius.Value : Options.DefaultBrightLightRadius;
            Options.DefaultDimLightRadius = SliderDimRadius != null ? SliderDimRadius.Value : Options.DefaultDimLightRadius;
            Options.ShadowCastRayCount = SliderRayCount != null ? (int)SliderRayCount.Value : Options.ShadowCastRayCount;
            Options.ShadowSoftnessPx = SliderShadowSoftness != null ? SliderShadowSoftness.Value : Options.ShadowSoftnessPx;
            Options.DefaultTokenVisionRange = SliderVisionRange != null ? (int)SliderVisionRange.Value : Options.DefaultTokenVisionRange;

            // Phase 5
            Options.PathfindingMaxDepth = SliderPathDepth != null ? (int)SliderPathDepth.Value : Options.PathfindingMaxDepth;
            Options.PathAnimationSecondsPerSquare = SliderAnimSpeed != null ? SliderAnimSpeed.Value : Options.PathAnimationSecondsPerSquare;

            // Phase 6
            Options.MaxParticlesPerEffect = SliderMaxParticles != null ? (int)SliderMaxParticles.Value : Options.MaxParticlesPerEffect;

            // Phase 8
            Options.MapLibraryMaxRecent = SliderMaxRecentMaps != null ? (int)SliderMaxRecentMaps.Value : Options.MapLibraryMaxRecent;
            Options.DefaultFeetPerSquare = SliderDefaultFeetPerSquare != null ? (int)SliderDefaultFeetPerSquare.Value : Options.DefaultFeetPerSquare;
            Options.MapNoteDefaultFontSize = SliderMapNoteFontSize != null ? SliderMapNoteFontSize.Value : Options.MapNoteDefaultFontSize;

            // Undecided Features
            Options.WeatherMaxParticles = SliderWeatherMaxParticles != null ? (int)SliderWeatherMaxParticles.Value : Options.WeatherMaxParticles;
            Options.AccessibilityUIScale = SliderUIScale != null ? (int)SliderUIScale.Value : Options.AccessibilityUIScale;

            // Phase 9
            Options.NetworkDefaultPort = SliderNetworkPort != null ? (int)SliderNetworkPort.Value : Options.NetworkDefaultPort;
            Options.NetworkConnectionTimeoutSeconds = SliderNetworkTimeout != null ? (int)SliderNetworkTimeout.Value : Options.NetworkConnectionTimeoutSeconds;

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

            // Phase 7
            Options.EnableAttackRollSystem = true;
            Options.AutoApplyDamage = true;
            Options.EnableSavingThrowAutomation = true;
            Options.AutoRollMonsterSaves = true;
            Options.EnableSpellSlotTracking = true;
            Options.EnableConcentrationTracking = true;
            Options.AutoPromptConcentrationCheck = true;
            Options.EnableConditionAutomation = true;
            Options.EnableCoverSystem = true;

            // Phase 8
            Options.EnableMultiMapManagement = true;
            Options.MapLibraryMaxRecent = 10;
            Options.EnableBackgroundLayers = true;
            Options.EnableHexGrid = true;
            Options.EnableGridlessMode = true;
            Options.EnableCustomGridSizes = true;
            Options.DefaultFeetPerSquare = 5;
            Options.EnableMapNotes = true;
            Options.ShowDMOnlyNotes = true;
            Options.MapNoteDefaultFontSize = 12;

            // Undecided Features
            Options.EnableWeatherEffects = false;
            Options.EnableDayNightCycle = false;
            Options.WeatherMaxParticles = 300;
            Options.EnableElevationSystem = false;
            Options.EnableAnimatedTiles = false;
            Options.EnableProceduralMapGeneration = false;
            Options.EnableTokenCustomization = false;
            Options.EnableMeasurements = false;
            Options.EnableDiceRoller3D = false;
            Options.EnableDiceStatistics = false;
            Options.EnableHighContrast = false;
            Options.EnableColorblindMode = false;
            Options.AccessibilityUIScale = 100;
            Options.EnableDyslexiaFont = false;
            Options.EnableKeyboardNavigation = false;

            // Phase 9
            Options.EnableNetworking = false;
            Options.NetworkDefaultPort = 7777;
            Options.NetworkConnectionTimeoutSeconds = 10;
            Options.EnableClientPrediction = true;
            Options.EnableMultiplayerChat = false;
            Options.EnableFogSync = false;
            Options.EnableVoiceChat = false;
            Options.EnableCloudSave = false;
            Options.CloudSaveServerUrl = string.Empty;

            _initializing = true;
            LoadCurrentValues();
            _initializing = false;
        }
    }
}
