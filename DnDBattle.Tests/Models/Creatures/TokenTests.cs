using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Combat;

namespace DnDBattle.Tests.Models.Creatures
{
    public class TokenTests
    {
        private Token CreateTestToken(string name = "Test Token", int hp = 50, int maxHp = 50, int ac = 15)
        {
            return new Token
            {
                Name = name,
                HP = hp,
                MaxHP = maxHp,
                ArmorClass = ac,
                Str = 16, Dex = 14, Con = 12, Int = 10, Wis = 8, Cha = 6
            };
        }

        #region Basic Properties

        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var token = new Token();
            Assert.Equal("Token", token.Name);
            Assert.NotEqual(Guid.Empty, token.Id);
            Assert.Equal(Condition.None, token.Conditions);
            Assert.Equal(0, token.TempHP);
            Assert.False(token.IsPlayer);
            Assert.Equal(1, token.SizeInSquares);
        }

        [Fact]
        public void SpeedSquares_ParsesSpeedString()
        {
            var token = new Token { Speed = "30 ft" };
            Assert.Equal(6, token.SpeedSquares);
        }

        [Fact]
        public void SpeedSquares_NullOrEmpty_ReturnsDefault6()
        {
            var token = new Token { Speed = "" };
            Assert.Equal(6, token.SpeedSquares);
        }

        [Fact]
        public void SpeedSquares_JustNumber_ParsesCorrectly()
        {
            var token = new Token { Speed = "40" };
            Assert.Equal(8, token.SpeedSquares);
        }

        #endregion

        #region Damage System

        [Fact]
        public void TakeDamage_ReducesHP()
        {
            var token = CreateTestToken(hp: 50, maxHp: 50);
            var (damageTaken, desc) = token.TakeDamage(10, DamageType.Slashing);
            Assert.Equal(10, damageTaken);
            Assert.Equal(40, token.HP);
        }

        [Fact]
        public void TakeDamage_HPFloorIsZero()
        {
            var token = CreateTestToken(hp: 5, maxHp: 50);
            token.TakeDamage(20, DamageType.Slashing);
            Assert.Equal(0, token.HP);
        }

        [Fact]
        public void TakeDamage_WithResistance_HalvesDamage()
        {
            var token = CreateTestToken(hp: 50, maxHp: 50);
            token.Resistances = "fire";
            var (damageTaken, desc) = token.TakeDamage(10, DamageType.Fire);
            Assert.Equal(5, damageTaken);
            Assert.Equal(45, token.HP);
            Assert.Contains("Resistant", desc);
        }

        [Fact]
        public void TakeDamage_WithImmunity_NoDamage()
        {
            var token = CreateTestToken(hp: 50, maxHp: 50);
            token.Immunities = "fire";
            var (damageTaken, desc) = token.TakeDamage(10, DamageType.Fire);
            Assert.Equal(0, damageTaken);
            Assert.Equal(50, token.HP);
            Assert.Contains("Immune", desc);
        }

        [Fact]
        public void TakeDamage_WithVulnerability_DoublesDamage()
        {
            var token = CreateTestToken(hp: 50, maxHp: 50);
            token.Vulnerabilities = "cold";
            var (damageTaken, desc) = token.TakeDamage(10, DamageType.Cold);
            Assert.Equal(20, damageTaken);
            Assert.Equal(30, token.HP);
            Assert.Contains("Vulnerable", desc);
        }

        [Fact]
        public void TakeDamage_WithTempHP_AbsorbsDamage()
        {
            var token = CreateTestToken(hp: 50, maxHp: 50);
            token.TempHP = 15;
            var (damageTaken, desc) = token.TakeDamage(10, DamageType.Slashing);
            Assert.Equal(0, damageTaken);
            Assert.Equal(50, token.HP);
            Assert.Equal(5, token.TempHP);
        }

