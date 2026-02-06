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
    }
}
