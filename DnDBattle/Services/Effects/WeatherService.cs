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
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.Effects;

namespace DnDBattle.Services.Effects
{
    /// <summary>
    /// Manages dynamic weather effects and day/night cycle with optimized particle pooling.
    /// All particles are pre-allocated and recycled to minimize GC pressure.
    /// </summary>
    public sealed class WeatherService
    {
        // ── Particle pool ──
        private readonly WeatherParticle[] _particlePool;
        private int _activeCount;

        // ── State ──
        public WeatherType CurrentWeather { get; private set; } = WeatherType.None;
        public TimeOfDay CurrentTimeOfDay { get; private set; } = TimeOfDay.Day;
        public Season CurrentSeason { get; private set; } = Season.Summer;
        public double WindAngleDegrees { get; set; } = 0;
        public double WindStrength { get; set; } = 1.0;
        public double WeatherIntensity { get; set; } = 0.5;

        // ── Day/night lighting cache ──
        private double _cachedLightingOpacity = 0.0;
        private TimeOfDay _lastCachedTimeOfDay = TimeOfDay.Day;
        private DateTime _lastLightingUpdate = DateTime.MinValue;
        private static readonly TimeSpan LightingCacheInterval = TimeSpan.FromSeconds(2);

        // ── Viewport bounds (only update particles within this) ──
        public Rect ViewportBounds { get; set; } = new Rect(0, 0, 1920, 1080);

        private readonly Random _rng = new();

        /// <summary>
        /// Creates the weather service with a fixed particle pool size.
        /// Pool size is allocated once and never resized to avoid GC pressure.
        /// </summary>
        public WeatherService(int maxParticles = 500)
        {
            _particlePool = new WeatherParticle[maxParticles];
            for (int i = 0; i < maxParticles; i++)
                _particlePool[i] = new WeatherParticle();
            _activeCount = 0;
        }

        /// <summary>
        /// Sets the current weather type and adjusts particle configuration.
        /// </summary>
        public void SetWeather(WeatherType weather, double intensity = 0.5)
        {
            if (!Options.EnableWeatherEffects) return;
            CurrentWeather = weather;
            WeatherIntensity = Math.Clamp(intensity, 0.0, 1.0);
            ResetParticles();
        }

        /// <summary>
        /// Sets the current time of day and invalidates the lighting cache.
        /// </summary>
        public void SetTimeOfDay(TimeOfDay time)
        {
            if (!Options.EnableDayNightCycle) return;
            CurrentTimeOfDay = time;
            _lastLightingUpdate = DateTime.MinValue; // force re-cache
        }

        /// <summary>
        /// Gets the current lighting overlay opacity (cached, updates every 2 seconds).
        /// Returns 0.0 (full daylight) to 0.55 (night darkness), up to 0.7 during storms.
        /// </summary>
        public double GetLightingOverlayOpacity()
        {
            if (!Options.EnableDayNightCycle) return 0.0;

            var now = DateTime.UtcNow;
            if (CurrentTimeOfDay == _lastCachedTimeOfDay &&
                now - _lastLightingUpdate < LightingCacheInterval)
                return _cachedLightingOpacity;

            _cachedLightingOpacity = CurrentTimeOfDay switch
            {
                TimeOfDay.Dawn => 0.15,
                TimeOfDay.Day => 0.0,
                TimeOfDay.Dusk => 0.25,
                TimeOfDay.Night => 0.55,
                _ => 0.0
            };

            // Storm darkens the sky further
            if (CurrentWeather == WeatherType.Storm)
                _cachedLightingOpacity = Math.Min(_cachedLightingOpacity + 0.15, 0.7);

            _lastCachedTimeOfDay = CurrentTimeOfDay;
            _lastLightingUpdate = now;
            return _cachedLightingOpacity;
        }

        /// <summary>
        /// Frame update: moves active particles and recycles dead ones.
        /// Uses viewport culling – particles outside the viewport are immediately recycled.
        /// deltaTime is in seconds for frame-rate independence.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!Options.EnableWeatherEffects || CurrentWeather == WeatherType.None)
                return;

            double windRadians = WindAngleDegrees * Math.PI / 180.0;
            double windX = Math.Cos(windRadians) * WindStrength * 60.0; // px/sec
            double windY = Math.Sin(windRadians) * WindStrength * 60.0;

            int targetCount = GetTargetParticleCount();

            // Spawn new particles up to target
            for (int i = _activeCount; i < targetCount && i < _particlePool.Length; i++)
            {
                SpawnParticle(_particlePool[i]);
                _activeCount = i + 1;
            }