        [Fact]
        public void TakeDamage_ExceedsTempHP_OverflowsToHP()
        {
            var token = CreateTestToken(hp: 50, maxHp: 50);
            token.TempHP = 5;
            var (damageTaken, desc) = token.TakeDamage(15, DamageType.Slashing);
            Assert.Equal(10, damageTaken);
            Assert.Equal(40, token.HP);
            Assert.Equal(0, token.TempHP);
        }

        [Fact]
        public void CalculateEffectiveDamage_NormalDamage_ReturnsOriginal()
        {
            var token = CreateTestToken();
            var (effective, desc) = token.CalculateEffectiveDamage(10, DamageType.Slashing);
            Assert.Equal(10, effective);
            Assert.Null(desc);
        }

        #endregion

        #region Conditions

        [Fact]
        public void AddCondition_SetsFlag()
        {
            var token = CreateTestToken();
            token.AddCondition(Condition.Blinded);
            Assert.True(token.HasCondition(Condition.Blinded));
        }

        [Fact]
        public void RemoveCondition_ClearsFlag()
        {
            var token = CreateTestToken();
            token.AddCondition(Condition.Blinded);
            token.RemoveCondition(Condition.Blinded);
            Assert.False(token.HasCondition(Condition.Blinded));
        }

        [Fact]
        public void ToggleCondition_AddsWhenMissing()
        {
            var token = CreateTestToken();
            token.ToggleCondition(Condition.Poisoned);
            Assert.True(token.HasCondition(Condition.Poisoned));
        }

        [Fact]
        public void ToggleCondition_RemovesWhenPresent()
        {
            var token = CreateTestToken();
            token.AddCondition(Condition.Poisoned);
            token.ToggleCondition(Condition.Poisoned);
            Assert.False(token.HasCondition(Condition.Poisoned));
        }

        [Fact]
        public void MultipleConditions_Supported()
        {
            var token = CreateTestToken();
            token.AddCondition(Condition.Blinded);
            token.AddCondition(Condition.Poisoned);
            Assert.True(token.HasCondition(Condition.Blinded));
            Assert.True(token.HasCondition(Condition.Poisoned));
            Assert.False(token.HasCondition(Condition.Stunned));
        }

        [Fact]
        public void RemoveConditionsByName_RemovesSpecifiedConditions()
        {
            var token = CreateTestToken();
            token.AddCondition(Condition.Blinded);
            token.AddCondition(Condition.Poisoned);
            token.RemoveConditionsByName("blinded");
            Assert.False(token.HasCondition(Condition.Blinded));
            Assert.True(token.HasCondition(Condition.Poisoned));
        }

        [Fact]
        public void RemoveConditionsByName_NullOrEmpty_DoesNothing()
        {
            var token = CreateTestToken();
            token.AddCondition(Condition.Blinded);
            token.RemoveConditionsByName(null!);
            token.RemoveConditionsByName("");
            Assert.True(token.HasCondition(Condition.Blinded));
        }

        #endregion

        #region Legendary Actions

        [Fact]
        public void LegendaryActions_UsageAndReset()
        {
            var token = CreateTestToken();
            token.LegendaryActionsMax = 3;
            token.LegendaryActionsRemaining = 3;

            Assert.True(token.HasLegendaryActions);
            Assert.True(token.UseLegendaryAction(1));
            Assert.Equal(2, token.LegendaryActionsRemaining);

            Assert.True(token.UseLegendaryAction(2));
            Assert.Equal(0, token.LegendaryActionsRemaining);

            Assert.False(token.UseLegendaryAction(1));

            token.ResetLegendaryActions();
            Assert.Equal(3, token.LegendaryActionsRemaining);
        }

        [Fact]
        public void LegendaryActionsDisplay_FormatsCorrectly()
        {
            var token = CreateTestToken();
            token.LegendaryActionsMax = 3;
            token.LegendaryActionsRemaining = 2;
            Assert.Equal("2/3", token.LegendaryActionsDisplay);
        }

        [Fact]
        public void HasLegendaryActions_FalseWhenZero()
        {
            var token = CreateTestToken();
            Assert.False(token.HasLegendaryActions);
        }

