using DnDBattle.Services.Encounters;
using DnDBattle.Models.Creatures;

namespace DnDBattle.Tests.Services.Encounters
{
    public class EncounterBuilderServiceTests
    {
        private readonly EncounterBuilderService _service = new();

        #region GetXPForCR

        [Theory]
        [InlineData("0", 10)]
        [InlineData("1/8", 25)]
        [InlineData("1/4", 50)]
        [InlineData("1/2", 100)]
        [InlineData("1", 200)]
        [InlineData("5", 1800)]
        [InlineData("10", 5900)]
        [InlineData("20", 25000)]
        [InlineData("30", 155000)]
        public void GetXPForCR_StandardCR_ReturnsCorrectXP(string cr, int expected)
        {
            Assert.Equal(expected, _service.GetXPForCR(cr));
        }

        [Fact]
        public void GetXPForCR_NullOrEmpty_ReturnsZero()
        {
            Assert.Equal(0, _service.GetXPForCR(null!));
            Assert.Equal(0, _service.GetXPForCR(""));
            Assert.Equal(0, _service.GetXPForCR("   "));
        }

        [Fact]
        public void GetXPForCR_InvalidCR_ReturnsZero()
        {
            Assert.Equal(0, _service.GetXPForCR("abc"));
            Assert.Equal(0, _service.GetXPForCR("999"));
        }

        [Fact]
        public void GetXPForCR_TrimsWhitespace()
        {
            Assert.Equal(200, _service.GetXPForCR("  1  "));
        }

        #endregion

        #region GetEncounterMultiplier

        [Theory]
        [InlineData(1, 4, 1.0)]
        [InlineData(2, 4, 1.5)]
        [InlineData(3, 4, 2.0)]
        [InlineData(6, 4, 2.0)]
        [InlineData(7, 4, 2.5)]
        [InlineData(10, 4, 2.5)]
        [InlineData(11, 4, 3.0)]
        [InlineData(14, 4, 3.0)]
        [InlineData(15, 4, 4.0)]
        public void GetEncounterMultiplier_NormalParty_CorrectMultiplier(int monsterCount, int partySize, double expected)
        {
            Assert.Equal(expected, _service.GetEncounterMultiplier(monsterCount, partySize));
        }

        [Fact]
        public void GetEncounterMultiplier_SmallParty_HigherMultipliers()
        {
            // Small party (< 3) increases multiplier by one step
            double smallParty = _service.GetEncounterMultiplier(1, 2);
            double normalParty = _service.GetEncounterMultiplier(1, 4);
            Assert.True(smallParty > normalParty);
        }

        [Fact]
        public void GetEncounterMultiplier_LargeParty_LowerMultiplier()
        {
            // Large party (>= 6) - multiplier for 2 monsters: 1.5 -> 1.0
            double largeParty = _service.GetEncounterMultiplier(2, 6);
            Assert.Equal(1.0, largeParty);
        }

        #endregion

        #region GetPartyThresholds

        [Fact]
        public void GetPartyThresholds_Level1Party4_CorrectValues()
        {
            var (easy, medium, hard, deadly) = _service.GetPartyThresholds(4, 1);
            Assert.Equal(100, easy);    // 25 * 4
            Assert.Equal(200, medium);  // 50 * 4
            Assert.Equal(300, hard);    // 75 * 4
            Assert.Equal(400, deadly);  // 100 * 4
        }

        [Fact]
        public void GetPartyThresholds_ClampsLevelTo1_20()
        {
            // Level 0 should be treated as level 1
            var (easy0, _, _, _) = _service.GetPartyThresholds(4, 0);
            var (easy1, _, _, _) = _service.GetPartyThresholds(4, 1);
            Assert.Equal(easy1, easy0);

            // Level 25 should be treated as level 20
            var (_, _, _, deadly25) = _service.GetPartyThresholds(4, 25);
            var (_, _, _, deadly20) = _service.GetPartyThresholds(4, 20);
            Assert.Equal(deadly20, deadly25);
        }

