using System;

namespace DnDBattle.Models
{
    /// <summary>
    /// Defines an animated tile with spritesheet-based frame animation.
    /// Uses UV-coordinate animation on a single spritesheet image for efficiency.
    /// </summary>
    public class AnimatedTileDefinition
    {
        /// <summary>Unique identifier for this animated tile type.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Display name (e.g., "Flowing Water", "Flickering Torch").</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Path to the spritesheet image containing all frames.</summary>
        public string SpritesheetPath { get; set; } = string.Empty;

        /// <summary>Number of frames in the animation.</summary>
        public int FrameCount { get; set; } = 4;

        /// <summary>Width of each frame in pixels.</summary>
        public int FrameWidth { get; set; } = 48;

        /// <summary>Height of each frame in pixels.</summary>
        public int FrameHeight { get; set; } = 48;

        /// <summary>Frames per second for this animation.</summary>
        public double FramesPerSecond { get; set; } = 8.0;

        /// <summary>Whether the animation loops (true) or plays once (false).</summary>
        public bool IsLooping { get; set; } = true;

        /// <summary>Whether frames are arranged horizontally (true) or vertically (false) in the spritesheet.</summary>
        public bool IsHorizontalLayout { get; set; } = true;

        /// <summary>Animation category for organizing in the tile palette.</summary>
        public AnimatedTileCategory Category { get; set; } = AnimatedTileCategory.Water;

        /// <summary>Whether to use random frame offset per instance (prevents all tiles animating in sync).</summary>
        public bool RandomStartFrame { get; set; } = true;
    }

    /// <summary>
    /// Categories for animated tile types.
    /// </summary>
    public enum AnimatedTileCategory
    {
        Water,
        Fire,
        Magic,
        Nature,
        Hazard,
        Atmospheric
    }
}
