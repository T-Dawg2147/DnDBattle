using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle
{
    /// <summary>
    /// Runtime-configurable options (edit here to tune behavior).
    /// These are public static fields so you can quickly adjust values in code.
    /// </summary>
    public static class Options
    {
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
        public static int GridMaxHeight { get; set; } = 60;

        public static bool AutoResolveAOOs { get; set; } = true;

        /// <summary>
        /// Live mode dictates if manual dice will be used (true) or not (false). True by default.
        /// </summary>
        public static bool LiveMode { get; set; } = false;

        public static int UndoStackLimit { get; set; } = 200;
    }
}
