using DnDBattle.Services.UI;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Combat;

namespace DnDBattle.Tests.Services.UI
{
    public class CombatStatisticsServiceTests
    {
        private Token CreateToken(string name)
        {
            return new Token { Name = name };
        }

        [Fact]
        public void RecordAttack_TracksAttacks()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin = CreateToken("Goblin");

            service.RecordAttack(fighter, goblin, true, false, false);
            service.RecordAttack(fighter, goblin, false, false, false);

            var stats = service.GetStats(fighter);
            Assert.Equal(2, stats.TotalAttacks);
            Assert.Equal(1, stats.TotalHits);
            Assert.Equal(1, stats.TotalMisses);
        }

        [Fact]
        public void RecordAttack_Critical_TrackedSeparately()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin = CreateToken("Goblin");

            service.RecordAttack(fighter, goblin, true, true, false);

            var stats = service.GetStats(fighter);
            Assert.Equal(1, stats.CriticalHits);
        }

        [Fact]
        public void RecordAttack_CriticalMiss_TrackedSeparately()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin = CreateToken("Goblin");

            service.RecordAttack(fighter, goblin, false, false, true);

            var stats = service.GetStats(fighter);
            Assert.Equal(1, stats.CriticalMisses);
        }

        [Fact]
        public void RecordDamageDealt_AccumulatesDamage()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin = CreateToken("Goblin");

            service.RecordDamageDealt(fighter, goblin, 15, DamageType.Slashing);
            service.RecordDamageDealt(fighter, goblin, 10, DamageType.Slashing);

            var stats = service.GetStats(fighter);
            Assert.Equal(25, stats.TotalDamageDealt);
        }

        [Fact]
        public void RecordDamageDealt_TracksHighestSingleHit()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin = CreateToken("Goblin");

            service.RecordDamageDealt(fighter, goblin, 15, DamageType.Slashing);
            service.RecordDamageDealt(fighter, goblin, 25, DamageType.Slashing);

            var stats = service.GetStats(fighter);
            Assert.Equal(25, stats.HighestSingleHit);
            Assert.Equal("Goblin", stats.HighestHitTarget);
        }

        [Fact]
        public void RecordDamageDealt_TracksTargetDamageTaken()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin = CreateToken("Goblin");

            service.RecordDamageDealt(fighter, goblin, 15, DamageType.Slashing);

            var targetStats = service.GetStats(goblin);
            Assert.Equal(15, targetStats.TotalDamageTaken);
        }

        [Fact]
        public void RecordHealing_AccumulatesHealing()
        {
            var service = new CombatStatisticsService();
            var cleric = CreateToken("Cleric");
            var fighter = CreateToken("Fighter");

            service.RecordHealing(cleric, fighter, 20);

            var healerStats = service.GetStats(cleric);
            Assert.Equal(20, healerStats.TotalHealingDone);

            var targetStats = service.GetStats(fighter);
            Assert.Equal(20, targetStats.TotalHealingReceived);
        }

        [Fact]
        public void RecordKill_TracksKills()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin1 = CreateToken("Goblin 1");
            var goblin2 = CreateToken("Goblin 2");

            service.RecordKill(fighter, goblin1);
            service.RecordKill(fighter, goblin2);

            var stats = service.GetStats(fighter);
            Assert.Equal(2, stats.Kills);
            Assert.Contains("Goblin 1", stats.KillList);
            Assert.Contains("Goblin 2", stats.KillList);
        }

        [Fact]
        public void RecordKill_TracksDeaths()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin = CreateToken("Goblin");

            service.RecordKill(fighter, goblin);

            var stats = service.GetStats(goblin);
            Assert.Equal(1, stats.Deaths);
            Assert.Equal("Fighter", stats.KilledBy);
        }

        [Fact]
        public void RecordSavingThrow_TracksSaves()
        {
            var service = new CombatStatisticsService();
            var wizard = CreateToken("Wizard");

            service.RecordSavingThrow(wizard, true, "DEX");
            service.RecordSavingThrow(wizard, false, "WIS");
            service.RecordSavingThrow(wizard, true, "CON");

            var stats = service.GetStats(wizard);
            Assert.Equal(2, stats.SavingThrowSuccesses);
            Assert.Equal(1, stats.SavingThrowFailures);
        }

        [Fact]
        public void GetCombatSummary_ReturnsOverallSummary()
        {
            var service = new CombatStatisticsService();
            service.StartCombat();
            var fighter = CreateToken("Fighter");
            var wizard = CreateToken("Wizard");
            var goblin = CreateToken("Goblin");

            service.RecordDamageDealt(fighter, goblin, 10, DamageType.Slashing);
            service.RecordDamageDealt(wizard, goblin, 20, DamageType.Fire);

            var summary = service.GetCombatSummary();
            Assert.Equal(30, summary.TotalDamageDealt);
        }

        [Fact]
        public void Clear_ClearsAll()
        {
            var service = new CombatStatisticsService();
            var fighter = CreateToken("Fighter");
            var goblin = CreateToken("Goblin");
            service.RecordAttack(fighter, goblin, true, false, false);
            service.Clear();

            var allStats = service.GetAllStats().ToList();
            Assert.Empty(allStats);
        }

        [Fact]
        public void StartCombat_ResetsState()
        {
            var service = new CombatStatisticsService();
            service.StartCombat();
            Assert.Equal(1, service.TotalRounds);
        }

        [Fact]
        public void NewRound_IncrementsRounds()
        {
            var service = new CombatStatisticsService();
            service.StartCombat();
            service.NewRound();
            Assert.Equal(2, service.TotalRounds);
        }

        [Fact]
        public void HitPercentage_CalculatesCorrectly()
        {
            var stats = new TokenCombatStats { TotalAttacks = 10, TotalHits = 7 };
            Assert.Equal(70.0, stats.HitPercentage);
        }

        [Fact]
        public void HitPercentage_NoAttacks_ReturnsZero()
        {
            var stats = new TokenCombatStats();
            Assert.Equal(0, stats.HitPercentage);
        }

        [Fact]
        public void AverageDamagePerHit_CalculatesCorrectly()
        {
            var stats = new TokenCombatStats { TotalDamageDealt = 100, TotalHits = 10 };
            Assert.Equal(10.0, stats.AverageDamagePerHit);
        }
    }
}
