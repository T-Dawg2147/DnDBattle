using DnDBattle.Models.Combat;
using Action = DnDBattle.Models.Combat.Action;

namespace DnDBattle.Tests.Models.Combat
{
    public class ActionTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var action = new Action();
            Assert.Equal(string.Empty, action.Name);
            Assert.Equal(string.Empty, action.Type);
            Assert.Null(action.Cost);
            Assert.Null(action.AttackBonus);
            Assert.Equal(string.Empty, action.DamageExpression);
            Assert.Equal(string.Empty, action.Range);
            Assert.Null(action.Description);
            Assert.Equal(DamageType.None, action.DamageType);
        }

        [Fact]
        public void ToString_ReturnsName()
        {
            var action = new Action { Name = "Longsword" };
            Assert.Equal("Longsword", action.ToString());
        }

        [Fact]
        public void ToString_NullName_ReturnsUnknownAction()
        {
            var action = new Action { Name = null! };
            Assert.Equal("Unknown Action", action.ToString());
        }

        [Fact]
        public void GetEffectiveDamageType_ExplicitType_ReturnsSet()
        {
            var action = new Action { DamageType = DamageType.Fire };
            Assert.Equal(DamageType.Fire, action.GetEffectiveDamageType());
        }

        [Theory]
        [InlineData("Longsword", "Melee attack with a longsword", DamageType.Slashing)]
        [InlineData("Dagger", "Piercing damage with a dagger", DamageType.Piercing)]
        [InlineData("Fire Bolt", "Ranged fire spell", DamageType.Fire)]
        [InlineData("Ice Storm", "cold damage in an area", DamageType.Cold)]
        [InlineData("Magic Missile", "force damage via magic missile", DamageType.Force)]
        [InlineData("Slam", "Slam attack", DamageType.Bludgeoning)]
        [InlineData("Bite", "Bite attack", DamageType.Slashing)]
        [InlineData("Poison Spray", "poison damage", DamageType.Poison)]
        [InlineData("Chill Touch", "necrotic damage", DamageType.Necrotic)]
        [InlineData("Sacred Flame", "radiant damage", DamageType.Radiant)]
        [InlineData("Vicious Mockery", "psychic damage", DamageType.Psychic)]
        public void GetEffectiveDamageType_DetectsFromDescription(string name, string description, DamageType expected)
        {
            var action = new Action { Name = name, Description = description };
            Assert.Equal(expected, action.GetEffectiveDamageType());
        }

        [Fact]
        public void GetEffectiveDamageType_LightningBolt_DetectsLightning()
        {
            var action = new Action { Name = "Lightning Bolt", Description = "lightning damage" };
            Assert.Equal(DamageType.Lightning, action.GetEffectiveDamageType());
        }

        [Fact]
        public void GetEffectiveDamageType_ThunderWave_DetectsThunder()
        {
            var action = new Action { Name = "Thunder Wave", Description = "thunder damage" };
            Assert.Equal(DamageType.Thunder, action.GetEffectiveDamageType());
        }

        [Fact]
        public void GetEffectiveDamageType_AcidSplash_DetectsAcid()
        {
            var action = new Action { Name = "Acid Splash", Description = "acid damage" };
            Assert.Equal(DamageType.Acid, action.GetEffectiveDamageType());
        }

        [Fact]
        public void ActionType_Values_AreCorrect()
        {
            Assert.Equal(0, (int)ActionType.Action);
            Assert.Equal(1, (int)ActionType.BonusAction);
            Assert.Equal(2, (int)ActionType.Reaction);
            Assert.Equal(3, (int)ActionType.LegendaryAction);
        }
    }
}