            // Update active particles
            for (int i = 0; i < _activeCount; i++)
            {
                var p = _particlePool[i];
                if (!p.IsActive) continue;

                p.X += (p.VelocityX + windX) * deltaTime;
                p.Y += (p.VelocityY + windY) * deltaTime;
                p.Life -= deltaTime;
                p.Opacity = Math.Clamp(p.Life / p.MaxLife, 0.0, 1.0);

                // Viewport culling: recycle if out of bounds or expired
                if (p.Life <= 0 || !ViewportBounds.Contains(p.X, p.Y))
                {
                    p.IsActive = false;
                    // Swap with last active to keep pool compact
                    if (i < _activeCount - 1)
                    {
                        (_particlePool[i], _particlePool[_activeCount - 1]) =
                            (_particlePool[_activeCount - 1], _particlePool[i]);
                        i--; // re-check swapped particle
                    }
                    _activeCount--;
                }
            }
        }

        /// <summary>
        /// Gets a read-only span of currently active particles for rendering.
        /// No allocation – returns a slice of the pre-allocated pool.
        /// </summary>
        public ReadOnlySpan<WeatherParticle> GetActiveParticles()
        {
            return new ReadOnlySpan<WeatherParticle>(_particlePool, 0, _activeCount);
        }

        /// <summary>
        /// Returns the vision range modifier for current weather conditions.
        /// 1.0 = no change, 0.5 = halved vision, etc.
        /// </summary>
        public double GetVisionRangeModifier()
        {
            return CurrentWeather switch
            {
                WeatherType.Fog => 0.3,
                WeatherType.Storm => 0.5,
                WeatherType.Rain => 0.8,
                WeatherType.Snow => 0.6,
                WeatherType.Sandstorm => 0.4,
                _ => 1.0
            };
        }

        private int GetTargetParticleCount()
        {
            int maxAllowed = Options.WeatherMaxParticles;
            double fraction = CurrentWeather switch
            {
                WeatherType.Rain => 1.0,
                WeatherType.Snow => 0.6,
                WeatherType.Fog => 0.3,
                WeatherType.Storm => 1.0,
                WeatherType.Sandstorm => 0.8,
                _ => 0.0
            };
            return (int)(maxAllowed * fraction * WeatherIntensity);
        }

        private void SpawnParticle(WeatherParticle p)
        {
            p.IsActive = true;
            p.X = ViewportBounds.Left + _rng.NextDouble() * ViewportBounds.Width;
            p.Y = ViewportBounds.Top;
            p.MaxLife = 2.0 + _rng.NextDouble() * 3.0;
            p.Life = p.MaxLife;
            p.Opacity = 1.0;
            p.Size = CurrentWeather switch
            {
                WeatherType.Rain => 1.5 + _rng.NextDouble() * 1.5,
                WeatherType.Snow => 3.0 + _rng.NextDouble() * 4.0,
                WeatherType.Fog => 20.0 + _rng.NextDouble() * 30.0,
                WeatherType.Storm => 2.0 + _rng.NextDouble() * 2.0,
                WeatherType.Sandstorm => 2.0 + _rng.NextDouble() * 3.0,
                _ => 2.0
            };

            // Base velocities (px/sec)
            p.VelocityX = CurrentWeather switch
            {
                WeatherType.Rain => (_rng.NextDouble() - 0.5) * 20.0,
                WeatherType.Snow => (_rng.NextDouble() - 0.5) * 30.0,
                WeatherType.Fog => (_rng.NextDouble() - 0.5) * 5.0,
                WeatherType.Storm => (_rng.NextDouble() - 0.5) * 60.0,
                WeatherType.Sandstorm => 40.0 + _rng.NextDouble() * 40.0,
                _ => 0
            };
            p.VelocityY = CurrentWeather switch
            {
                WeatherType.Rain => 200.0 + _rng.NextDouble() * 100.0,
                WeatherType.Snow => 40.0 + _rng.NextDouble() * 30.0,
                WeatherType.Fog => (_rng.NextDouble() - 0.5) * 3.0,
                WeatherType.Storm => 250.0 + _rng.NextDouble() * 150.0,
                WeatherType.Sandstorm => (_rng.NextDouble() - 0.5) * 20.0,
                _ => 100.0
            };
        }

        private void ResetParticles()
        {
            for (int i = 0; i < _activeCount; i++)
                _particlePool[i].IsActive = false;
            _activeCount = 0;
        }
    }

    /// <summary>
    /// Pre-allocated particle struct for weather rendering.
    /// Uses class (not struct) so it can be pooled by reference without copying.
    /// </summary>
    public sealed class WeatherParticle
    {
        public double X;
        public double Y;
        public double VelocityX;
        public double VelocityY;
        public double Life;
        public double MaxLife;
        public double Opacity;
        public double Size;
        public bool IsActive;
    }
}
