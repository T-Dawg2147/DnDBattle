using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DnDBattle
{
    /// <summary>
    /// Runtime-configurable options (edit here to tune behavior).
    /// These are public static fields so you can quickly adjust values in code.
    /// </summary>
    public static class Options
    {
        /// <summary>
        /// Default images path, used for both Images and Icons.
        /// </summary>
        public static string DefaultTokenImagePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Entites", "Tokens");

        /// <summary>
        /// Lighing / shadow softness in pixels (applied via BlurEffect radius).
        /// </summary>
        public static double ShadowSoftnessPx { get; set; } = 6.0;

        /// <summary>
        /// Path animation speed in squares per second.
        /// </summary>
        public static double PathSpeedSquaresPerSecond { get; set; } = 3.0;

        /// <summary>
        /// Maximum number of angles when generating rays for lighting (caps work per-light).
        /// Larger numbers give smoother shadows but cost more CPU.
        /// </summary>
        public static int MaxRaycaseAngles { get; set; } = 1024;

        /// <summary>
        /// Limit for A* search nodes / safe-guard.
        /// </summary>
        public static int MaxAStarNodes { get; set; } = 20000;

        public static bool DefaultLockToGrid { get; set; } = true;

        public static bool EnabledPeriodicAutosave { get; set; } = true;
        public static int AutosaveIntervalSeconds { get; set; } = 300;

        public static double DefaultGridCellSize { get; set; } = 48.0;
        public static int GridMaxWidth { get; set; } = 100;
        public static int GridMaxHeight { get; set; } = 100;

        public static bool AutoResolveAOOs { get; set; } = true;

        /// <summary>
        /// Live mode dictates if manual dice will be used (true) or not (false). True by default.
        /// </summary>
        public static bool LiveMode { get; set; } = true;

        public static int UndoStackLimit { get; set; } = 200;

        // Turn Timer
        public static bool TurnTimerEnabled { get; set; } = false;
        public static int TurnTimerSeconds { get; set; } = 120; // Default 2 minutes

        // Sound Effects
        public static bool SoundEffectsEnabled { get; set; } = true;
        public static double SoundEffectsVolume { get; set; } = 0.5;

        // Combat Statistics
        public static bool TrackCombatStatistics { get; set; } = true;

        // Dice History
        public static int DiceHistoryMaxSize { get; set; } = 500;

        // ── Lighting & Vision System ──

        /// <summary>
        /// Enable the lighting system (light sources on the map).
        /// </summary>
        public static bool EnableLighting { get; set; } = true;

        /// <summary>
        /// Enable shadow casting from walls for light sources.
        /// </summary>
        public static bool EnableShadowCasting { get; set; } = true;

        /// <summary>
        /// Enable the per-token vision system.
        /// </summary>
        public static bool EnableTokenVision { get; set; } = true;

        /// <summary>
        /// Enable vision-mode rendering (darkvision grayscale, blindsight silhouettes).
        /// </summary>
        public static bool EnableVisionModeRendering { get; set; } = true;

        /// <summary>
        /// Automatically reveal fog of war based on token vision.
        /// </summary>
        public static bool EnableAutoFogReveal { get; set; } = true;

        /// <summary>
        /// Enable directional / cone lights on the map.
        /// </summary>
        public static bool EnableDirectionalLights { get; set; } = true;

        /// <summary>
        /// Default bright light radius for new lights (in grid squares).
        /// </summary>
        public static double DefaultBrightLightRadius { get; set; } = 4.0;

        /// <summary>
        /// Default dim light radius for new lights (in grid squares).
        /// </summary>
        public static double DefaultDimLightRadius { get; set; } = 8.0;

        /// <summary>
        /// Number of rays used for shadow casting per light. Higher = smoother but slower.
        /// </summary>
        public static int ShadowCastRayCount { get; set; } = 180;

        /// <summary>
        /// Shadow quality tier: 0 = Low (no blur), 1 = Medium, 2 = High, 3 = Ultra.
        /// </summary>
        public static int ShadowQualityTier { get; set; } = 1;

        /// <summary>
        /// Fog reveal mode: 0 = Exploration (once revealed, stays), 1 = Dynamic (only current vision),
        /// 2 = Hybrid (revealed areas shown dimmer).
        /// </summary>
        public static int FogRevealMode { get; set; } = 0;

        /// <summary>
        /// Default vision range for tokens without explicit vision settings (in grid squares).
        /// </summary>
        public static int DefaultTokenVisionRange { get; set; } = 12;

        // ── Phase 5: Advanced Token Features ──

        /// <summary>Enable A* pathfinding with diagonal support and difficult terrain.</summary>
        public static bool EnablePathfinding { get; set; } = true;

        /// <summary>Allow diagonal movement in pathfinding (costs 1.5 squares each).</summary>
        public static bool AllowDiagonalMovement { get; set; } = true;

        /// <summary>Maximum search depth for pathfinding in squares (prevents runaway searches).</summary>
        public static int PathfindingMaxDepth { get; set; } = 60;

        /// <summary>Enable movement cost preview overlay when hovering.</summary>
        public static bool EnableMovementCostPreview { get; set; } = true;

        /// <summary>Enable smooth path animation when tokens move along a path.</summary>
        public static bool EnablePathAnimation { get; set; } = true;

        /// <summary>Seconds per square during path animation.</summary>
        public static double PathAnimationSecondsPerSquare { get; set; } = 0.3;

        /// <summary>Enable Attack of Opportunity detection during movement.</summary>
        public static bool EnableAOODetection { get; set; } = true;

        /// <summary>Enable token aura rendering (Paladin aura, Spirit Guardians, etc.).</summary>
        public static bool EnableTokenAuras { get; set; } = true;

        /// <summary>Enable token elevation tracking and visual display.</summary>
        public static bool EnableTokenElevation { get; set; } = true;

        /// <summary>Enable token facing/direction indicator.</summary>
        public static bool EnableTokenFacing { get; set; } = true;

        /// <summary>Auto-face tokens in their movement direction.</summary>
        public static bool AutoFaceMovementDirection { get; set; } = true;

        /// <summary>Enable flanking detection based on token positioning.</summary>
        public static bool EnableFlankingDetection { get; set; } = true;

        // ── Phase 6: Area Effects Expansion ──

        /// <summary>Enable the spell templates library (search/browse/favorites).</summary>
        public static bool EnableSpellLibrary { get; set; } = true;

        /// <summary>Enable duration tracking for area effects (countdown per round).</summary>
        public static bool EnableDurationTracking { get; set; } = true;

        /// <summary>Enable damage-over-time auto-application for area effects.</summary>
        public static bool EnableDamageOverTime { get; set; } = true;

        /// <summary>Auto-apply DoT damage (false = manual/reminder only).</summary>
        public static bool AutoApplyDotDamage { get; set; } = true;

        /// <summary>Enable custom polygon area effects (click vertices to draw).</summary>
        public static bool EnablePolygonEffects { get; set; } = true;

        /// <summary>Enable area effect animations (pulsing, particles, rotation).</summary>
        public static bool EnableEffectAnimations { get; set; } = true;

        /// <summary>Maximum particles per animated effect (performance tuning).</summary>
        public static int MaxParticlesPerEffect { get; set; } = 50;

        /// <summary>Default animation type for newly placed effects.</summary>
        public static int DefaultAnimationType { get; set; } = 1; // 0=None, 1=Pulse, 2=Particle, 3=Rotate

        // ── Phase 7: Combat Automation ──

        /// <summary>Enable the automated attack roll system (d20 + modifiers vs AC).</summary>
        public static bool EnableAttackRollSystem { get; set; } = true;

        /// <summary>Enable automated saving throw rolls (d20 + modifier vs DC).</summary>
        public static bool EnableSavingThrowAutomation { get; set; } = true;

        /// <summary>Enable spell slot tracking and consumption on cast.</summary>
        public static bool EnableSpellSlotTracking { get; set; } = true;

        /// <summary>Enable concentration tracking with auto-checks on damage.</summary>
        public static bool EnableConcentrationTracking { get; set; } = true;

        /// <summary>Enable condition-based mechanical effects (advantage/disadvantage, auto-fail saves, auto-crits).</summary>
        public static bool EnableConditionAutomation { get; set; } = true;

        /// <summary>Enable the cover system (half/three-quarters/full cover modifying AC and DEX saves).</summary>
        public static bool EnableCoverSystem { get; set; } = true;

        /// <summary>Auto-roll saving throws for monster tokens (no player prompt).</summary>
        public static bool AutoRollMonsterSaves { get; set; } = true;

        /// <summary>Auto-apply damage to targets on a successful hit.</summary>
        public static bool AutoApplyDamage { get; set; } = true;

        /// <summary>Auto-prompt concentration check when a concentrating token takes damage.</summary>
        public static bool AutoPromptConcentrationCheck { get; set; } = true;

        // ── Phase 8: Advanced Map Features ──

        /// <summary>Enable the multi-map management system (map library, quick-switch, map linking).</summary>
        public static bool EnableMultiMapManagement { get; set; } = true;

        /// <summary>Maximum number of recently used maps shown in the map library.</summary>
        public static int MapLibraryMaxRecent { get; set; } = 10;

        /// <summary>Enable background image layers on maps.</summary>
        public static bool EnableBackgroundLayers { get; set; } = true;

        /// <summary>Enable hexagonal grid support (flat-top and pointy-top).</summary>
        public static bool EnableHexGrid { get; set; } = true;

        /// <summary>Enable gridless (free-form) token placement mode.</summary>
        public static bool EnableGridlessMode { get; set; } = true;

        /// <summary>Enable custom grid size configuration (feet per square).</summary>
        public static bool EnableCustomGridSizes { get; set; } = true;

        /// <summary>Default feet per grid square for new maps.</summary>
        public static int DefaultFeetPerSquare { get; set; } = 5;

        /// <summary>Enable map notes and labels on the grid.</summary>
        public static bool EnableMapNotes { get; set; } = true;

        /// <summary>Default font size for new map notes.</summary>
        public static double MapNoteDefaultFontSize { get; set; } = 12;

        /// <summary>Show DM-only notes in the map view (true = visible to DM, false = hidden entirely).</summary>
        public static bool ShowDMOnlyNotes { get; set; } = true;

        // ── Undecided Features: 2.5D Elevation System ──

        /// <summary>Enable the 2.5D terrain elevation system (height layers, 3D distance, falling damage).</summary>
        public static bool EnableElevationSystem { get; set; } = false;

        // ── Dynamic Weather & Environment ──

        /// <summary>Enable dynamic weather particle effects on the map.</summary>
        public static bool EnableWeatherEffects { get; set; } = false;

        /// <summary>Enable the day/night cycle lighting overlay.</summary>
        public static bool EnableDayNightCycle { get; set; } = false;

        /// <summary>Maximum weather particles allowed (performance tuning).</summary>
        public static int WeatherMaxParticles { get; set; } = 300;

        // ── Undecided Features: Animated Tiles ──

        /// <summary>Enable animated tile rendering (water, fire, torch, magic effects).</summary>
        public static bool EnableAnimatedTiles { get; set; } = false;

        // ── Undecided Features: Procedural Map Generation ──

        /// <summary>Enable procedural map generation (BSP dungeons, cellular automata caves).</summary>
        public static bool EnableProceduralMapGeneration { get; set; } = false;

        // ── Undecided Features: Advanced Token Customization ──

        /// <summary>Enable advanced token customization (borders, shapes, name plates, overlays).</summary>
        public static bool EnableTokenCustomization { get; set; } = false;

        // ── Undecided Features: Persistent Measurements ──

        /// <summary>Enable persistent measurement templates on the battle map.</summary>
        public static bool EnableMeasurements { get; set; } = false;

        // ── Undecided Features: 3D Dice Roller ──

        /// <summary>Enable 3D dice roller with physics simulation.</summary>
        public static bool EnableDiceRoller3D { get; set; } = false;

        /// <summary>Enable dice roll statistics tracking.</summary>
        public static bool EnableDiceStatistics { get; set; } = false;

        // ── Undecided Features: Accessibility Suite ──

        /// <summary>Enable high contrast mode for maximum readability.</summary>
        public static bool EnableHighContrast { get; set; } = false;

        /// <summary>Enable colorblind-friendly color palettes.</summary>
        public static bool EnableColorblindMode { get; set; } = false;

        /// <summary>UI scale percentage (100 = normal, 150 = 150%, 200 = 200%, up to 300).</summary>
        public static int AccessibilityUIScale { get; set; } = 100;

        /// <summary>Enable dyslexia-friendly font (OpenDyslexic/Verdana).</summary>
        public static bool EnableDyslexiaFont { get; set; } = false;

        /// <summary>Enable full keyboard navigation mode.</summary>
        public static bool EnableKeyboardNavigation { get; set; } = false;

        /// <summary>Enable multiplayer networking (Phase 9).</summary>
        public static bool EnableNetworking { get; set; } = false;

        /// <summary>Timeout in seconds for network connection attempts.</summary>
        public static int NetworkConnectionTimeoutSeconds { get; set; } = 10;

        /// <summary>Enable voice chat integration (Discord) for Phase 9 multiplayer.</summary>
        public static bool EnableVoiceChat { get; set; } = false;
    }
}
