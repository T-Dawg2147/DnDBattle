using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Services.TileService
{
    public class DelayedSpawnQueue
    {
        private class DelayedSpawn
        {
            public SpawnMetadata Spawn { get; set; }
            public int TriggeredOnRound { get; set; }
            public int ActivateOnRound { get; set; }
            public bool IsActive { get; set; } = true;
        }

        private readonly List<DelayedSpawn> _queue = new List<DelayedSpawn>();

        public event Action<SpawnMetadata> SpawnReady;
        public event Action<string> LogMessage;

        public void EnqueueSpawn(SpawnMetadata spawn, int currentRound)
        {
            if (spawn.SpawnDelay <= 0)
            {
                SpawnReady?.Invoke(spawn);
                return;
            }

            var delayed = new DelayedSpawn()
            {
                Spawn = spawn,
                TriggeredOnRound = currentRound,
                ActivateOnRound = currentRound + spawn.SpawnDelay
            };

            _queue.Add(delayed);

            LogMessage?.Invoke($"⏳ {spawn.Name} will spawn in {spawn.SpawnDelay} rounds (Round {delayed.ActivateOnRound})");
        }

        public List<SpawnMetadata> ProcessRound(int currentRound)
        {
            var readySpawns = new List<SpawnMetadata>();

            foreach (var delayed in _queue.Where(d => d.IsActive && d.ActivateOnRound == currentRound).ToList())
            {
                LogMessage?.Invoke($"⏰ Delayed spawn ready: {delayed.Spawn.Name}");
                readySpawns.Add(delayed.Spawn);
                delayed.IsActive = false;
                SpawnReady?.Invoke(delayed.Spawn);
            }

            _queue.RemoveAll(d => !d.IsActive);

            return readySpawns;
        }

        public List<(SpawnMetadata Spawn, int RoundsRemaining)> GetPendingSpawns(int currentRound)
        {
            return _queue
                .Where(d => d.IsActive)
                .Select(d => (d.Spawn, d.ActivateOnRound - currentRound))
                .ToList();
        }

        public bool CancelSpawn(SpawnMetadata spawn)
        {
            var delayed = _queue.FirstOrDefault(d => d.Spawn == spawn && d.IsActive);
            if (delayed != null)
            {
                delayed.IsActive = false;
                LogMessage?.Invoke($"❌ Cancelled delayed spawn: {spawn.Name}");
                return true;
            }
            return false;
        }

        public void Clear() =>
            _queue.Clear();

        public int PendingCount => _queue.Count(d => d.IsActive);
    }
}
