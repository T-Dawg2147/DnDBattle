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
using DnDBattle.Services.Dice;

namespace DnDBattle.Services.Dice
{
    /// <summary>
    /// Tracks dice roll history and provides statistics.
    /// Uses a circular buffer for bounded memory and running averages for O(1) queries.
    /// </summary>
    public sealed class DiceStatisticsService
    {
        private readonly DiceStatRollRecord[] _history;
        private int _head;
        private int _count;

        // Running averages per dice type (avoid re-scanning history)
        private readonly Dictionary<int, RunningAverage> _averages = new();

        // Critical hit/fail counters
        private int _criticalHits;
        private int _criticalFails;
        private int _totalD20Rolls;

        public DiceStatisticsService(int maxHistory = 0)
        {
            int size = maxHistory > 0 ? maxHistory : Options.DiceHistoryMaxSize;
            _history = new DiceStatRollRecord[size];
        }

        /// <summary>
        /// Records a dice roll result.
        /// </summary>
        public void RecordRoll(int result, int sides, string? label = null)
        {
            if (!Options.EnableDiceStatistics) return;

            var record = new DiceStatRollRecord
            {
                Result = result,
                Sides = sides,
                Timestamp = DateTime.UtcNow,
                Label = label ?? string.Empty
            };

            // Circular buffer insert
            _history[_head] = record;
            _head = (_head + 1) % _history.Length;
            if (_count < _history.Length) _count++;

            // Update running average
            if (!_averages.TryGetValue(sides, out var avg))
            {
                avg = new RunningAverage();
                _averages[sides] = avg;
            }
            avg.Add(result);

            // Track d20 crits
            if (sides == 20)
            {
                _totalD20Rolls++;
                if (result == 20) _criticalHits++;
                if (result == 1) _criticalFails++;
            }
        }

        /// <summary>
        /// Gets the average roll for a dice type. O(1) via running average.
        /// </summary>
        public double GetAverageRoll(int sides)
        {
            return _averages.TryGetValue(sides, out var avg) ? avg.Average : 0;
        }

        /// <summary>
        /// Gets the total number of critical hits (natural 20 on d20).
        /// </summary>
        public int CriticalHitCount => _criticalHits;

        /// <summary>
        /// Gets the total number of critical fails (natural 1 on d20).
        /// </summary>
        public int CriticalFailCount => _criticalFails;

        /// <summary>
        /// Gets a "luck score" based on d20 rolls.
        /// 1.0 = average, >1.0 = lucky, less than 1.0 = unlucky.
        /// </summary>
        public double LuckScore
        {
            get
            {
                if (_totalD20Rolls == 0) return 1.0;
                double avg = GetAverageRoll(20);
                return avg / 10.5; // expected d20 average = 10.5
            }
        }

        /// <summary>
        /// Gets the total number of rolls recorded.
        /// </summary>
        public int TotalRolls => _count;

        /// <summary>
        /// Gets the most recent N rolls (newest first).
        /// </summary>
        public IReadOnlyList<DiceStatRollRecord> GetRecentRolls(int count = 20)
        {
            var result = new List<DiceStatRollRecord>(Math.Min(count, _count));
            for (int i = 0; i < Math.Min(count, _count); i++)
            {
                int idx = (_head - 1 - i + _history.Length) % _history.Length;
                if (_history[idx] != null)
                    result.Add(_history[idx]);
            }
            return result;
        }

        /// <summary>
        /// Gets the distribution of rolls for a dice type (how many of each value).
        /// </summary>
        public Dictionary<int, int> GetDistribution(int sides)
        {
            var dist = new Dictionary<int, int>();
            for (int i = 1; i <= sides; i++)
                dist[i] = 0;

            for (int i = 0; i < _count; i++)
            {
                var record = _history[i];
                if (record != null && record.Sides == sides)
                {
                    if (dist.ContainsKey(record.Result))
                        dist[record.Result]++;
                }
            }
            return dist;
        }

        /// <summary>
        /// Resets all statistics.
        /// </summary>
        public void Reset()
        {
            Array.Clear(_history, 0, _history.Length);
            _head = 0;
            _count = 0;
            _averages.Clear();
            _criticalHits = 0;
            _criticalFails = 0;
            _totalD20Rolls = 0;
        }

        /// <summary>
        /// Internal running average tracker to avoid re-scanning history.
        /// </summary>
        private class RunningAverage
        {
            private double _sum;
            private int _count;

            public double Average => _count > 0 ? _sum / _count : 0;

            public void Add(double value)
            {
                _sum += value;
                _count++;
            }
        }
    }

    /// <summary>
    /// Record of a single dice roll for statistics tracking.
    /// </summary>
    public class DiceStatRollRecord
    {
        public int Result { get; set; }
        public int Sides { get; set; }
        public DateTime Timestamp { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
