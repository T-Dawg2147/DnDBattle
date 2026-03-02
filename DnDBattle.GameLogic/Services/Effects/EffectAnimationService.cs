using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Encounters;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Effects
{
    /// <summary>
    /// Manages effect animations (pulsing, particles, rotation) for area effects
    /// </summary>
    public class EffectAnimationService
    {
        private const double MaxDeltaTimeSeconds = 0.016; // ~60 FPS cap to prevent jumps
        private const double OpacityDecayRate = 0.5;
        private const double PulseSpeed = 2.0; // radians per second
        private const double RotateSpeed = 45.0; // degrees per second

        private readonly AreaEffectService _areaEffectService;
        private readonly Random _random = new Random();
        private DateTime _lastUpdate = DateTime.Now;

        // Particle storage keyed by effect ID
        private readonly Dictionary<Guid, List<Particle>> _particleSets = new Dictionary<Guid, List<Particle>>();

        public EffectAnimationService(AreaEffectService areaEffectService)
        {
            _areaEffectService = areaEffectService;
        }

        /// <summary>
        /// Updates all animated effects. Call each frame.
        /// Returns true if any effect was updated (requires redraw).
        /// </summary>
        public bool Update()
        {
            if (!Options.EnableEffectAnimations) return false;

            var now = DateTime.Now;
            double deltaTime = (now - _lastUpdate).TotalSeconds;
            _lastUpdate = now;

            // Clamp delta to avoid huge jumps if window was hidden
            if (deltaTime > 0.5) deltaTime = MaxDeltaTimeSeconds;

            bool anyUpdated = false;

            foreach (var effect in _areaEffectService.ActiveEffects)
            {
                if (effect.AnimationType == EffectAnimationType.None) continue;

                switch (effect.AnimationType)
                {
                    case EffectAnimationType.Pulse:
                        effect.AnimationPhase += deltaTime * PulseSpeed;
                        if (effect.AnimationPhase > Math.PI * 2)
                            effect.AnimationPhase -= Math.PI * 2;
                        anyUpdated = true;
                        break;

                    case EffectAnimationType.Rotate:
                        effect.AnimationPhase += deltaTime * RotateSpeed;
                        if (effect.AnimationPhase > 360)
                            effect.AnimationPhase -= 360;
                        anyUpdated = true;
                        break;

                    case EffectAnimationType.Particle:
                        UpdateParticles(effect, deltaTime);
                        anyUpdated = true;
                        break;
                }
            }

            // Clean up particles for removed effects
            var activeIds = new HashSet<Guid>(_areaEffectService.ActiveEffects.Select(e => e.Id));
            var staleIds = _particleSets.Keys.Where(id => !activeIds.Contains(id)).ToList();
            foreach (var id in staleIds)
                _particleSets.Remove(id);

            return anyUpdated;
        }

        /// <summary>
        /// Gets the pulse value (0-1) for an effect with Pulse animation
        /// </summary>
        public double GetPulseValue(AreaEffect effect)
        {
            return (Math.Sin(effect.AnimationPhase) + 1.0) / 2.0;
        }

        /// <summary>
        /// Gets particles for a given effect
        /// </summary>
        public IReadOnlyList<Particle> GetParticles(Guid effectId)
        {
            return _particleSets.TryGetValue(effectId, out var list) ? list : Array.Empty<Particle>();
        }

        private void UpdateParticles(AreaEffect effect, double deltaTime)
        {
            if (!_particleSets.TryGetValue(effect.Id, out var particles))
            {
                particles = new List<Particle>();
                _particleSets[effect.Id] = particles;
            }

            int maxParticles = Options.MaxParticlesPerEffect;

            // Update existing
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.Position = new System.Windows.Point(
                    p.Position.X + p.Velocity.X * deltaTime,
                    p.Position.Y + p.Velocity.Y * deltaTime);
                p.Lifetime -= deltaTime;
                p.Opacity = Math.Max(0, p.Opacity - deltaTime * OpacityDecayRate);

                if (p.Lifetime <= 0 || p.Opacity <= 0)
                    particles.RemoveAt(i);
            }

            // Spawn new ones
            if (particles.Count < maxParticles)
            {
                double cx = effect.Origin.X;
                double cy = effect.Origin.Y;
                double radius = effect.SizeInSquares;

                // Random position inside effect area
                double angle = _random.NextDouble() * Math.PI * 2;
                double dist = _random.NextDouble() * radius;
                double px = cx + Math.Cos(angle) * dist;
                double py = cy + Math.Sin(angle) * dist;

                particles.Add(new Particle
                {
                    Position = new System.Windows.Point(px, py),
                    Velocity = new System.Windows.Vector(
                        (_random.NextDouble() - 0.5) * 0.5,
                        -_random.NextDouble() * 0.8),
                    Color = effect.Color,
                    Size = 2 + _random.NextDouble() * 3,
                    Lifetime = 1.5 + _random.NextDouble(),
                    Opacity = 0.8 + _random.NextDouble() * 0.2
                });
            }
        }

        /// <summary>
        /// Resets all particle data
        /// </summary>
        public void Clear()
        {
            _particleSets.Clear();
        }
    }
}
