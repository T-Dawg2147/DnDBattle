using DnDBattle.Models.Creatures;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Effects;
using DnDBattle.Services.Combat;

namespace DnDBattle.Tests.Services.Combat
{
    public class CombatConditionHelperTests
    {
        private Token CreateToken(Condition conditions = Condition.None, int gridX = 0, int gridY = 0)
        {
            return new Token
            {
                Name = "Test",
                Conditions = conditions,
                GridX = gridX,
                GridY = gridY
            };
        }

        #region CanMove

        [Fact]
        public void CanMove_NoConditions_True()
        {
            var token = CreateToken();
            Assert.True(CombatConditionHelper.CanMove(token));
        }

        [Theory]
        [InlineData(Condition.Stunned)]
        [InlineData(Condition.Paralyzed)]
        [InlineData(Condition.Restrained)]
        [InlineData(Condition.Unconscious)]
        [InlineData(Condition.Petrified)]
        public void CanMove_ImmobilizingConditions_False(Condition condition)
        {
            var token = CreateToken(condition);
            Assert.False(CombatConditionHelper.CanMove(token));
        }

        [Theory]
        [InlineData(Condition.Blinded)]
        [InlineData(Condition.Poisoned)]
        [InlineData(Condition.Charmed)]
        [InlineData(Condition.Frightened)]
        public void CanMove_NonImmobilizingConditions_True(Condition condition)
        {
            var token = CreateToken(condition);
            Assert.True(CombatConditionHelper.CanMove(token));
        }

        #endregion

        #region GetAttackModifier

        [Fact]
        public void GetAttackModifier_NoConditions_Normal()
        {
            var attacker = CreateToken();
            var defender = CreateToken();
            Assert.Equal(AttackMode.Normal, CombatConditionHelper.GetAttackModifier(attacker, defender));
        }

        [Theory]
        [InlineData(Condition.Invisible)]
        [InlineData(Condition.Hidden)]
        public void GetAttackModifier_AdvantageConditions_ReturnAdvantage(Condition condition)
        {
            var attacker = CreateToken(condition);
            var defender = CreateToken();
            Assert.Equal(AttackMode.Advantage, CombatConditionHelper.GetAttackModifier(attacker, defender));
        }

        [Theory]
        [InlineData(Condition.Blinded)]
        [InlineData(Condition.Frightened)]
        [InlineData(Condition.Poisoned)]
        [InlineData(Condition.Prone)]
        [InlineData(Condition.Restrained)]
        public void GetAttackModifier_DisadvantageConditions_ReturnDisadvantage(Condition condition)
        {
            var attacker = CreateToken(condition);
            var defender = CreateToken();
            Assert.Equal(AttackMode.Disadvantage, CombatConditionHelper.GetAttackModifier(attacker, defender));
        }

        #endregion

        #region GetDefenseModifier

        [Fact]
        public void GetDefenseModifier_NoConditions_Normal()
        {
            var defender = CreateToken();
            Assert.Equal(AttackMode.Normal, CombatConditionHelper.GetDefenseModifier(defender));
        }

        [Theory]
        [InlineData(Condition.Paralyzed)]
        [InlineData(Condition.Unconscious)]
        [InlineData(Condition.Stunned)]
        [InlineData(Condition.Restrained)]
        [InlineData(Condition.Prone)]
        public void GetDefenseModifier_VulnerableConditions_ReturnAdvantage(Condition condition)
        {
            var defender = CreateToken(condition);
            Assert.Equal(AttackMode.Advantage, CombatConditionHelper.GetDefenseModifier(defender));
        }

        [Theory]
        [InlineData(Condition.Invisible)]
        [InlineData(Condition.Dodging)]
        public void GetDefenseModifier_ProtectiveConditions_ReturnDisadvantage(Condition condition)
        {
            var defender = CreateToken(condition);
            Assert.Equal(AttackMode.Disadvantage, CombatConditionHelper.GetDefenseModifier(defender));
        }

        #endregion

        #region AutoFailSave

        [Theory]
        [InlineData(Condition.Stunned)]
        [InlineData(Condition.Paralyzed)]
        [InlineData(Condition.Unconscious)]
        public void AutoFailSave_StrSave_ImmobilizedConditions_True(Condition condition)
        {
            var token = CreateToken(condition);
            Assert.True(CombatConditionHelper.AutoFailSave(token, Ability.Strength));
        }

        [Theory]
        [InlineData(Condition.Stunned)]
        [InlineData(Condition.Paralyzed)]
        [InlineData(Condition.Unconscious)]
        public void AutoFailSave_DexSave_ImmobilizedConditions_True(Condition condition)
        {
            var token = CreateToken(condition);
            Assert.True(CombatConditionHelper.AutoFailSave(token, Ability.Dexterity));
        }