        #endregion

        #region Death Saves

        [Fact]
        public void RecordDeathSave_NaturalTwenty_Revives()
        {
            var token = CreateTestToken(hp: 0);
            var result = token.RecordDeathSave(20);
            Assert.Equal(1, token.HP);
            Assert.Contains("Natural 20", result);
        }

        [Fact]
        public void RecordDeathSave_NaturalOne_TwoFailures()
        {
            var token = CreateTestToken(hp: 0);
            token.RecordDeathSave(1);
            Assert.Equal(2, token.DeathSaveFailures);
        }

        [Fact]
        public void RecordDeathSave_RollAbove10_Success()
        {
            var token = CreateTestToken(hp: 0);
            token.RecordDeathSave(15);
            Assert.Equal(1, token.DeathSaveSuccesses);
            Assert.Equal(0, token.DeathSaveFailures);
        }

        [Fact]
        public void RecordDeathSave_RollBelow10_Failure()
        {
            var token = CreateTestToken(hp: 0);
            token.RecordDeathSave(5);
            Assert.Equal(0, token.DeathSaveSuccesses);
            Assert.Equal(1, token.DeathSaveFailures);
        }

        [Fact]
        public void ThreeSuccesses_IsStabilized()
        {
            var token = CreateTestToken(hp: 0);
            token.RecordDeathSave(15);
            token.RecordDeathSave(15);
            token.RecordDeathSave(15);
            Assert.True(token.IsStabilized);
            Assert.False(token.IsDead);
        }

        [Fact]
        public void ThreeFailures_IsDead()
        {
            var token = CreateTestToken(hp: 0);
            token.RecordDeathSave(5);
            token.RecordDeathSave(5);
            token.RecordDeathSave(5);
            Assert.True(token.IsDead);
            Assert.False(token.IsStabilized);
        }

        [Fact]
        public void ResetDeathSaves_ClearsAll()
        {
            var token = CreateTestToken(hp: 0);
            token.DeathSaveSuccesses = 2;
            token.DeathSaveFailures = 1;
            token.ResetDeathSaves();
            Assert.Equal(0, token.DeathSaveSuccesses);
            Assert.Equal(0, token.DeathSaveFailures);
        }

        [Fact]
        public void TakeDamageWhileUnconscious_Critical_TwoFailures()
        {
            var token = CreateTestToken(hp: 0);
            token.TakeDamageWhileUnconscious(10, wasCritical: true);
            Assert.Equal(2, token.DeathSaveFailures);
        }

        [Fact]
        public void TakeDamageWhileUnconscious_Normal_OneFailure()
        {
            var token = CreateTestToken(hp: 0);
            token.TakeDamageWhileUnconscious(10, wasCritical: false);
            Assert.Equal(1, token.DeathSaveFailures);
        }

        [Fact]
        public void DeathSaveStatusText_Dead_ShowsDead()
        {
            var token = CreateTestToken(hp: 0);
            token.DeathSaveFailures = 3;
            Assert.Contains("DEAD", token.DeathSaveStatusText);
        }

        [Fact]
        public void DeathSaveStatusText_Stabilized_ShowsStabilized()
        {
            var token = CreateTestToken(hp: 0);
            token.DeathSaveSuccesses = 3;
            Assert.Contains("Stabilized", token.DeathSaveStatusText);
        }

        [Fact]
        public void DeathSaveStatusText_PositiveHP_ReturnsNull()
        {
            var token = CreateTestToken(hp: 10);
            Assert.Null(token.DeathSaveStatusText);
        }

        #endregion

        #region Concentration

        [Fact]
        public void SetConcentration_SetsFlags()
        {
            var token = CreateTestToken();
            token.SetConcentration("Fireball");
            Assert.True(token.IsConcentrating);
            Assert.Equal("Fireball", token.ConcentrationSpell);
        }

        [Fact]
        public void BreakConcentration_ClearsFlags()
        {
            var token = CreateTestToken();
            token.SetConcentration("Fireball");
            token.BreakConcentration();
            Assert.False(token.IsConcentrating);
            Assert.Null(token.ConcentrationSpell);
        }

