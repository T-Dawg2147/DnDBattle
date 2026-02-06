using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Services.TileService
{
    public class SpawnPointService
    {
        public event Action<string> LogMessage;
        public event Action<SpawnMetadata, List<Token>> CreaturesSpawned;

        private readonly Random _rng = new Random();
        private readonly DelayedSpawnQueue _delayedQueue = new DelayedSpawnQueue();

        public SpawnPointService()
        {
            // Wire up delayed queue
            _delayedQueue.LogMessage += (msg) => LogMessage?.Invoke(msg);
            _delayedQueue.SpawnReady += (spawn) =>
            {
                LogMessage?.Invoke($"⏰ Delayed spawn is now ready: {spawn.Name}");
            };
        }

        public DelayedSpawnQueue DelayedQueue => _delayedQueue;

        public List<Token> ActivateSpawnPoint(SpawnMetadata spawn, ObservableCollection<Token> creatureBank, TileMap tileMap, double cellSize)
        {
            if (spawn.HasSpawned && !spawn.IsReusable)
            {
                LogMessage?.Invoke($"⚠️ Spawn point '{spawn.Name}' has already been used.");
                return new List<Token>();
            }

            var template = creatureBank.FirstOrDefault(c =>
            c.Name.Equals(spawn.CreatureName, StringComparison.OrdinalIgnoreCase));

            if (template == null)
            {
                LogMessage?.Invoke($"❌ Creature '{spawn.CreatureName}' not found in Creature Bank!");
                return new List<Token>();
            }

            LogMessage?.Invoke($"👹 Activating spawn point: {spawn.Name}");
            LogMessage?.Invoke($"📍 Spawning {spawn.SpawnCount} × {spawn.CreatureName}");

            var spawnTile = tileMap.PlacedTiles.FirstOrDefault(t => t.Metadata.Contains(spawn));
            if (spawnTile == null)
            {
                LogMessage?.Invoke($"❌ Could not find spawn point tile!");
                return new List<Token>();
            }

            int spawnX = spawnTile.GridX;
            int spawnY = spawnTile.GridY;

            var spawnPositions = GenerateSpawnPositions(spawnX, spawnY, spawn.SpawnCount, spawn.SpawnRadius, tileMap);

            var spawnedTokens = new List<Token>();
            for (int i = 0; i < spawn.SpawnCount && i < spawnPositions.Count; i++)
            {
                var newToken = CloneToken(template);
                newToken.Name = $"{template.Name} {i + 1}";
                newToken.GridX = spawnPositions[i].X;
                newToken.GridY = spawnPositions[i].Y;
                newToken.Id = Guid.NewGuid();

                spawnedTokens.Add(newToken);
                LogMessage?.Invoke($"  • {newToken.Name} at ({newToken.GridX}, {newToken.GridY})");
            }

            spawn.HasSpawned = true;
            spawn.IsTriggered = true;

            CreaturesSpawned?.Invoke(spawn, spawnedTokens);

            return spawnedTokens;
        }

        /// <summary>
        /// Check spawn point triggers with delayed spawn support
        /// </summary>
        public List<SpawnMetadata> CheckSpawnTriggers(TileMap tileMap, System.Collections.ObjectModel.ObservableCollection<Token> tokens, int currentRound, bool combatJustStarted)
        {
            var triggeredSpawns = new List<SpawnMetadata>();

            // Process delayed spawns
            var readySpawns = _delayedQueue.ProcessRound(currentRound);
            triggeredSpawns.AddRange(readySpawns);

            // Check for new triggers
            foreach (var tile in tileMap.PlacedTiles)
            {
                var spawns = tile.GetMetadata(TileMetadataType.Spawn).OfType<SpawnMetadata>().ToList();

                foreach (var spawn in spawns)
                {
                    if (!spawn.IsEnabled || (spawn.HasSpawned && !spawn.IsReusable))
                        continue;

                    bool shouldTrigger = false;

                    switch (spawn.TriggerCondition)
                    {
                        case SpawnTrigger.CombatStart:
                            shouldTrigger = combatJustStarted;
                            break;

                        case SpawnTrigger.RoundNumber:
                            shouldTrigger = currentRound == spawn.SpawnOnRound;
                            break;

                        case SpawnTrigger.Proximity:
                            shouldTrigger = CheckProximityTrigger(tile, tokens, spawn.TriggerDistance);
                            break;

                        case SpawnTrigger.Manual:
                            // Manual spawns don't auto-trigger
                            break;
                    }

                    if (shouldTrigger)
                    {
                        // Check for delay
                        if (spawn.SpawnDelay > 0)
                        {
                            _delayedQueue.EnqueueSpawn(spawn, currentRound);
                        }
                        else
                        {
                            triggeredSpawns.Add(spawn);
                        }
                    }
                }
            }

            return triggeredSpawns;
        }

        private bool CheckProximityTrigger(Tile spawnTile, ObservableCollection<Token> tokens, int triggerDistance)
        {
            foreach (var token in tokens.Where(t => t.IsPlayer))
            {
                int distance = Math.Abs(token.GridX - spawnTile.GridX) + Math.Abs(token.GridY - spawnTile.GridY);
                if (distance <= triggerDistance)
                    return true;
            }
            return false;
        }

        private List<(int X, int Y)> GenerateSpawnPositions(int centerX, int centerY, int count, int radius, TileMap tileMap)
        {
            var positions = new List<(int X, int Y)>();

            if (radius == 0)
            {
                positions.Add((centerX, centerY));
                return positions;
            }

            var canidates = new List<(int X, int Y)>();
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x < 0 || x >= tileMap.Width || y < 0 || y >= tileMap.Height)
                        continue;

                    int dist = Math.Abs(x - centerX) + Math.Abs(y - centerY);
                    if (dist <= radius)
                    {
                        canidates.Add((x, y));
                    }
                }
            }
            canidates = canidates.OrderBy(x => _rng.Next()).ToList();
            positions.AddRange(canidates.Take(count));

            return positions;
        }

        private Token CloneToken(Token template)
        {
            return new Token
            {
                Name = template.Name,
                Size = template.Size,
                Type = template.Type,
                Alignment = template.Alignment,
                ChallengeRating = template.ChallengeRating,
                ArmorClass = template.ArmorClass,
                MaxHP = template.MaxHP,
                HP = template.MaxHP,
                HitDice = template.HitDice,
                InitiativeModifier = template.InitiativeModifier,
                Speed = template.Speed,
                Str = template.Str,
                Dex = template.Dex,
                Con = template.Con,
                Int = template.Int,
                Wis = template.Wis,
                Cha = template.Cha,
                StrMod = template.StrMod,
                DexMod = template.DexMod,
                ConMod = template.ConMod,
                IntMod = template.IntMod,
                WisMod = template.WisMod,
                ChaMod = template.ChaMod,
                PassivePerception = template.PassivePerception,
                SizeInSquares = template.SizeInSquares,
                IsPlayer = false,
                IconPath = template.IconPath
            };
        }
    }
}