        [Fact]
        public void AutoFailSave_ConSave_NeverAutoFails()
        {
            var token = CreateToken(Condition.Stunned);
            Assert.False(CombatConditionHelper.AutoFailSave(token, Ability.Constitution));
        }

        [Fact]
        public void AutoFailSave_NoConditions_False()
        {
            var token = CreateToken();
            Assert.False(CombatConditionHelper.AutoFailSave(token, Ability.Strength));
        }

        #endregion

        #region IsAutoCrit

        [Fact]
        public void IsAutoCrit_ParalyzedWithin5ft_True()
        {
            var attacker = CreateToken(gridX: 0, gridY: 0);
            var defender = CreateToken(Condition.Paralyzed, gridX: 1, gridY: 0);
            Assert.True(CombatConditionHelper.IsAutoCrit(attacker, defender));
        }

        [Fact]
        public void IsAutoCrit_UnconsciousWithin5ft_True()
        {
            var attacker = CreateToken(gridX: 0, gridY: 0);
            var defender = CreateToken(Condition.Unconscious, gridX: 0, gridY: 1);
            Assert.True(CombatConditionHelper.IsAutoCrit(attacker, defender));
        }

        [Fact]
        public void IsAutoCrit_ParalyzedFarAway_False()
        {
            var attacker = CreateToken(gridX: 0, gridY: 0);
            var defender = CreateToken(Condition.Paralyzed, gridX: 5, gridY: 5);
            Assert.False(CombatConditionHelper.IsAutoCrit(attacker, defender));
        }

        [Fact]
        public void IsAutoCrit_NoCondition_False()
        {
            var attacker = CreateToken(gridX: 0, gridY: 0);
            var defender = CreateToken(gridX: 0, gridY: 1);
            Assert.False(CombatConditionHelper.IsAutoCrit(attacker, defender));
        }

        #endregion

        #region CalculateDistance

        [Theory]
        [InlineData(0, 0, 3, 0, 3)]
        [InlineData(0, 0, 0, 4, 4)]
        [InlineData(0, 0, 3, 4, 4)]  // Chebyshev distance
        [InlineData(5, 5, 5, 5, 0)]
        [InlineData(2, 3, 5, 7, 4)]
        public void CalculateDistance_ChebyshevDistance(int x1, int y1, int x2, int y2, int expected)
        {
            var a = CreateToken(gridX: x1, gridY: y1);
            var b = CreateToken(gridX: x2, gridY: y2);
            Assert.Equal(expected, CombatConditionHelper.CalculateDistance(a, b));
        }

        #endregion

        #region CanTakeActions

        [Fact]
        public void CanTakeActions_NoConditions_True()
        {
            var token = CreateToken();
            Assert.True(CombatConditionHelper.CanTakeActions(token));
        }

        [Theory]
        [InlineData(Condition.Incapacitated)]
        [InlineData(Condition.Stunned)]
        [InlineData(Condition.Paralyzed)]
        [InlineData(Condition.Unconscious)]
        [InlineData(Condition.Petrified)]
        public void CanTakeActions_IncapacitatingConditions_False(Condition condition)
        {
            var token = CreateToken(condition);
            Assert.False(CombatConditionHelper.CanTakeActions(token));
        }

        #endregion

        #region CanTakeReactions

        [Fact]
        public void CanTakeReactions_NoConditions_True()
        {
            var token = CreateToken();
            Assert.True(CombatConditionHelper.CanTakeReactions(token));
        }

        [Fact]
        public void CanTakeReactions_Stunned_False()
        {
            var token = CreateToken(Condition.Stunned);
            Assert.False(CombatConditionHelper.CanTakeReactions(token));
        }

        #endregion

        #region Cover

        [Theory]
        [InlineData(CoverLevel.None, 0)]
        [InlineData(CoverLevel.Half, 2)]
        [InlineData(CoverLevel.ThreeQuarters, 5)]
        [InlineData(CoverLevel.Full, 0)]
        public void GetCoverACBonus_ReturnsCorrectBonus(CoverLevel cover, int expected)
        {
            Assert.Equal(expected, CombatConditionHelper.GetCoverACBonus(cover));
        }

        [Theory]
        [InlineData(CoverLevel.None, 0)]
        [InlineData(CoverLevel.Half, 2)]
        [InlineData(CoverLevel.ThreeQuarters, 5)]
        [InlineData(CoverLevel.Full, 0)]
        public void GetCoverDexSaveBonus_ReturnsCorrectBonus(CoverLevel cover, int expected)
        {
            Assert.Equal(expected, CombatConditionHelper.GetCoverDexSaveBonus(cover));
        }

        #endregion
    }
}
