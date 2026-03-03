using System;
using System.Collections.Generic;
using System.Windows;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Encounters;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Effects
{
    /// <summary>
    /// Manages animated tile rendering with viewport culling, LOD, and frame skipping.
    /// Pre-allocates all per-tile state to avoid GC pressure during animation loops.
    /// </summary>
    public sealed class AnimatedTileService
    {
        /// <summary>Per-instance animation state (pre-allocated, pooled).</summary>
        public sealed class TileAnimState
        {
            public string DefinitionId;
            public int GridX;
            public int GridY;
            public int CurrentFrame;
            public double FrameTimer;
            public bool IsVisible;
            public bool IsCompleted; // for non-looping animations

            public void Reset()
            {
                CurrentFrame = 0;
                FrameTimer = 0;
                IsVisible = true;
                IsCompleted = false;
            }
        }

        // Pool of animation states, keyed by (gridX, gridY) for O(1) lookup
        private readonly Dictionary<long, TileAnimState> _activeAnimations = new();
        private readonly Dictionary<string, AnimatedTileDefinition> _definitions = new();

        // Performance tuning
        private int _visibleAnimatedTileCount;
        private double _currentEffectiveFps;
        private const int FrameSkipThreshold = 50; // reduce FPS when this many tiles visible
        private const double ReducedFps = 15.0;
        private const double MinZoomForAnimation = 0.3; // stop animating below this zoom

        /// <summary>Current viewport for culling.</summary>
        public Rect ViewportBounds { get; set; } = new Rect(0, 0, 1920, 1080);

        /// <summary>Current zoom level (1.0 = 100%).</summary>
        public double ZoomLevel { get; set; } = 1.0;

        /// <summary>Grid cell size in pixels.</summary>
        public double CellSize { get; set; } = 48.0;

        /// <summary>
        /// Registers an animated tile definition for use.
        /// </summary>
        public void RegisterDefinition(AnimatedTileDefinition definition)
        {
            _definitions[definition.Id] = definition;
        }

        /// <summary>
        /// Gets a registered definition by ID.
        /// </summary>
        public AnimatedTileDefinition? GetDefinition(string id)
        {
            return _definitions.TryGetValue(id, out var def) ? def : null;
        }

        /// <summary>
        /// Places an animated tile on the grid. Lazy-initializes animation state.
        /// </summary>
        public void PlaceAnimatedTile(string definitionId, int gridX, int gridY)
        {
            if (!Options.EnableAnimatedTiles) return;
            if (!_definitions.ContainsKey(definitionId)) return;

            long key = PackKey(gridX, gridY);
            if (!_activeAnimations.TryGetValue(key, out var state))
            {
                state = new TileAnimState();
                _activeAnimations[key] = state;
            }

            state.DefinitionId = definitionId;
            state.GridX = gridX;
            state.GridY = gridY;
            state.Reset();

            // Random start frame to prevent sync
            var def = _definitions[definitionId];
            if (def.RandomStartFrame && def.FrameCount > 1)
            {
                state.CurrentFrame = Random.Shared.Next(def.FrameCount);
                state.FrameTimer = Random.Shared.NextDouble() * (1.0 / def.FramesPerSecond);
            }
        }

        /// <summary>
        /// Removes an animated tile from the grid.
        /// </summary>
        public void RemoveAnimatedTile(int gridX, int gridY)
        {
            _activeAnimations.Remove(PackKey(gridX, gridY));
        }

        /// <summary>
        /// Updates all animated tile frames. Uses viewport culling and LOD.
        /// deltaTime in seconds for frame-rate independence.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!Options.EnableAnimatedTiles) return;

            // LOD: don't animate if zoomed out too far
            if (ZoomLevel < MinZoomForAnimation) return;

            _visibleAnimatedTileCount = 0;

            foreach (var kvp in _activeAnimations)
            {
                var state = kvp.Value;
                if (state.IsCompleted) continue;

                if (!_definitions.TryGetValue(state.DefinitionId, out var def)) continue;

                // Viewport culling
                double px = state.GridX * CellSize;
                double py = state.GridY * CellSize;
                state.IsVisible = ViewportBounds.Contains(px, py);

                if (!state.IsVisible) continue;

                _visibleAnimatedTileCount++;

                // Frame skipping: use reduced FPS when many tiles visible
                double effectiveFps = _visibleAnimatedTileCount > FrameSkipThreshold
                    ? Math.Min(def.FramesPerSecond, ReducedFps)
                    : def.FramesPerSecond;

                _currentEffectiveFps = effectiveFps;
                double frameDuration = 1.0 / effectiveFps;

                state.FrameTimer += deltaTime;
                if (state.FrameTimer >= frameDuration)
                {
                    state.FrameTimer -= frameDuration;
                    state.CurrentFrame++;

                    if (state.CurrentFrame >= def.FrameCount)
                    {
                        if (def.IsLooping)
                            state.CurrentFrame = 0;
                        else
                        {
                            state.CurrentFrame = def.FrameCount - 1;
                            state.IsCompleted = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current frame rectangle (UV coordinates) for a tile at a grid position.
        /// Returns null if no animated tile at that position.
        /// Uses spritesheet UV calculation – no per-frame image loading.
        /// </summary>
        public (int frameX, int frameY, int frameW, int frameH)? GetCurrentFrameRect(int gridX, int gridY)
        {
            long key = PackKey(gridX, gridY);
            if (!_activeAnimations.TryGetValue(key, out var state)) return null;
            if (!state.IsVisible) return null;
            if (!_definitions.TryGetValue(state.DefinitionId, out var def)) return null;

            int frame = state.CurrentFrame;
            int fx, fy;

            if (def.IsHorizontalLayout)
            {
                fx = frame * def.FrameWidth;
                fy = 0;
            }
            else
            {
                fx = 0;
                fy = frame * def.FrameHeight;
            }

            return (fx, fy, def.FrameWidth, def.FrameHeight);
        }

        /// <summary>
        /// Gets all active animation states for rendering. No allocation.
        /// </summary>
        public IReadOnlyDictionary<long, TileAnimState> GetAllAnimations() => _activeAnimations;

        /// <summary>
        /// Gets current performance stats.
        /// </summary>
        public (int totalAnimated, int visibleAnimated, double effectiveFps) GetPerformanceStats()
        {
            return (_activeAnimations.Count, _visibleAnimatedTileCount, _currentEffectiveFps);
        }

        /// <summary>
        /// Pre-built animated tile definitions for common D&D effects.
        /// </summary>
        public static IReadOnlyList<AnimatedTileDefinition> GetBuiltInDefinitions()
        {
            return new[]
            {
                new AnimatedTileDefinition { Id = "water_flow", DisplayName = "Flowing Water", FrameCount = 4, FramesPerSecond = 6, Category = AnimatedTileCategory.Water },
                new AnimatedTileDefinition { Id = "water_ripple", DisplayName = "Water Ripple", FrameCount = 4, FramesPerSecond = 4, Category = AnimatedTileCategory.Water },
                new AnimatedTileDefinition { Id = "fire_small", DisplayName = "Small Fire", FrameCount = 6, FramesPerSecond = 10, Category = AnimatedTileCategory.Fire },
                new AnimatedTileDefinition { Id = "fire_large", DisplayName = "Large Fire", FrameCount = 6, FramesPerSecond = 12, Category = AnimatedTileCategory.Fire },
                new AnimatedTileDefinition { Id = "torch_flicker", DisplayName = "Torch Flicker", FrameCount = 4, FramesPerSecond = 8, Category = AnimatedTileCategory.Fire, RandomStartFrame = true },
                new AnimatedTileDefinition { Id = "magic_circle", DisplayName = "Magic Circle", FrameCount = 8, FramesPerSecond = 4, Category = AnimatedTileCategory.Magic },
                new AnimatedTileDefinition { Id = "lava_bubble", DisplayName = "Lava Bubbling", FrameCount = 6, FramesPerSecond = 5, Category = AnimatedTileCategory.Hazard },
                new AnimatedTileDefinition { Id = "blood_splatter", DisplayName = "Blood Splatter", FrameCount = 3, FramesPerSecond = 8, IsLooping = false, Category = AnimatedTileCategory.Hazard },
                new AnimatedTileDefinition { Id = "leaves_falling", DisplayName = "Falling Leaves", FrameCount = 6, FramesPerSecond = 4, Category = AnimatedTileCategory.Nature },
                new AnimatedTileDefinition { Id = "banner_wave", DisplayName = "Waving Banner", FrameCount = 4, FramesPerSecond = 6, Category = AnimatedTileCategory.Atmospheric },
            };
        }

        /// <summary>
        /// Clears all animated tiles.
        /// </summary>
        public void ClearAll()
        {
            _activeAnimations.Clear();
        }

        // Pack two ints into one long for Dictionary key (avoids tuple allocation)
        private static long PackKey(int x, int y) => ((long)x << 32) | (uint)y;
    }
}
