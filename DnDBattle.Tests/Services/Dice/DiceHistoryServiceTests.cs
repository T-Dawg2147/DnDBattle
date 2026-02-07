using DnDBattle.Services.Dice;

namespace DnDBattle.Tests.Services.Dice
{
    public class DiceHistoryServiceTests
    {
        [Fact]
        public void RecordRoll_AddsToHistory()
        {
            var service = new DiceHistoryService();
            service.RecordRoll("Fighter", "1d20", 15, DiceRollType.Attack);
            Assert.Single(service.History);
        }

        [Fact]
        public void RecordRoll_InsertsAtBeginning()
        {
            var service = new DiceHistoryService();
            service.RecordRoll("First", "1d20", 10, DiceRollType.Attack);
            service.RecordRoll("Second", "1d20", 15, DiceRollType.Attack);
            Assert.Equal("Second", service.History[0].RollerName);
            Assert.Equal("First", service.History[1].RollerName);
        }

        [Fact]
        public void RecordRoll_TrimsHistory_WhenOverMax()
        {
            var service = new DiceHistoryService { MaxHistorySize = 5 };
            for (int i = 0; i < 10; i++)
            {
                service.RecordRoll("Test", "1d20", i, DiceRollType.Attack);
            }
            Assert.Equal(5, service.History.Count);
        }

        [Fact]
        public void RecordRoll_FiresEvent()
        {
            var service = new DiceHistoryService();
            DiceRollRecord? captured = null;
            service.RollRecorded += (record) => captured = record;

            service.RecordRoll("Fighter", "1d20", 15, DiceRollType.Attack);
            Assert.NotNull(captured);
            Assert.Equal("Fighter", captured!.RollerName);
        }

        [Fact]
        public void RecordRoll_WithRecord_AddsDirectly()
        {
            var service = new DiceHistoryService();
            var record = new DiceRollRecord
            {
                RollerName = "Wizard",
                Expression = "3d6",
                Result = 12,
                RollType = DiceRollType.Damage,
                Timestamp = DateTime.Now
            };
            service.RecordRoll(record);
            Assert.Single(service.History);
            Assert.Equal("Wizard", service.History[0].RollerName);
        }

        [Fact]
        public void GetByRoller_FiltersByName()
        {
            var service = new DiceHistoryService();
            service.RecordRoll("Fighter", "1d20", 15, DiceRollType.Attack);
            service.RecordRoll("Wizard", "1d20", 12, DiceRollType.Attack);
            service.RecordRoll("Fighter", "1d8", 6, DiceRollType.Damage);

            var fighterRolls = service.GetByRoller("Fighter").ToList();
            Assert.Equal(2, fighterRolls.Count);
            Assert.All(fighterRolls, r => Assert.Equal("Fighter", r.RollerName));
        }

        [Fact]
        public void GetByRoller_CaseInsensitive()
        {
            var service = new DiceHistoryService();
            service.RecordRoll("Fighter", "1d20", 15, DiceRollType.Attack);
            Assert.Single(service.GetByRoller("fighter"));
        }

        [Fact]
        public void GetByType_FiltersByRollType()
        {
            var service = new DiceHistoryService();
            service.RecordRoll("Fighter", "1d20", 15, DiceRollType.Attack);
            service.RecordRoll("Fighter", "1d8", 6, DiceRollType.Damage);
            service.RecordRoll("Fighter", "1d20", 18, DiceRollType.SavingThrow);

            var attacks = service.GetByType(DiceRollType.Attack).ToList();
            Assert.Single(attacks);
        }

        [Fact]
        public void GetRecent_ReturnsRequestedCount()
        {
            var service = new DiceHistoryService();
            for (int i = 0; i < 10; i++)
            {
                service.RecordRoll("Test", "1d20", i, DiceRollType.Attack);
            }
            Assert.Equal(3, service.GetRecent(3).Count());
        }

        [Fact]
        public void GetStatistics_CalculatesCorrectly()
        {
            var service = new DiceHistoryService();
            service.RecordRoll(new DiceRollRecord
            {
                RollerName = "Fighter",
                Expression = "1d20",
                Result = 20,
                NaturalRoll = 20,
                RollType = DiceRollType.Attack
            });
            service.RecordRoll(new DiceRollRecord
            {
                RollerName = "Fighter",
                Expression = "1d20",
                Result = 1,
                NaturalRoll = 1,
                RollType = DiceRollType.Attack
            });

            var stats = service.GetStatistics("Fighter");
            Assert.Equal(2, stats.TotalRolls);
            Assert.Equal(2, stats.D20Rolls);
            Assert.Equal(1, stats.Natural20s);
            Assert.Equal(1, stats.Natural1s);
            Assert.Equal(20, stats.HighestRoll);
            Assert.Equal(1, stats.LowestRoll);
        }

        [Fact]
        public void GetStatistics_NoRolls_ReturnsZeroDefaults()
        {
            var service = new DiceHistoryService();
            var stats = service.GetStatistics();
            Assert.Equal(0, stats.TotalRolls);
            Assert.Equal(0, stats.D20Rolls);
            Assert.Equal(0, stats.AverageD20);
        }

        [Fact]
        public void Clear_RemovesAll()
        {
            var service = new DiceHistoryService();
            service.RecordRoll("Test", "1d20", 15, DiceRollType.Attack);
            service.Clear();
            Assert.Empty(service.History);
        }

        #region DiceRollRecord

        [Fact]
        public void DiceRollRecord_IsCritical_True_On20()
        {
            var record = new DiceRollRecord { NaturalRoll = 20 };
            Assert.True(record.IsCritical);
            Assert.False(record.IsCriticalFail);
        }

        [Fact]
        public void DiceRollRecord_IsCriticalFail_True_On1()
        {
            var record = new DiceRollRecord { NaturalRoll = 1 };
            Assert.True(record.IsCriticalFail);
            Assert.False(record.IsCritical);
        }

        [Fact]
        public void DiceRollRecord_DisplayText_FormatsCorrectly()
        {
            var record = new DiceRollRecord
            {
                RollerName = "Fighter",
                Expression = "1d20+5",
                Result = 18
            };
            Assert.Equal("Fighter: 1d20+5 = 18", record.DisplayText);
        }

        #endregion

        #region DiceRollStatistics

        [Fact]
        public void CritRate_CalculatesPercentage()
        {
            var stats = new DiceRollStatistics { D20Rolls = 100, Natural20s = 5 };
            Assert.Equal(5.0, stats.CritRate);
        }

        [Fact]
        public void CritFailRate_CalculatesPercentage()
        {
            var stats = new DiceRollStatistics { D20Rolls = 100, Natural1s = 10 };
            Assert.Equal(10.0, stats.CritFailRate);
        }

        [Fact]
        public void CritRate_NoRolls_ReturnsZero()
        {
            var stats = new DiceRollStatistics { D20Rolls = 0, Natural20s = 0 };
            Assert.Equal(0, stats.CritRate);
        }

        #endregion
    }
}
