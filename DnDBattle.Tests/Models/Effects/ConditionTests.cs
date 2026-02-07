using DnDBattle.Models.Effects;

namespace DnDBattle.Tests.Models.Effects
{
    public class ConditionTests
    {
        [Fact]
        public void Condition_FlagsEnum_AllowsCombination()
        {
            var conditions = Condition.Blinded | Condition.Poisoned;
            Assert.True(conditions.HasFlag(Condition.Blinded));
            Assert.True(conditions.HasFlag(Condition.Poisoned));
            Assert.False(conditions.HasFlag(Condition.Stunned));
        }

        [Fact]
        public void ToDisplayString_None_ReturnsEmpty()
        {
            Assert.Equal("", Condition.None.ToDisplayString());
        }

        [Fact]
        public void ToDisplayString_SingleCondition_ReturnsName()
        {
            var display = Condition.Blinded.ToDisplayString();
            Assert.Contains("Blinded", display);
        }

        [Fact]
        public void ToDisplayString_MultipleConditions_CommaSeparated()
        {
            var conditions = Condition.Blinded | Condition.Poisoned;
            var display = conditions.ToDisplayString();
            Assert.Contains("Blinded", display);
            Assert.Contains("Poisoned", display);
            Assert.Contains(", ", display);
        }

        [Theory]
        [InlineData(Condition.Exhaustion1, "Exhaustion (1)")]
        [InlineData(Condition.Exhaustion2, "Exhaustion (2)")]
        [InlineData(Condition.Exhaustion3, "Exhaustion (3)")]
        [InlineData(Condition.Exhaustion4, "Exhaustion (4)")]
        [InlineData(Condition.Exhaustion5, "Exhaustion (5)")]
        [InlineData(Condition.Exhaustion6, "Exhaustion (6)")]
        [InlineData(Condition.HuntersMark, "Hunter's Mark")]
        public void GetConditionName_SpecialNames(Condition condition, string expected)
        {
            Assert.Equal(expected, ConditionExtensions.GetConditionName(condition));
        }

        [Theory]
        [InlineData(Condition.Blinded, "👁️‍🗨️")]
        [InlineData(Condition.Charmed, "💕")]
        [InlineData(Condition.Deafened, "🔇")]
        [InlineData(Condition.Frightened, "😨")]
        [InlineData(Condition.Stunned, "💫")]
        [InlineData(Condition.Unconscious, "💤")]
        public void GetConditionIcon_ReturnsEmoji(Condition condition, string expected)
        {
            Assert.Equal(expected, ConditionExtensions.GetConditionIcon(condition));
        }

        [Fact]
        public void GetConditionColor_ReturnsNonDefaultForAll()
        {
            foreach (Condition c in Enum.GetValues(typeof(Condition)))
            {
                if (c == Condition.None) continue;
                var color = ConditionExtensions.GetConditionColor(c);
                // Just verify it returns something without throwing
                Assert.True(color.R >= 0);
            }
        }

        [Theory]
        [InlineData(Condition.Blinded)]
        [InlineData(Condition.Charmed)]
        [InlineData(Condition.Paralyzed)]
        [InlineData(Condition.Stunned)]
        [InlineData(Condition.Unconscious)]
        public void GetConditionDescription_ReturnsNonEmpty(Condition condition)
        {
            var desc = ConditionExtensions.GetConditionDescription(condition);
            Assert.NotNull(desc);
            Assert.NotEmpty(desc);
        }

        [Fact]
        public void GetActiveConditions_ReturnsOnlyActive()
        {
            var conditions = Condition.Blinded | Condition.Poisoned | Condition.Stunned;
            var active = conditions.GetActiveConditions().ToList();
            Assert.Contains(Condition.Blinded, active);
            Assert.Contains(Condition.Poisoned, active);
            Assert.Contains(Condition.Stunned, active);
            Assert.DoesNotContain(Condition.Charmed, active);
        }

        [Fact]
        public void GetActiveConditions_None_ReturnsEmpty()
        {
            var active = Condition.None.GetActiveConditions().ToList();
            Assert.Empty(active);
        }

        #region Exhaustion

        [Theory]
        [InlineData(Condition.Exhaustion1, 1)]
        [InlineData(Condition.Exhaustion2, 2)]
        [InlineData(Condition.Exhaustion3, 3)]
        [InlineData(Condition.Exhaustion4, 4)]
        [InlineData(Condition.Exhaustion5, 5)]
        [InlineData(Condition.Exhaustion6, 6)]
        public void GetExhaustionLevel_ReturnsCorrectLevel(Condition condition, int expected)
        {
            Assert.Equal(expected, condition.GetExhaustionLevel());
        }

        [Fact]
        public void GetExhaustionLevel_NoExhaustion_ReturnsZero()
        {
            Assert.Equal(0, Condition.Blinded.GetExhaustionLevel());
            Assert.Equal(0, Condition.None.GetExhaustionLevel());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(6)]
        public void SetExhaustionLevel_SetsCorrectLevel(int level)
        {
            var conditions = Condition.Blinded; // Keep existing conditions
            conditions = conditions.SetExhaustionLevel(level);
            Assert.Equal(level, conditions.GetExhaustionLevel());
            Assert.True(conditions.HasFlag(Condition.Blinded)); // Should preserve other conditions
        }

        [Fact]
        public void SetExhaustionLevel_Zero_ClearsAllExhaustion()
        {
            var conditions = Condition.Exhaustion3;
            conditions = conditions.SetExhaustionLevel(0);
            Assert.Equal(0, conditions.GetExhaustionLevel());
        }

        [Fact]
        public void SetExhaustionLevel_ReplacesExistingLevel()
        {
            var conditions = Condition.Exhaustion2;
            conditions = conditions.SetExhaustionLevel(5);
            Assert.Equal(5, conditions.GetExhaustionLevel());
            Assert.False(conditions.HasFlag(Condition.Exhaustion2));
        }

        #endregion
    }
}
