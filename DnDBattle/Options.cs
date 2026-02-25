using OptionsLib.Core.Attributes;
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
    /// Properties marked [Option] are persisted to app-settings.json.
    /// Properties marked [OptionUI] are surfaced in the settings UI automatically.
    /// TODO: Ensure all [Option]-marked properties are loaded from settings on startup via OptionsService.
    /// </summary>
    public static class Options
    {
        // ── General / File Paths ──

        /// <summary>
        /// Default images path, used for both Images and Icons.
        /// </summary>
        // TODO: Load from settings (DefaultTokenImagePath)
        [Option]
        [OptionUI]
        public static string DefaultTokenImagePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Entites", "Tokens");

        // ── Rendering / Lighting ──

        /// <summary>
        /// Lighting / shadow softness in pixels (applied via BlurEffect radius).
        /// </summary>
        // TODO: Load from settings (ShadowSoftnessPx)
        [Option]
        [OptionUI]
        public static double ShadowSoftnessPx { get; set; } = 6.0;

        /// <summary>
        /// Path animation speed in squares per second.
        /// </summary>
        // TODO: Load from settings (PathSpeedSquaresPerSecond)
        [Option]
        [OptionUI]
        public static double PathSpeedSquaresPerSecond { get; set; } = 3.0;

        /// <summary>
        /// Maximum number of angles when generating rays for lighting (caps work per-light).
        /// Larger numbers give smoother shadows but cost more CPU.
        /// </summary>
        // TODO: Load from settings (MaxRaycaseAngles)
        [Option]
        [OptionUI]
        public static int MaxRaycaseAngles { get; set; } = 1024;

        /// <summary>
        /// Limit for A* search nodes / safe-guard.
        /// </summary>
        // TODO: Load from settings (MaxAStarNodes)
        [Option]
        [OptionUI]
        public static int MaxAStarNodes { get; set; } = 20000;

        // ── Grid ──

        // TODO: Load from settings (DefaultLockToGrid)
        [Option]
        [OptionUI]
        public static bool DefaultLockToGrid { get; set; } = true;

        // TODO: Load from settings (DefaultGridCellSize)
        [Option]
        [OptionUI]
        public static double DefaultGridCellSize { get; set; } = 48.0;

        // TODO: Load from settings (GridMaxWidth)
        [Option]
        [OptionUI]
        public static int GridMaxWidth { get; set; } = 100;

        // TODO: Load from settings (GridMaxHeight)
        [Option]
        [OptionUI]
        public static int GridMaxHeight { get; set; } = 100;

        // ── Autosave ──

        // TODO: Load from settings (EnabledPeriodicAutosave)
        [Option]
        [OptionUI]
        public static bool EnabledPeriodicAutosave { get; set; } = true;

        // TODO: Load from settings (AutosaveIntervalSeconds)
        [Option]
        [OptionUI]
        public static int AutosaveIntervalSeconds { get; set; } = 300;

        // ── Combat Rules ──

        // TODO: Load from settings (AutoResolveAOOs)
        [Option]
        [OptionUI]
        public static bool AutoResolveAOOs { get; set; } = true;

        /// <summary>
        /// Live mode dictates if manual dice will be used (true) or not (false). True by default.
        /// </summary>
        // TODO: Load from settings (LiveMode)
        [Option]
        [OptionUI]
        public static bool LiveMode { get; set; } = true;

        // TODO: Load from settings (UndoStackLimit)
        [Option]
        [OptionUI]
        public static int UndoStackLimit { get; set; } = 200;

        // ── Turn Timer ──

        // TODO: Load from settings (TurnTimerEnabled)
        [Option]
        [OptionUI]
        public static bool TurnTimerEnabled { get; set; } = false;

        // TODO: Load from settings (TurnTimerSeconds)
        [Option]
        [OptionUI]
        public static int TurnTimerSeconds { get; set; } = 120; // Default 2 minutes

        // ── Sound Effects ──

        // TODO: Load from settings (SoundEffectsEnabled)
        [Option]
        [OptionUI]
        public static bool SoundEffectsEnabled { get; set; } = true;

        // TODO: Load from settings (SoundEffectsVolume)
        [Option]
        [OptionUI]
        public static double SoundEffectsVolume { get; set; } = 0.5;

        // ── Combat Statistics ──

        // TODO: Load from settings (TrackCombatStatistics)
        [Option]
        [OptionUI]
        public static bool TrackCombatStatistics { get; set; } = true;

        // ── Dice History ──

        // TODO: Load from settings (DiceHistoryMaxSize)
        [Option]
        [OptionUI]
        public static int DiceHistoryMaxSize { get; set; } = 500;

        // ── Lighting & Vision System ──

        /// <summary>
        /// Enable the lighting system (light sources on the map).
        /// </summary>
        // TODO: Load from settings (EnableLighting)
        [Option]
        [OptionUI]
        public static bool EnableLighting { get; set; } = true;

        /// <summary>
        /// Enable shadow casting from walls for light sources.
        /// </summary>
        // TODO: Load from settings (EnableShadowCasting)
        [Option]
        [OptionUI]
        public static bool EnableShadowCasting { get; set; } = true;

        /// <summary>
        /// Enable the per-token vision system.
        /// </summary>
        // TODO: Load from settings (EnableTokenVision)
        [Option]
        [OptionUI]
        public static bool EnableTokenVision { get; set; } = true;

        /// <summary>
        /// Enable vision-mode rendering (darkvision grayscale, blindsight silhouettes).
        /// </summary>
        // TODO: Load from settings (EnableVisionModeRendering)
        [Option]
        [OptionUI]
        public static bool EnableVisionModeRendering { get; set; } = true;

        /// <summary>
        /// Automatically reveal fog of war based on token vision.
        /// </summary>
        // TODO: Load from settings (EnableAutoFogReveal)
        [Option]
        [OptionUI]
        public static bool EnableAutoFogReveal { get; set; } = true;

        /// <summary>
        /// Enable directional / cone lights on the map.
        /// </summary>
        // TODO: Load from settings (EnableDirectionalLights)
        [Option]
        [OptionUI]
        public static bool EnableDirectionalLights { get; set; } = true;

        /// <summary>
        /// Default bright light radius for new lights (in grid squares).
        /// </summary>
        // TODO: Load from settings (DefaultBrightLightRadius)
        [Option]
        [OptionUI]
        public static double DefaultBrightLightRadius { get; set; } = 4.0;

        /// <summary>
        /// Default dim light radius for new lights (in grid squares).
        /// </summary>
        // TODO: Load from settings (DefaultDimLightRadius)
        [Option]
        [OptionUI]
        public static double DefaultDimLightRadius { get; set; } = 8.0;

        /// <summary>
        /// Number of rays used for shadow casting per light. Higher = smoother but slower.
        /// </summary>
        // TODO: Load from settings (ShadowCastRayCount)
        [Option]
        [OptionUI]
        public static int ShadowCastRayCount { get; set; } = 180;

        /// <summary>
        /// Shadow quality tier: 0 = Low (no blur), 1 = Medium, 2 = High, 3 = Ultra.
        /// </summary>
        // TODO: Load from settings (ShadowQualityTier)
        [Option]
        [OptionUI]
        public static int ShadowQualityTier { get; set; } = 1;

        /// <summary>
        /// Fog reveal mode: 0 = Exploration (once revealed, stays), 1 = Dynamic (only current vision),
        /// 2 = Hybrid (revealed areas shown dimmer).
        /// </summary>
        // TODO: Load from settings (FogRevealMode)
        [Option]
        [OptionUI]
        public static int FogRevealMode { get; set; } = 0;

        /// <summary>
        /// Default vision range for tokens without explicit vision settings (in grid squares).
        /// </summary>
        // TODO: Load from settings (DefaultTokenVisionRange)
        [Option]
        [OptionUI]
        public static int DefaultTokenVisionRange { get; set; } = 12;

        // ── Phase 5: Advanced Token Features ──

        /// <summary>Enable A* pathfinding with diagonal support and difficult terrain.</summary>
        // TODO: Load from settings (EnablePathfinding)
        [Option]
        [OptionUI]
        public static bool EnablePathfinding { get; set; } = true;

        /// <summary>Allow diagonal movement in pathfinding (costs 1.5 squares each).</summary>
        // TODO: Load from settings (AllowDiagonalMovement)
        [Option]
        [OptionUI]
        public static bool AllowDiagonalMovement { get; set; } = true;

        /// <summary>Maximum search depth for pathfinding in squares (prevents runaway searches).</summary>
        // TODO: Load from settings (PathfindingMaxDepth)
        [Option]
        [OptionUI]
        public static int PathfindingMaxDepth { get; set; } = 60;

        /// <summary>Enable movement cost preview overlay when hovering.</summary>
        // TODO: Load from settings (EnableMovementCostPreview)
        [Option]
        [OptionUI]
        public static bool EnableMovementCostPreview { get; set; } = true;

        /// <summary>Enable smooth path animation when tokens move along a path.</summary>
        // TODO: Load from settings (EnablePathAnimation)
        [Option]
        [OptionUI]
        public static bool EnablePathAnimation { get; set; } = true;

        /// <summary>Seconds per square during path animation.</summary>
        // TODO: Load from settings (PathAnimationSecondsPerSquare)
        [Option]
        [OptionUI]
        public static double PathAnimationSecondsPerSquare { get; set; } = 0.3;

        /// <summary>Enable Attack of Opportunity detection during movement.</summary>
        // TODO: Load from settings (EnableAOODetection)
        [Option]
        [OptionUI]
        public static bool EnableAOODetection { get; set; } = true;

        /// <summary>Enable token aura rendering (Paladin aura, Spirit Guardians, etc.).</summary>
        // TODO: Load from settings (EnableTokenAuras)
        [Option]
        [OptionUI]
        public static bool EnableTokenAuras { get; set; } = true;

        /// <summary>Enable token elevation tracking and visual display.</summary>
        // TODO: Load from settings (EnableTokenElevation)
        [Option]
        [OptionUI]
        public static bool EnableTokenElevation { get; set; } = true;

        /// <summary>Enable token facing/direction indicator.</summary>
        // TODO: Load from settings (EnableTokenFacing)
        [Option]
        [OptionUI]
        public static bool EnableTokenFacing { get; set; } = true;

        /// <summary>Auto-face tokens in their movement direction.</summary>
        // TODO: Load from settings (AutoFaceMovementDirection)
        [Option]
        [OptionUI]
        public static bool AutoFaceMovementDirection { get; set; } = true;

        /// <summary>Enable flanking detection based on token positioning.</summary>
        // TODO: Load from settings (EnableFlankingDetection)
        [Option]
        [OptionUI]
        public static bool EnableFlankingDetection { get; set; } = true;

        // ── Phase 6: Area Effects Expansion ──

        /// <summary>Enable the spell templates library (search/browse/favorites).</summary>
        // TODO: Load from settings (EnableSpellLibrary)
        [Option]
        [OptionUI]
        public static bool EnableSpellLibrary { get; set; } = true;

        /// <summary>Enable duration tracking for area effects (countdown per round).</summary>
        // TODO: Load from settings (EnableDurationTracking)
        [Option]
        [OptionUI]
        public static bool EnableDurationTracking { get; set; } = true;

        /// <summary>Enable damage-over-time auto-application for area effects.</summary>
        // TODO: Load from settings (EnableDamageOverTime)
        [Option]
        [OptionUI]
        public static bool EnableDamageOverTime { get; set; } = true;

        /// <summary>Auto-apply DoT damage (false = manual/reminder only).</summary>
        // TODO: Load from settings (AutoApplyDotDamage)
        [Option]
        [OptionUI]
        public static bool AutoApplyDotDamage { get; set; } = true;

        /// <summary>Enable custom polygon area effects (click vertices to draw).</summary>
        // TODO: Load from settings (EnablePolygonEffects)
        [Option]
        [OptionUI]
        public static bool EnablePolygonEffects { get; set; } = true;

        /// <summary>Enable area effect animations (pulsing, particles, rotation).</summary>
        // TODO: Load from settings (EnableEffectAnimations)
        [Option]
        [OptionUI]
        public static bool EnableEffectAnimations { get; set; } = true;

        /// <summary>Maximum particles per animated effect (performance tuning).</summary>
        // TODO: Load from settings (MaxParticlesPerEffect)
        [Option]
        [OptionUI]
        public static int MaxParticlesPerEffect { get; set; } = 50;

        /// <summary>Default animation type for newly placed effects.</summary>
        // TODO: Load from settings (DefaultAnimationType)
        [Option]
        [OptionUI]
        public static int DefaultAnimationType { get; set; } = 1; // 0=None, 1=Pulse, 2=Particle, 3=Rotate

        // ── Phase 7: Combat Automation ──

        /// <summary>Enable the automated attack roll system (d20 + modifiers vs AC).</summary>
        // TODO: Load from settings (EnableAttackRollSystem)
        [Option]
        [OptionUI]
        public static bool EnableAttackRollSystem { get; set; } = true;

        /// <summary>Enable automated saving throw rolls (d20 + modifier vs DC).</summary>
        // TODO: Load from settings (EnableSavingThrowAutomation)
        [Option]
        [OptionUI]
        public static bool EnableSavingThrowAutomation { get; set; } = true;

        /// <summary>Enable spell slot tracking and consumption on cast.</summary>
        // TODO: Load from settings (EnableSpellSlotTracking)
        [Option]
        [OptionUI]
        public static bool EnableSpellSlotTracking { get; set; } = true;

        /// <summary>Enable concentration tracking with auto-checks on damage.</summary>
        // TODO: Load from settings (EnableConcentrationTracking)
        [Option]
        [OptionUI]
        public static bool EnableConcentrationTracking { get; set; } = true;

        /// <summary>Enable condition-based mechanical effects (advantage/disadvantage, auto-fail saves, auto-crits).</summary>
        // TODO: Load from settings (EnableConditionAutomation)
        [Option]
        [OptionUI]
        public static bool EnableConditionAutomation { get; set; } = true;

        /// <summary>Enable the cover system (half/three-quarters/full cover modifying AC and DEX saves).</summary>
        // TODO: Load from settings (EnableCoverSystem)
        [Option]
        [OptionUI]
        public static bool EnableCoverSystem { get; set; } = true;

        /// <summary>Auto-roll saving throws for monster tokens (no player prompt).</summary>
        // TODO: Load from settings (AutoRollMonsterSaves)
        [Option]
        [OptionUI]
        public static bool AutoRollMonsterSaves { get; set; } = true;

        /// <summary>Auto-apply damage to targets on a successful hit.</summary>
        // TODO: Load from settings (AutoApplyDamage)
        [Option]
        [OptionUI]
        public static bool AutoApplyDamage { get; set; } = true;

        /// <summary>Auto-prompt concentration check when a concentrating token takes damage.</summary>
        // TODO: Load from settings (AutoPromptConcentrationCheck)
        [Option]
        [OptionUI]
        public static bool AutoPromptConcentrationCheck { get; set; } = true;

        // ── Phase 8: Advanced Map Features ──

        /// <summary>Enable the multi-map management system (map library, quick-switch, map linking).</summary>
        // TODO: Load from settings (EnableMultiMapManagement)
        [Option]
        [OptionUI]
        public static bool EnableMultiMapManagement { get; set; } = true;

        /// <summary>Maximum number of recently used maps shown in the map library.</summary>
        // TODO: Load from settings (MapLibraryMaxRecent)
        [Option]
        [OptionUI]
        public static int MapLibraryMaxRecent { get; set; } = 10;

        /// <summary>Enable background image layers on maps.</summary>
        // TODO: Load from settings (EnableBackgroundLayers)
        [Option]
        [OptionUI]
        public static bool EnableBackgroundLayers { get; set; } = true;

        /// <summary>Enable hexagonal grid support (flat-top and pointy-top).</summary>
        // TODO: Load from settings (EnableHexGrid)
        [Option]
        [OptionUI]
        public static bool EnableHexGrid { get; set; } = true;

        /// <summary>Enable gridless (free-form) token placement mode.</summary>
        // TODO: Load from settings (EnableGridlessMode)
        [Option]
        [OptionUI]
        public static bool EnableGridlessMode { get; set; } = true;

        /// <summary>Enable custom grid size configuration (feet per square).</summary>
        // TODO: Load from settings (EnableCustomGridSizes)
        [Option]
        [OptionUI]
        public static bool EnableCustomGridSizes { get; set; } = true;

        /// <summary>Default feet per grid square for new maps.</summary>
        // TODO: Load from settings (DefaultFeetPerSquare)
        [Option]
        [OptionUI]
        public static int DefaultFeetPerSquare { get; set; } = 5;

        /// <summary>Enable map notes and labels on the grid.</summary>
        // TODO: Load from settings (EnableMapNotes)
        [Option]
        [OptionUI]
        public static bool EnableMapNotes { get; set; } = true;

        /// <summary>Default font size for new map notes.</summary>
        // TODO: Load from settings (MapNoteDefaultFontSize)
        [Option]
        [OptionUI]
        public static double MapNoteDefaultFontSize { get; set; } = 12;

        /// <summary>Show DM-only notes in the map view (true = visible to DM, false = hidden entirely).</summary>
        // TODO: Load from settings (ShowDMOnlyNotes)
        [Option]
        [OptionUI]
        public static bool ShowDMOnlyNotes { get; set; } = true;

        // ── Undecided Features: 2.5D Elevation System ──

        /// <summary>Enable the 2.5D terrain elevation system (height layers, 3D distance, falling damage).</summary>
        // TODO: Load from settings (EnableElevationSystem)
        [Option]
        [OptionUI]
        public static bool EnableElevationSystem { get; set; } = false;

        // ── Dynamic Weather & Environment ──

        /// <summary>Enable dynamic weather particle effects on the map.</summary>
        // TODO: Load from settings (EnableWeatherEffects)
        [Option]
        [OptionUI]
        public static bool EnableWeatherEffects { get; set; } = false;

        /// <summary>Enable the day/night cycle lighting overlay.</summary>
        // TODO: Load from settings (EnableDayNightCycle)
        [Option]
        [OptionUI]
        public static bool EnableDayNightCycle { get; set; } = false;

        /// <summary>Maximum weather particles allowed (performance tuning).</summary>
        // TODO: Load from settings (WeatherMaxParticles)
        [Option]
        [OptionUI]
        public static int WeatherMaxParticles { get; set; } = 300;

        // ── Undecided Features: Animated Tiles ──

        /// <summary>Enable animated tile rendering (water, fire, torch, magic effects).</summary>
        // TODO: Load from settings (EnableAnimatedTiles)
        [Option]
        [OptionUI]
        public static bool EnableAnimatedTiles { get; set; } = false;

        // ── Undecided Features: Procedural Map Generation ──

        /// <summary>Enable procedural map generation (BSP dungeons, cellular automata caves).</summary>
        // TODO: Load from settings (EnableProceduralMapGeneration)
        [Option]
        [OptionUI]
        public static bool EnableProceduralMapGeneration { get; set; } = false;

        // ── Undecided Features: Advanced Token Customization ──

        /// <summary>Enable advanced token customization (borders, shapes, name plates, overlays).</summary>
        // TODO: Load from settings (EnableTokenCustomization)
        [Option]
        [OptionUI]
        public static bool EnableTokenCustomization { get; set; } = false;

        // ── Undecided Features: Persistent Measurements ──

        /// <summary>Enable persistent measurement templates on the battle map.</summary>
        // TODO: Load from settings (EnableMeasurements)
        [Option]
        [OptionUI]
        public static bool EnableMeasurements { get; set; } = false;

        // ── Undecided Features: 3D Dice Roller ──

        /// <summary>Enable 3D dice roller with physics simulation.</summary>
        // TODO: Load from settings (EnableDiceRoller3D)
        [Option]
        [OptionUI]
        public static bool EnableDiceRoller3D { get; set; } = false;

        /// <summary>Enable dice roll statistics tracking.</summary>
        // TODO: Load from settings (EnableDiceStatistics)
        [Option]
        [OptionUI]
        public static bool EnableDiceStatistics { get; set; } = false;

        // ── Undecided Features: Accessibility Suite ──

        /// <summary>Enable high contrast mode for maximum readability.</summary>
        // TODO: Load from settings (EnableHighContrast)
        [Option]
        [OptionUI]
        public static bool EnableHighContrast { get; set; } = false;

        /// <summary>Enable colorblind-friendly color palettes.</summary>
        // TODO: Load from settings (EnableColorblindMode)
        [Option]
        [OptionUI]
        public static bool EnableColorblindMode { get; set; } = false;

        /// <summary>UI scale percentage (100 = normal, 150 = 150%, 200 = 200%, up to 300).</summary>
        // TODO: Load from settings (AccessibilityUIScale)
        [Option]
        [OptionUI]
        public static int AccessibilityUIScale { get; set; } = 100;

        /// <summary>Enable dyslexia-friendly font (OpenDyslexic/Verdana).</summary>
        // TODO: Load from settings (EnableDyslexiaFont)
        [Option]
        [OptionUI]
        public static bool EnableDyslexiaFont { get; set; } = false;

        /// <summary>Enable full keyboard navigation mode.</summary>
        // TODO: Load from settings (EnableKeyboardNavigation)
        [Option]
        [OptionUI]
        public static bool EnableKeyboardNavigation { get; set; } = false;

        // ── Phase 9: Multiplayer / Networking ──

        /// <summary>Enable multiplayer networking system (host/join game sessions).</summary>
        // TODO: Load from settings (EnableNetworking)
        [Option]
        [OptionUI]
        public static bool EnableNetworking { get; set; } = false;

        /// <summary>Default TCP port for the multiplayer game server.</summary>
        // TODO: Load from settings (NetworkDefaultPort)
        [Option]
        [OptionUI]
        public static int NetworkDefaultPort { get; set; } = 7777;

        /// <summary>Timeout in seconds for network connection attempts.</summary>
        // TODO: Load from settings (NetworkConnectionTimeoutSeconds)
        [Option]
        [OptionUI]
        public static int NetworkConnectionTimeoutSeconds { get; set; } = 10;

        /// <summary>Enable real-time fog of war synchronization across clients.</summary>
        // TODO: Load from settings (EnableFogSync)
        [Option]
        [OptionUI]
        public static bool EnableFogSync { get; set; } = false;

        /// <summary>Enable the multiplayer chat system (text messages and dice rolls).</summary>
        // TODO: Load from settings (EnableMultiplayerChat)
        [Option]
        [OptionUI]
        public static bool EnableMultiplayerChat { get; set; } = false;

        /// <summary>Enable client-side prediction for token movement (reduces perceived lag).</summary>
        // TODO: Load from settings (EnableClientPrediction)
        [Option]
        [OptionUI]
        public static bool EnableClientPrediction { get; set; } = true;

        /// <summary>Enable voice chat integration (Discord link sharing).</summary>
        // TODO: Load from settings (EnableVoiceChat)
        [Option]
        [OptionUI]
        public static bool EnableVoiceChat { get; set; } = false;

        /// <summary>Enable cloud save and sync for encounters.</summary>
        // TODO: Load from settings (EnableCloudSave)
        [Option]
        [OptionUI]
        public static bool EnableCloudSave { get; set; } = false;

        /// <summary>URL of the self-hosted cloud save server (e.g. "https://my-server.example.com").</summary>
        // TODO: Load from settings (CloudSaveServerUrl)
        [Option]
        [OptionUI]
        public static string CloudSaveServerUrl { get; set; } = string.Empty;

        // ── Recommended Additions: Fog of War UI Defaults ──
        // These correspond to FogOfWarService.DmFogOpacity, PlayerFogOpacity, and BrushSize,
        // which are set per-session but have no persistent defaults. Adding them here lets the
        // DM configure their preferred fog-of-war look without changing it every session.

        /// <summary>
        /// Fog opacity seen by the DM for hidden (unrevealed) areas (0.0 = invisible, 1.0 = fully opaque).
        /// Based on FogOfWarService.DmFogOpacity (default 0.5).
        /// </summary>
        [Option]
        [OptionUI]
        public static double DefaultFogOpacityDm { get; set; } = 0.5;

        /// <summary>
        /// Fog opacity seen by players for hidden areas (0.0 = invisible, 1.0 = fully opaque).
        /// Based on FogOfWarService.PlayerFogOpacity (default 1.0).
        /// </summary>
        [Option]
        [OptionUI]
        public static double DefaultFogOpacityPlayer { get; set; } = 1.0;

        /// <summary>
        /// Default fog-of-war brush size in grid squares for manual fog painting.
        /// Based on FogOfWarService.BrushSize (default 3).
        /// </summary>
        [Option]
        [OptionUI]
        public static int DefaultFogBrushSizeSquares { get; set; } = 3;

        // ── Recommended Additions: Token Display Preferences ──
        // DMs commonly want to toggle these visual overlays on/off for cleaner presentation.
        // These are display-only flags; no existing backing field exists yet but they are
        // expected to be checked wherever tokens are rendered on the BattleGridControl.

        /// <summary>
        /// Show the token's name label above it on the battle grid.
        /// Recommended addition: lets DMs hide labels for a cleaner map presentation.
        /// </summary>
        [Option]
        [OptionUI]
        public static bool ShowTokenNameLabels { get; set; } = true;

        /// <summary>
        /// Show HP bars below each token on the battle grid.
        /// Recommended addition: lets DMs hide HP bars when using narrative-style damage tracking.
        /// </summary>
        [Option]
        [OptionUI]
        public static bool ShowTokenHPBars { get; set; } = true;

        /// <summary>
        /// Show condition icons (e.g. poisoned, stunned) on tokens in the battle grid.
        /// Recommended addition: mirrors Token.Conditions but as a global display toggle.
        /// </summary>
        [Option]
        [OptionUI]
        public static bool ShowTokenConditionIcons { get; set; } = true;

        // ── Recommended Additions: Combat Rule Options ──
        // D&D 5e has optional rules DMs commonly want to toggle. These feed into
        // InitiativeManager and AttackRollSystem but currently have no persistent defaults.

        /// <summary>
        /// Break initiative ties by comparing DEX scores (D&D 5e DMG rule).
        /// If false, ties are left for the DM to resolve manually.
        /// Recommended addition: corresponds to the tie-breaking logic in InitiativeManager.
        /// </summary>
        [Option]
        [OptionUI]
        public static bool InitiativeTiebreakerUseDex { get; set; } = true;

        /// <summary>
        /// Allow players to roll death saving throws publicly (visible to the table).
        /// If false, death saves are rolled privately (DM-only).
        /// Recommended addition: based on Token.RecordDeathSave — gives DMs control over table tension.
        /// </summary>
        [Option]
        [OptionUI]
        public static bool DeathSavesPublic { get; set; } = true;

        // ── Recommended Additions: Grid Appearance ──
        // These complement DefaultGridCellSize and DefaultLockToGrid already in Options,
        // letting users customize the look of the grid itself.

        /// <summary>
        /// Color of the battle grid lines as an ARGB hex string (e.g. "#FF444444").
        /// Recommended addition: lets users match grid color to their map art style.
        /// </summary>
        [Option]
        [OptionUI]
        public static string GridLineColor { get; set; } = "#FF444444";

        /// <summary>
        /// Opacity of the battle grid lines (0.0 = invisible, 1.0 = fully opaque).
        /// Recommended addition: complements GridLineColor for finer grid visibility control.
        /// </summary>
        [Option]
        [OptionUI]
        public static double GridLineOpacity { get; set; } = 0.4;

        // ── Recommended Additions: Procedural Map Defaults ──
        // ProceduralMapConfig already has Width/Height properties but they reset every time
        // the dialog opens. Storing defaults here lets users set their preferred map size once.

        /// <summary>
        /// Default width in grid cells for procedurally generated maps.
        /// Based on ProceduralMapConfig.Width (default 50).
        /// </summary>
        [Option]
        [OptionUI]
        public static int ProceduralMapDefaultWidth { get; set; } = 50;

        /// <summary>
        /// Default height in grid cells for procedurally generated maps.
        /// Based on ProceduralMapConfig.Height (default 50).
        /// </summary>
        [Option]
        [OptionUI]
        public static int ProceduralMapDefaultHeight { get; set; } = 50;

        // ── Recommended Additions: Sound Effects Path ──
        // SoundEffectsService hard-codes its sounds directory relative to the executable.
        // Exposing it here lets users point to a custom sound pack without recompiling.

        /// <summary>
        /// Directory path for custom sound effect files.
        /// Based on SoundEffectsService._soundsDirectory (default: {AppDir}\Sounds).
        /// Leave empty to use the default application sounds folder.
        /// </summary>
        [Option]
        [OptionUI]
        public static string SoundEffectsDirectory { get; set; } = string.Empty;

        // ── Recommended Additions: Colorblind Mode Selection ──
        // EnableColorblindMode already exists but there is no persistent option for *which*
        // colorblind mode to use (Protanopia, Deuteranopia, Tritanopia). AccessibilityService
        // holds this as an in-memory enum; exposing it here allows it to be saved/loaded.

        /// <summary>
        /// Active colorblind palette: 0 = Normal, 1 = Protanopia, 2 = Deuteranopia, 3 = Tritanopia.
        /// Based on AccessibilityService.ColorblindMode (in-memory only until now).
        /// Only applied when EnableColorblindMode is true.
        /// </summary>
        [Option]
        [OptionUI]
        public static int ColorblindModeSelection { get; set; } = 0;
    }
}
