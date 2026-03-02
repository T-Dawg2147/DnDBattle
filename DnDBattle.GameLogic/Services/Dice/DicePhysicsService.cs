using System;
using System.Collections.Generic;
using System.Linq;
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
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Dice
{
    /// <summary>
    /// Simplified dice physics simulation for visual dice rolling.
    /// Pre-determines the result, then animates dice to land on that face.
    /// This avoids expensive real-time physics while looking realistic.
    /// </summary>
    public sealed class DicePhysicsService
    {
        private readonly List<DiceState> _activeDice = new();
        private readonly Random _rng = new();
        private bool _isSimulating;
        private const int MaxDicePerRoll = 20;

        /// <summary>Whether a roll animation is currently in progress.</summary>
        public bool IsRolling => _isSimulating;

        /// <summary>Event raised when all dice have settled.</summary>
        public event Action<IReadOnlyList<DiceState>>? OnDiceSettled;

        /// <summary>
        /// Starts a dice roll. Pre-determines results for fairness,
        /// then simulates physics for visual effect.
        /// </summary>
        public void Roll(DiceType type, int count = 1)
        {
            if (!Options.EnableDiceRoller3D) return;

            _activeDice.Clear();
            int sides = (int)type;

            for (int i = 0; i < Math.Min(count, MaxDicePerRoll); i++)
            {
                int result = _rng.Next(1, sides + 1);
                _activeDice.Add(new DiceState
                {
                    Type = type,
                    FinalValue = result,
                    X = (_rng.NextDouble() - 0.5) * 200,
                    Y = 300 + i * 30,
                    Z = (_rng.NextDouble() - 0.5) * 200,
                    VelocityX = (_rng.NextDouble() - 0.5) * 100,
                    VelocityY = -200 - _rng.NextDouble() * 100,
                    VelocityZ = (_rng.NextDouble() - 0.5) * 100,
                    RotationX = _rng.NextDouble() * 360,
                    RotationY = _rng.NextDouble() * 360,
                    RotationZ = _rng.NextDouble() * 360,
                    AngularVelX = (_rng.NextDouble() - 0.5) * 720,
                    AngularVelY = (_rng.NextDouble() - 0.5) * 720,
                    AngularVelZ = (_rng.NextDouble() - 0.5) * 720,
                    IsSettled = false
                });
            }

            _isSimulating = true;
        }

        /// <summary>
        /// Updates physics simulation. Call every frame with delta time in seconds.
        /// Uses simplified Euler integration – sufficient for visual effect.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!_isSimulating) return;

            bool allSettled = true;
            const double gravity = 500.0; // px/sec²
            const double groundY = 0;
            const double bounceFactor = 0.4;
            const double friction = 0.92;
            const double settleThreshold = 5.0;

            foreach (var d in _activeDice)
            {
                if (d.IsSettled) continue;

                // Gravity
                d.VelocityY += gravity * deltaTime;

                // Position update
                d.X += d.VelocityX * deltaTime;
                d.Y += d.VelocityY * deltaTime;
                d.Z += d.VelocityZ * deltaTime;

                // Rotation update
                d.RotationX += d.AngularVelX * deltaTime;
                d.RotationY += d.AngularVelY * deltaTime;
                d.RotationZ += d.AngularVelZ * deltaTime;

                // Ground bounce
                if (d.Y >= groundY)
                {
                    d.Y = groundY;
                    d.VelocityY = -d.VelocityY * bounceFactor;
                    d.VelocityX *= friction;
                    d.VelocityZ *= friction;
                    d.AngularVelX *= friction;
                    d.AngularVelY *= friction;
                    d.AngularVelZ *= friction;
                }

                // Wall bounces (keep in bounds)
                if (Math.Abs(d.X) > 300)
                {
                    d.VelocityX = -d.VelocityX * bounceFactor;
                    d.X = Math.Sign(d.X) * 300;
                }
                if (Math.Abs(d.Z) > 300)
                {
                    d.VelocityZ = -d.VelocityZ * bounceFactor;
                    d.Z = Math.Sign(d.Z) * 300;
                }

                // Check if settled
                double speed = Math.Sqrt(d.VelocityX * d.VelocityX + d.VelocityY * d.VelocityY + d.VelocityZ * d.VelocityZ);
                if (speed < settleThreshold && Math.Abs(d.Y - groundY) < 1)
                {
                    d.IsSettled = true;
                    d.Y = groundY;
                    d.VelocityX = 0;
                    d.VelocityY = 0;
                    d.VelocityZ = 0;
                    d.AngularVelX = 0;
                    d.AngularVelY = 0;
                    d.AngularVelZ = 0;
                }
                else
                {
                    allSettled = false;
                }
            }

            if (allSettled && _activeDice.Count > 0)
            {
                _isSimulating = false;
                OnDiceSettled?.Invoke(_activeDice.AsReadOnly());
            }
        }

        /// <summary>
        /// Gets the sum of all dice results.
        /// </summary>
        public int GetTotal() => _activeDice.Sum(d => d.FinalValue);

        /// <summary>
        /// Gets all active dice states for rendering.
        /// </summary>
        public IReadOnlyList<DiceState> GetDice() => _activeDice.AsReadOnly();

        /// <summary>
        /// Immediately settles all dice (skip animation).
        /// </summary>
        public void SkipAnimation()
        {
            foreach (var d in _activeDice)
            {
                d.IsSettled = true;
                d.Y = 0;
            }
            _isSimulating = false;
            OnDiceSettled?.Invoke(_activeDice.AsReadOnly());
        }
    }

    /// <summary>
    /// State of a single die during physics simulation.
    /// </summary>
    public class DiceState
    {
        public DiceType Type { get; set; }
        public int FinalValue { get; set; }

        // Position
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        // Linear velocity (px/sec)
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double VelocityZ { get; set; }

        // Rotation (degrees)
        public double RotationX { get; set; }
        public double RotationY { get; set; }
        public double RotationZ { get; set; }

        // Angular velocity (degrees/sec)
        public double AngularVelX { get; set; }
        public double AngularVelY { get; set; }
        public double AngularVelZ { get; set; }

        // Settled state
        public bool IsSettled { get; set; }

        /// <summary>Number of sides for this die type.</summary>
        public int Sides => (int)Type;
    }
}