        [Fact]
        public void ConcentrationSaveModifier_CalculatesFromCon()
        {
            var token = new Token { Con = 14 }; // Modifier = (14-10)/2 = 2
            Assert.Equal(2, token.ConcentrationSaveModifier);
        }

        [Fact]
        public void CalculateConcentrationDC_SmallDamage_Returns10()
        {
            Assert.Equal(10, Token.CalculateConcentrationDC(10));
            Assert.Equal(10, Token.CalculateConcentrationDC(15));
        }

        [Fact]
        public void CalculateConcentrationDC_LargeDamage_ReturnsHalf()
        {
            Assert.Equal(15, Token.CalculateConcentrationDC(30));
            Assert.Equal(25, Token.CalculateConcentrationDC(50));
        }

        [Fact]
        public void ConcentrationStatusText_WhenConcentrating_ShowsSpell()
        {
            var token = CreateTestToken();
            token.SetConcentration("Hold Person");
            Assert.Contains("Hold Person", token.ConcentrationStatusText);
        }

        [Fact]
        public void ConcentrationStatusText_WhenNotConcentrating_ReturnsNull()
        {
            var token = CreateTestToken();
            Assert.Null(token.ConcentrationStatusText);
        }

        #endregion

        #region Movement

        [Fact]
        public void TryUseMovement_EnoughRemaining_Succeeds()
        {
            var token = new Token { Speed = "30" };
            token.ResetMovementForNewTurn();
            Assert.True(token.TryUseMovement(3));
            Assert.Equal(3, token.MovementUsedThisTurn);
            Assert.Equal(3, token.MovementRemainingThisTurn);
        }

        [Fact]
        public void TryUseMovement_NotEnoughRemaining_Fails()
        {
            var token = new Token { Speed = "30" };
            token.ResetMovementForNewTurn();
            Assert.False(token.TryUseMovement(10));
        }

        [Fact]
        public void ResetMovementForNewTurn_ClearsUsed()
        {
            var token = new Token { Speed = "30" };
            token.MovementUsedThisTurn = 4;
            token.ResetMovementForNewTurn();
            Assert.Equal(0, token.MovementUsedThisTurn);
        }

        [Fact]
        public void MovementStatusText_ShowsRemaining()
        {
            var token = new Token { Speed = "30" };
            token.ResetMovementForNewTurn();
            Assert.Contains("6 / 6", token.MovementStatusText);
        }

        #endregion

        #region Combat Notes

        [Fact]
        public void AddNote_AddsToList()
        {
            var token = CreateTestToken();
            token.AddNote("Test note");
            Assert.Single(token.CombatNotes);
            Assert.True(token.HasNotes);
        }

        [Fact]
        public void RemoveNote_RemovesByIdString()
        {
            var token = CreateTestToken();
            token.AddNote("Test note");
            var noteId = token.CombatNotes[0].Id;
            token.RemoveNote(noteId);
            Assert.Empty(token.CombatNotes);
        }

        [Fact]
        public void ClearExpiredNotes_RemovesExpired()
        {
            var token = CreateTestToken();
            token.AddNote("Temporary effect", expiresOnRound: 3);
            token.AddNote("Permanent note");
            token.ClearExpiredNotes(currentRound: 3);
            Assert.Single(token.CombatNotes);
            Assert.Equal("Permanent note", token.CombatNotes[0].Text);
        }

        #endregion

        #region Tags

        [Fact]
        public void HasTag_CaseInsensitive()
        {
            var token = CreateTestToken();
            token.Tags.Add("Boss");
            Assert.True(token.HasTag("boss"));
            Assert.True(token.HasTag("BOSS"));
        }

        [Fact]
        public void HasTag_ReturnsFalseForMissing()
        {
            var token = CreateTestToken();
            Assert.False(token.HasTag("unknown"));
        }

        #endregion
    }
}