        #endregion

        #region CalculateDifficulty

        [Fact]
        public void CalculateDifficulty_BelowEasy_Trivial()
        {
            // Party of 4 at level 1: Easy = 100
            Assert.Equal(EncounterDifficulty.Trivial, _service.CalculateDifficulty(50, 4, 1));
        }

        [Fact]
        public void CalculateDifficulty_AtEasy_Easy()
        {
            Assert.Equal(EncounterDifficulty.Easy, _service.CalculateDifficulty(100, 4, 1));
        }

        [Fact]
        public void CalculateDifficulty_AtMedium_Medium()
        {
            Assert.Equal(EncounterDifficulty.Medium, _service.CalculateDifficulty(200, 4, 1));
        }

        [Fact]
        public void CalculateDifficulty_AtHard_Hard()
        {
            Assert.Equal(EncounterDifficulty.Hard, _service.CalculateDifficulty(300, 4, 1));
        }

        [Fact]
        public void CalculateDifficulty_AtDeadly_Deadly()
        {
            Assert.Equal(EncounterDifficulty.Deadly, _service.CalculateDifficulty(400, 4, 1));
        }

        #endregion

        #region CalculateEncounter

        [Fact]
        public void CalculateEncounter_SingleMonster_CorrectCalculation()
        {
            var creatures = new List<EncounterCreature>
            {
                new EncounterCreature
                {
                    Creature = new Token { ChallengeRating = "1" },
                    Quantity = 1
                }
            };

            var result = _service.CalculateEncounter(creatures, 4, 1);
            Assert.Equal(1, result.TotalCreatures);
            Assert.Equal(200, result.TotalXP);
            Assert.Equal(1.0, result.Multiplier);
            Assert.Equal(200, result.AdjustedXP);
        }

        [Fact]
        public void CalculateEncounter_MultipleMonsters_AppliesMultiplier()
        {
            var creatures = new List<EncounterCreature>
            {
                new EncounterCreature
                {
                    Creature = new Token { ChallengeRating = "1" },
                    Quantity = 3
                }
            };

            var result = _service.CalculateEncounter(creatures, 4, 1);
            Assert.Equal(3, result.TotalCreatures);
            Assert.Equal(600, result.TotalXP);
            Assert.Equal(2.0, result.Multiplier);
            Assert.Equal(1200, result.AdjustedXP);
        }

        [Fact]
        public void CalculateEncounter_ReturnsThresholds()
        {
            var creatures = new List<EncounterCreature>
            {
                new EncounterCreature
                {
                    Creature = new Token { ChallengeRating = "1" },
                    Quantity = 1
                }
            };

            var result = _service.CalculateEncounter(creatures, 4, 1);
            Assert.True(result.EasyThreshold > 0);
            Assert.True(result.MediumThreshold > result.EasyThreshold);
            Assert.True(result.HardThreshold > result.MediumThreshold);
            Assert.True(result.DeadlyThreshold > result.HardThreshold);
        }

        #endregion

        #region EncounterDifficulty Extensions

        [Theory]
        [InlineData(EncounterDifficulty.Trivial, "Trivial")]
        [InlineData(EncounterDifficulty.Easy, "Easy")]
        [InlineData(EncounterDifficulty.Medium, "Medium")]
        [InlineData(EncounterDifficulty.Hard, "Hard")]
        [InlineData(EncounterDifficulty.Deadly, "Deadly")]
        public void GetDisplayName_ReturnsCorrectName(EncounterDifficulty difficulty, string expected)
        {
            Assert.Equal(expected, difficulty.GetDisplayName());
        }

        [Fact]
        public void GetIcon_ReturnsNonEmpty()
        {
            foreach (EncounterDifficulty d in Enum.GetValues(typeof(EncounterDifficulty)))
            {
                Assert.NotEmpty(d.GetIcon());
            }
        }

        #endregion
    }
}
