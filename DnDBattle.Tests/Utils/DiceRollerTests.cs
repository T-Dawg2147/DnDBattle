using DnDBattle.Utils;

namespace DnDBattle.Tests.Utils
{
    public class DiceRollerTests
    {
        [Fact]
        public void RollExpression_NullOrWhitespace_ReturnsOneWithTotal1()
        {
            var result = DiceRoller.RollExpression("");
            Assert.Equal(1, result.Total);
            Assert.Single(result.Individual);
        }

        [Fact]
        public void RollExpression_PlainInteger_ReturnsThatValue()
        {
            var result = DiceRoller.RollExpression("5");
            Assert.Equal(5, result.Total);
            Assert.Single(result.Individual);
            Assert.Equal(5, result.Individual[0]);
        }

        [Fact]
        public void RollExpression_NegativeInteger_ReturnsThatValue()
        {
            var result = DiceRoller.RollExpression("-3");
            Assert.Equal(-3, result.Total);
        }

        [Fact]
        public void RollExpression_1d6_ReturnsValueBetween1And6()
        {
            for (int i = 0; i < 100; i++)
            {
                var result = DiceRoller.RollExpression("1d6");
                Assert.InRange(result.Total, 1, 6);
                Assert.Single(result.Individual);
                Assert.InRange(result.Individual[0], 1, 6);
            }
        }

        [Fact]
        public void RollExpression_2d6_ReturnsSumOfTwoDice()
        {
            for (int i = 0; i < 100; i++)
            {
                var result = DiceRoller.RollExpression("2d6");
                Assert.Equal(2, result.Individual.Count);
                Assert.Equal(result.Individual.Sum(), result.Total);
                Assert.InRange(result.Total, 2, 12);
            }
        }

        [Fact]
        public void RollExpression_1d20Plus5_AddsModifier()
        {
            for (int i = 0; i < 100; i++)
            {
                var result = DiceRoller.RollExpression("1d20+5");
                Assert.Single(result.Individual);
                Assert.InRange(result.Individual[0], 1, 20);
                Assert.Equal(result.Individual[0] + 5, result.Total);
            }
        }

        [Fact]
        public void RollExpression_1d20Minus3_SubtractsModifier()
        {
            for (int i = 0; i < 100; i++)
            {
                var result = DiceRoller.RollExpression("1d20-3");
                Assert.Single(result.Individual);
                Assert.InRange(result.Individual[0], 1, 20);
                Assert.Equal(result.Individual[0] - 3, result.Total);
            }
        }

        [Fact]
        public void RollExpression_d20_ImpliesOneDie()
        {
            var result = DiceRoller.RollExpression("d20");
            Assert.Single(result.Individual);
            Assert.InRange(result.Total, 1, 20);
        }

        [Fact]
        public void RollExpression_4d6_RollsFourDice()
        {
            var result = DiceRoller.RollExpression("4d6");
            Assert.Equal(4, result.Individual.Count);
            foreach (var roll in result.Individual)
            {
                Assert.InRange(roll, 1, 6);
            }
            Assert.Equal(result.Individual.Sum(), result.Total);
        }

        [Fact]
        public void RollExpression_InvalidInput_ReturnsDefault()
        {
            var result = DiceRoller.RollExpression("abc");
            Assert.Equal(1, result.Total);
        }

        [Fact]
        public void RollExpression_TrimsAndLowercases()
        {
            var result = DiceRoller.RollExpression("  1D6  ");
            Assert.InRange(result.Total, 1, 6);
        }

        [Fact]
        public void RollExpression_LargeDiceCount_CappedAt100()
        {
            var result = DiceRoller.RollExpression("200d6");
            Assert.Equal(100, result.Individual.Count);
        }

        [Fact]
        public void RollExpression_DiceResult_HasExpression()
        {
            var result = DiceRoller.RollExpression("2d8+3");
            Assert.Equal("2d8+3", result.Expression);
        }

        [Fact]
        public void DiceResult_DefaultValues()
        {
            var result = new DiceResult();
            Assert.Equal(0, result.Total);
            Assert.NotNull(result.Individual);
            Assert.Empty(result.Individual);
        }
    }
}
