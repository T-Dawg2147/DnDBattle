using DnDBattle.Models;
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
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.Vision;
using DnDBattle.Services.UI;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.UI
{
    public class CombatStatisticsService
    {
        private Dictionary<Guid, TokenCombatStats> _stats = new Dictionary<Guid, TokenCombatStats>();
        private int _totalRounds = 0;
        private DateTime _combatStartTime;

        public event System.Action StatsUpdated;

        public void StartCombat()
        {
            _stats.Clear();
            _totalRounds = 1;
            _combatStartTime = DateTime.Now;
            StatsUpdated?.Invoke();
        }

        public void EndCombat()
        {
            StatsUpdated?.Invoke();
        }

        public void NewRound()
        {
            _totalRounds++;
            StatsUpdated?.Invoke();
        }

        public TokenCombatStats GetStats(Token token)
        {
            if (!_stats.ContainsKey(token.Id))
                _stats[token.Id] = new TokenCombatStats { TokenId = token.Id, TokenName = token.Name };

            return _stats[token.Id];
        }

        public void RecordAttack(Token attacker, Token target, bool hit, bool critical, bool criticalMiss)
        {
            var stats = GetStats(attacker);
            stats.TotalAttacks++;

            if (hit)
            {
                stats.TotalHits++;
                if (critical) stats.CriticalHits++;
            }
            else
            {
                stats.TotalMisses++;
                if (criticalMiss) stats.CriticalMisses++;
            }

            StatsUpdated?.Invoke();
        }

        public void RecordDamageDealt(Token attacker, Token target, int damage, DamageType damageType)
        {
            var attackerStats = GetStats(attacker);
            attackerStats.TotalDamageDealt += damage;
            attackerStats.DamageByType[damageType] = attackerStats.DamageByType.GetValueOrDefault(damageType);

            if (damage > attackerStats.HighestSingleHit)
            {
                attackerStats.HighestSingleHit = damage;
                attackerStats.HighestHitTarget = target.Name;
            }

            var targetStats = GetStats(target);
            targetStats.TotalDamageTaken += damage;

            StatsUpdated?.Invoke();
        }

        public void RecordHealing(Token healer, Token target, int healing)
        {
            var healerStats = GetStats(healer);
            healerStats.TotalHealingDone += healing;

            var targetStats = GetStats(target);
            targetStats.TotalHealingReceived += healing;

            StatsUpdated?.Invoke();
        }

        public void RecordKill(Token killer, Token victim)
        {
            var killerStats = GetStats(killer);
            killerStats.Kills++;
            killerStats.KillList.Add(victim.Name);

            var victimStats = GetStats(victim);
            victimStats.Deaths++;
            victimStats.KilledBy = killer.Name;

            StatsUpdated?.Invoke();
        }

        public void RecordConditionApplied(Token source, Token target, Condition condition)
        {
            var stats = GetStats(source);
            stats.ConditionsApplied++;
            StatsUpdated?.Invoke();
        }

        public void RecordSavingThrow(Token token, bool success, string saveType)
        {
            var stats = GetStats(token);
            if (success)
                stats.SavingThrowSuccesses++;
            else
                stats.SavingThrowFailures++;
            StatsUpdated?.Invoke();
        }

        public int TotalRounds => _totalRounds;
        public TimeSpan CombatDuration => DateTime.Now - _combatStartTime;

        public IEnumerable<TokenCombatStats> GetAllStats() => _stats.Values.OrderByDescending(s => s.TotalDamageDealt);

        public CombatSummary GetCombatSummary()
        {
            var allStats = _stats.Values.ToList();

            return new CombatSummary
            {
                TotalRounds = _totalRounds,
                CombatDuration = CombatDuration,
                TotalDamageDealt = allStats.Sum(s => s.TotalDamageDealt),
                TotalHealing = allStats.Sum(s => s.TotalHealingDone),
                TotalKills = allStats.Sum(s => s.Kills),
                MostDamageDealer = allStats.OrderByDescending(s => s.TotalDamageDealt).FirstOrDefault(),
                MostKills = allStats.OrderByDescending(s => s.Kills).FirstOrDefault(),
                HighestSingleHit = allStats.OrderByDescending(s => s.HighestSingleHit).FirstOrDefault(),
                MostDamageTaken = allStats.OrderByDescending(s => s.TotalDamageTaken).FirstOrDefault()
            };
        }

        public void Clear()
        {
            _stats.Clear();
            _totalRounds = 0;
            StatsUpdated?.Invoke();
        }
    }

    public class TokenCombatStats
    {
        public Guid TokenId { get; set; }
        public string TokenName { get; set; }

        // Attack stats
        public int TotalAttacks { get; set; }
        public int TotalHits { get; set; }
        public int TotalMisses { get; set; }
        public int CriticalHits { get; set; }
        public int CriticalMisses { get; set; }

        public double HitPercentage => TotalAttacks > 0 ? (double)TotalHits / TotalAttacks * 100 : 0;

        // Damage stats
        public int TotalDamageDealt { get; set; }
        public int TotalDamageTaken { get; set; }
        public int HighestSingleHit { get; set; }
        public string HighestHitTarget { get; set; }
        public Dictionary<DamageType, int> DamageByType { get; set; } = new Dictionary<DamageType, int>();

        // Healing stats
        public int TotalHealingDone { get; set; }
        public int TotalHealingReceived { get; set; }

        // Kill stats
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public List<string> KillList { get; set; } = new List<string>();
        public string KilledBy { get; set; }


        // Other stats
        public int ConditionsApplied { get; set; }
        public int SavingThrowSuccesses { get; set; }
        public int SavingThrowFailures { get; set; }

        public double AverageDamagePerHit => TotalHits > 0 ? (double)TotalDamageDealt / TotalHits : 0;
    }

    public class CombatSummary
    {
        public int TotalRounds { get; set; }
        public TimeSpan CombatDuration { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalHealing { get; set; }
        public int TotalKills { get; set; }
        public TokenCombatStats MostDamageDealer { get; set; }
        public TokenCombatStats MostKills { get; set; }
        public TokenCombatStats HighestSingleHit { get; set; }
        public TokenCombatStats MostDamageTaken { get; set; }
    }
}
