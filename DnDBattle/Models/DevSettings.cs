using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Text.Json;

namespace DnDBattle.Models
{
    /// <summary>
    /// Developer Settings model for controlling experimental and advanced features.
    /// This class contains all feature toggles organized by phase/category.
    /// </summary>
    public class DevSettings : ObservableObject
    {
        private static DevSettings? _instance;
        private static readonly object _lock = new();
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DnDBattle",
            "dev_settings.json");

        public static DevSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= Load();
                    }
                }
                return _instance;
            }
        }

        #region Phase 4: Lighting & Vision System

        // Light Sources
        private bool _enablePointLights = true;
        public bool EnablePointLights
        {
            get => _enablePointLights;
            set => SetProperty(ref _enablePointLights, value);
        }

        private bool _enableDirectionalLights = true;
        public bool EnableDirectionalLights
        {
            get => _enableDirectionalLights;
            set => SetProperty(ref _enableDirectionalLights, value);
        }

        private bool _enableColoredLights = true;
        public bool EnableColoredLights
        {
            get => _enableColoredLights;
            set => SetProperty(ref _enableColoredLights, value);
        }

        private bool _enableLightBlending = true;
        public bool EnableLightBlending
        {
            get => _enableLightBlending;
            set => SetProperty(ref _enableLightBlending, value);
        }

        // Shadow Casting
        private bool _enableShadowCasting = true;
        public bool EnableShadowCasting
        {
            get => _enableShadowCasting;
            set => SetProperty(ref _enableShadowCasting, value);
        }

        private bool _enableSoftShadows = true;
        public bool EnableSoftShadows
        {
            get => _enableSoftShadows;
            set => SetProperty(ref _enableSoftShadows, value);
        }

        private double _shadowOpacity = 0.7;
        public double ShadowOpacity
        {
            get => _shadowOpacity;
            set => SetProperty(ref _shadowOpacity, Math.Clamp(value, 0.0, 1.0));
        }

        private bool _enableViewportCulling = true;
        public bool EnableViewportCulling
        {
            get => _enableViewportCulling;
            set => SetProperty(ref _enableViewportCulling, value);
        }

        // Token Vision
        private bool _enableTokenVision = true;
        public bool EnableTokenVision
        {
            get => _enableTokenVision;
            set => SetProperty(ref _enableTokenVision, value);
        }

        private bool _enableDarkvision = true;
        public bool EnableDarkvision
        {
            get => _enableDarkvision;
            set => SetProperty(ref _enableDarkvision, value);
        }

        private bool _enableBlindsight = true;
        public bool EnableBlindsight
        {
            get => _enableBlindsight;
            set => SetProperty(ref _enableBlindsight, value);
        }

        private bool _enableTruesight = true;
        public bool EnableTruesight
        {
            get => _enableTruesight;
            set => SetProperty(ref _enableTruesight, value);
        }

        private bool _enableTremorsense = true;
        public bool EnableTremorsense
        {
            get => _enableTremorsense;
            set => SetProperty(ref _enableTremorsense, value);
        }

        // Vision Modes
        private bool _enableDevilsSight = true;
        public bool EnableDevilsSight
        {
            get => _enableDevilsSight;
            set => SetProperty(ref _enableDevilsSight, value);
        }

        // Line of Sight
        private bool _enableLineOfSight = true;
        public bool EnableLineOfSight
        {
            get => _enableLineOfSight;
            set => SetProperty(ref _enableLineOfSight, value);
        }

        private bool _enableAutoFogReveal = true;
        public bool EnableAutoFogReveal
        {
            get => _enableAutoFogReveal;
            set => SetProperty(ref _enableAutoFogReveal, value);
        }

        private bool _enableWallOcclusion = true;
        public bool EnableWallOcclusion
        {
            get => _enableWallOcclusion;
            set => SetProperty(ref _enableWallOcclusion, value);
        }

        #endregion

        #region Phase 5: Advanced Token Features

        // Pathfinding
        private bool _enablePathfinding = true;
        public bool EnablePathfinding
        {
            get => _enablePathfinding;
            set => SetProperty(ref _enablePathfinding, value);
        }

        private bool _enableMovementCostPreview = true;
        public bool EnableMovementCostPreview
        {
            get => _enableMovementCostPreview;
            set => SetProperty(ref _enableMovementCostPreview, value);
        }

        private bool _enablePathAnimation = true;
        public bool EnablePathAnimation
        {
            get => _enablePathAnimation;
            set => SetProperty(ref _enablePathAnimation, value);
        }

        private bool _enableAOOWarnings = true;
        public bool EnableAOOWarnings
        {
            get => _enableAOOWarnings;
            set => SetProperty(ref _enableAOOWarnings, value);
        }

        private bool _enableDifficultTerrain = true;
        public bool EnableDifficultTerrain
        {
            get => _enableDifficultTerrain;
            set => SetProperty(ref _enableDifficultTerrain, value);
        }

        // Token Measurements
        private bool _enableRangeIndicators = true;
        public bool EnableRangeIndicators
        {
            get => _enableRangeIndicators;
            set => SetProperty(ref _enableRangeIndicators, value);
        }

        private bool _enableAreaTemplates = true;
        public bool EnableAreaTemplates
        {
            get => _enableAreaTemplates;
            set => SetProperty(ref _enableAreaTemplates, value);
        }

        private bool _enableMovementRuler = true;
        public bool EnableMovementRuler
        {
            get => _enableMovementRuler;
            set => SetProperty(ref _enableMovementRuler, value);
        }

        private DiagonalMovementRule _diagonalMovementRule = DiagonalMovementRule.Standard5105;
        public DiagonalMovementRule DiagonalMovementRule
        {
            get => _diagonalMovementRule;
            set => SetProperty(ref _diagonalMovementRule, value);
        }

        // Visual Enhancements
        private bool _enableTokenStatusRings = true;
        public bool EnableTokenStatusRings
        {
            get => _enableTokenStatusRings;
            set => SetProperty(ref _enableTokenStatusRings, value);
        }

        private bool _enableTokenHPBars = true;
        public bool EnableTokenHPBars
        {
            get => _enableTokenHPBars;
            set => SetProperty(ref _enableTokenHPBars, value);
        }

        private bool _enableAuraVisualization = true;
        public bool EnableAuraVisualization
        {
            get => _enableAuraVisualization;
            set => SetProperty(ref _enableAuraVisualization, value);
        }

        private bool _enableTokenFacing = true;
        public bool EnableTokenFacing
        {
            get => _enableTokenFacing;
            set => SetProperty(ref _enableTokenFacing, value);
        }

        private bool _enableTokenElevation = true;
        public bool EnableTokenElevation
        {
            get => _enableTokenElevation;
            set => SetProperty(ref _enableTokenElevation, value);
        }

        // Token States Visual
        private bool _enableProneVisual = true;
        public bool EnableProneVisual
        {
            get => _enableProneVisual;
            set => SetProperty(ref _enableProneVisual, value);
        }

        private bool _enableInvisibleVisual = true;
        public bool EnableInvisibleVisual
        {
            get => _enableInvisibleVisual;
            set => SetProperty(ref _enableInvisibleVisual, value);
        }

        private bool _enableUnconsciousVisual = true;
        public bool EnableUnconsciousVisual
        {
            get => _enableUnconsciousVisual;
            set => SetProperty(ref _enableUnconsciousVisual, value);
        }

        private bool _enableDeadVisual = true;
        public bool EnableDeadVisual
        {
            get => _enableDeadVisual;
            set => SetProperty(ref _enableDeadVisual, value);
        }

        private bool _enableConcentrationIndicator = true;
        public bool EnableConcentrationIndicator
        {
            get => _enableConcentrationIndicator;
            set => SetProperty(ref _enableConcentrationIndicator, value);
        }

        #endregion

        #region Phase 6: Area Effects System

        // Spell Templates
        private bool _enableSpellTemplatesLibrary = true;
        public bool EnableSpellTemplatesLibrary
        {
            get => _enableSpellTemplatesLibrary;
            set => SetProperty(ref _enableSpellTemplatesLibrary, value);
        }

        private bool _enableCustomTemplates = true;
        public bool EnableCustomTemplates
        {
            get => _enableCustomTemplates;
            set => SetProperty(ref _enableCustomTemplates, value);
        }

        private bool _enableDamageTypeVisualization = true;
        public bool EnableDamageTypeVisualization
        {
            get => _enableDamageTypeVisualization;
            set => SetProperty(ref _enableDamageTypeVisualization, value);
        }

        // Duration Tracking
        private bool _enableEffectDurationTracking = true;
        public bool EnableEffectDurationTracking
        {
            get => _enableEffectDurationTracking;
            set => SetProperty(ref _enableEffectDurationTracking, value);
        }

        private bool _enableAutoRemoveExpiredEffects = true;
        public bool EnableAutoRemoveExpiredEffects
        {
            get => _enableAutoRemoveExpiredEffects;
            set => SetProperty(ref _enableAutoRemoveExpiredEffects, value);
        }

        private bool _enableDamageOverTime = true;
        public bool EnableDamageOverTime
        {
            get => _enableDamageOverTime;
            set => SetProperty(ref _enableDamageOverTime, value);
        }

        // Advanced Shapes
        private bool _enableAdvancedShapes = true;
        public bool EnableAdvancedShapes
        {
            get => _enableAdvancedShapes;
            set => SetProperty(ref _enableAdvancedShapes, value);
        }

        private bool _enableWallOfFireShape = true;
        public bool EnableWallOfFireShape
        {
            get => _enableWallOfFireShape;
            set => SetProperty(ref _enableWallOfFireShape, value);
        }

        private bool _enableHemisphereShape = true;
        public bool EnableHemisphereShape
        {
            get => _enableHemisphereShape;
            set => SetProperty(ref _enableHemisphereShape, value);
        }

        private bool _enableCustomPolygonShapes = true;
        public bool EnableCustomPolygonShapes
        {
            get => _enableCustomPolygonShapes;
            set => SetProperty(ref _enableCustomPolygonShapes, value);
        }

        // Effect Animations
        private bool _enableEffectAnimations = true;
        public bool EnableEffectAnimations
        {
            get => _enableEffectAnimations;
            set => SetProperty(ref _enableEffectAnimations, value);
        }

        private bool _enablePulsingBorders = true;
        public bool EnablePulsingBorders
        {
            get => _enablePulsingBorders;
            set => SetProperty(ref _enablePulsingBorders, value);
        }

        private bool _enableParticleEffects = false;
        public bool EnableParticleEffects
        {
            get => _enableParticleEffects;
            set => SetProperty(ref _enableParticleEffects, value);
        }

        private bool _enableFadeTransitions = true;
        public bool EnableFadeTransitions
        {
            get => _enableFadeTransitions;
            set => SetProperty(ref _enableFadeTransitions, value);
        }

        #endregion

        #region Phase 7: Combat Automation

        // Attack Rolls
        private bool _enableClickToTarget = true;
        public bool EnableClickToTarget
        {
            get => _enableClickToTarget;
            set => SetProperty(ref _enableClickToTarget, value);
        }

        private bool _enableAutoAttackRoll = true;
        public bool EnableAutoAttackRoll
        {
            get => _enableAutoAttackRoll;
            set => SetProperty(ref _enableAutoAttackRoll, value);
        }

        private bool _enableCriticalEffects = true;
        public bool EnableCriticalEffects
        {
            get => _enableCriticalEffects;
            set => SetProperty(ref _enableCriticalEffects, value);
        }

        private bool _enableAutoDamageApplication = true;
        public bool EnableAutoDamageApplication
        {
            get => _enableAutoDamageApplication;
            set => SetProperty(ref _enableAutoDamageApplication, value);
        }

        // Defense Automation
        private bool _enableAutoCoverCalculation = true;
        public bool EnableAutoCoverCalculation
        {
            get => _enableAutoCoverCalculation;
            set => SetProperty(ref _enableAutoCoverCalculation, value);
        }

        private bool _enableSavingThrowPrompts = true;
        public bool EnableSavingThrowPrompts
        {
            get => _enableSavingThrowPrompts;
            set => SetProperty(ref _enableSavingThrowPrompts, value);
        }

        private bool _enableAdvantageTracking = true;
        public bool EnableAdvantageTracking
        {
            get => _enableAdvantageTracking;
            set => SetProperty(ref _enableAdvantageTracking, value);
        }

        private bool _enableResistanceApplication = true;
        public bool EnableResistanceApplication
        {
            get => _enableResistanceApplication;
            set => SetProperty(ref _enableResistanceApplication, value);
        }

        // Spell Management
        private bool _enableSpellSlotTracking = true;
        public bool EnableSpellSlotTracking
        {
            get => _enableSpellSlotTracking;
            set => SetProperty(ref _enableSpellSlotTracking, value);
        }

        private bool _enableConcentrationChecks = true;
        public bool EnableConcentrationChecks
        {
            get => _enableConcentrationChecks;
            set => SetProperty(ref _enableConcentrationChecks, value);
        }

        private bool _enableAreaAutoTargeting = true;
        public bool EnableAreaAutoTargeting
        {
            get => _enableAreaAutoTargeting;
            set => SetProperty(ref _enableAreaAutoTargeting, value);
        }

        private bool _enableSpellSaveDCCalculation = true;
        public bool EnableSpellSaveDCCalculation
        {
            get => _enableSpellSaveDCCalculation;
            set => SetProperty(ref _enableSpellSaveDCCalculation, value);
        }

        // Combat Helpers
        private bool _enableAOODetection = true;
        public bool EnableAOODetection
        {
            get => _enableAOODetection;
            set => SetProperty(ref _enableAOODetection, value);
        }

        private bool _enableFlankingIndicators = true;
        public bool EnableFlankingIndicators
        {
            get => _enableFlankingIndicators;
            set => SetProperty(ref _enableFlankingIndicators, value);
        }

        private bool _enableSneakAttackEligibility = true;
        public bool EnableSneakAttackEligibility
        {
            get => _enableSneakAttackEligibility;
            set => SetProperty(ref _enableSneakAttackEligibility, value);
        }

        private bool _enableReactionPrompts = true;
        public bool EnableReactionPrompts
        {
            get => _enableReactionPrompts;
            set => SetProperty(ref _enableReactionPrompts, value);
        }

        #endregion

        #region Phase 8: Map Features

        // Multi-Map Support
        private bool _enableMultiMapSupport = true;
        public bool EnableMultiMapSupport
        {
            get => _enableMultiMapSupport;
            set => SetProperty(ref _enableMultiMapSupport, value);
        }

        private bool _enableMapLinking = true;
        public bool EnableMapLinking
        {
            get => _enableMapLinking;
            set => SetProperty(ref _enableMapLinking, value);
        }

        private bool _enableRecentMaps = true;
        public bool EnableRecentMaps
        {
            get => _enableRecentMaps;
            set => SetProperty(ref _enableRecentMaps, value);
        }

        // Background Layers
        private bool _enableBackgroundLayers = true;
        public bool EnableBackgroundLayers
        {
            get => _enableBackgroundLayers;
            set => SetProperty(ref _enableBackgroundLayers, value);
        }

        private bool _enableGridAlignment = true;
        public bool EnableGridAlignment
        {
            get => _enableGridAlignment;
            set => SetProperty(ref _enableGridAlignment, value);
        }

        // Grid Options
        private bool _enableHexGrid = false;
        public bool EnableHexGrid
        {
            get => _enableHexGrid;
            set => SetProperty(ref _enableHexGrid, value);
        }

        private bool _enableIsometricGrid = false;
        public bool EnableIsometricGrid
        {
            get => _enableIsometricGrid;
            set => SetProperty(ref _enableIsometricGrid, value);
        }

        private bool _enableGridlessMode = false;
        public bool EnableGridlessMode
        {
            get => _enableGridlessMode;
            set => SetProperty(ref _enableGridlessMode, value);
        }

        private int _customGridSizeFeet = 5;
        public int CustomGridSizeFeet
        {
            get => _customGridSizeFeet;
            set => SetProperty(ref _customGridSizeFeet, Math.Clamp(value, 5, 30));
        }

        // Map Decorations
        private bool _enableTextLabels = true;
        public bool EnableTextLabels
        {
            get => _enableTextLabels;
            set => SetProperty(ref _enableTextLabels, value);
        }

        private bool _enableMeasurementLines = true;
        public bool EnableMeasurementLines
        {
            get => _enableMeasurementLines;
            set => SetProperty(ref _enableMeasurementLines, value);
        }

        private bool _enableAreaMarkers = true;
        public bool EnableAreaMarkers
        {
            get => _enableAreaMarkers;
            set => SetProperty(ref _enableAreaMarkers, value);
        }

        private bool _enableDMOnlyNotes = true;
        public bool EnableDMOnlyNotes
        {
            get => _enableDMOnlyNotes;
            set => SetProperty(ref _enableDMOnlyNotes, value);
        }

        #endregion

        #region Performance Settings

        private int _maxLightSources = 20;
        public int MaxLightSources
        {
            get => _maxLightSources;
            set => SetProperty(ref _maxLightSources, Math.Clamp(value, 1, 100));
        }

        private int _maxRaycastAngles = 512;
        public int MaxRaycastAngles
        {
            get => _maxRaycastAngles;
            set => SetProperty(ref _maxRaycastAngles, Math.Clamp(value, 64, 2048));
        }

        private int _maxPathfindingNodes = 10000;
        public int MaxPathfindingNodes
        {
            get => _maxPathfindingNodes;
            set => SetProperty(ref _maxPathfindingNodes, Math.Clamp(value, 1000, 50000));
        }

        private bool _enablePerformanceMode = false;
        public bool EnablePerformanceMode
        {
            get => _enablePerformanceMode;
            set => SetProperty(ref _enablePerformanceMode, value);
        }

        #endregion

        #region Persistence

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save dev settings: {ex.Message}");
            }
        }

        public static DevSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<DevSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load dev settings: {ex.Message}");
            }

            return new DevSettings();
        }

        public void ResetToDefaults()
        {
            var defaults = new DevSettings();
            foreach (var prop in typeof(DevSettings).GetProperties())
            {
                if (prop.CanWrite && prop.CanRead && prop.Name != nameof(Instance))
                {
                    prop.SetValue(this, prop.GetValue(defaults));
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Diagonal movement rules for D&D
    /// </summary>
    public enum DiagonalMovementRule
    {
        /// <summary>Standard D&D 5e: alternating 5-10-5 feet</summary>
        Standard5105,
        /// <summary>Simple: every diagonal is 5 feet</summary>
        Simple5,
        /// <summary>Euclidean distance calculation</summary>
        Euclidean,
        /// <summary>Manhattan distance (no diagonals)</summary>
        Manhattan
    }
}
