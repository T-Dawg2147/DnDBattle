using DnDBattle.Models.Combat;

namespace DnDBattle.Tests.Models.Combat
{
    public class SavingThrowResultTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var result = new SavingThrowResult();
            Assert.Null(result.Target);
            Assert.Equal(0, result.DC);
            Assert.Equal(0, result.D20Roll);
            Assert.Equal(0, result.Modifier);
            Assert.Equal(0, result.Total);
            Assert.False(result.Success);
            Assert.False(result.IsNaturalOne);
            Assert.False(result.IsNaturalTwenty);
            Assert.False(result.UsedLegendaryResistance);
            Assert.False(result.AutoFailed);
        }

        [Fact]
        public void ToString_AutoFail_ShowsAutoFail()
        {
            var token = new DnDBattle.Models.Creatures.Token { Name = "Goblin" };
            var result = new SavingThrowResult
            {
                Target = token,
                Ability = Ability.Dexterity,
                AutoFailed = true
            };
            var text = result.ToString();
            Assert.Contains("Auto-FAIL", text);
            Assert.Contains("Goblin", text);
            Assert.Contains("Dexterity", text);
        }

        [Fact]
        public void ToString_Success_ShowsSuccess()
        {
            var token = new DnDBattle.Models.Creatures.Token { Name = "Fighter" };
            var result = new SavingThrowResult
            {
                Target = token,
                D20Roll = 15,
                Modifier = 3,
                Total = 18,
                DC = 15,
                Success = true
            };
            var text = result.ToString();
            Assert.Contains("SUCCESS", text);
            Assert.Contains("15", text);
        }

        [Fact]
        public void ToString_Failure_ShowsFail()
        {
            var token = new DnDBattle.Models.Creatures.Token { Name = "Wizard" };
            var result = new SavingThrowResult
            {
                Target = token,
                D20Roll = 5,
                Modifier = 1,
                Total = 6,
                DC = 15,
                Success = false
            };
            var text = result.ToString();
            Assert.Contains("FAIL", text);
        }

        [Fact]
        public void ToString_NaturalTwenty_ShowsNat20()
        {
            var token = new DnDBattle.Models.Creatures.Token { Name = "Rogue" };
            var result = new SavingThrowResult
            {
                Target = token,
                D20Roll = 20,
                IsNaturalTwenty = true,
                Success = true,
                Total = 25,
                DC = 15
            };
            var text = result.ToString();
            Assert.Contains("Nat 20", text);
        }

        [Fact]
        public void ToString_NaturalOne_ShowsNat1()
        {
            var token = new DnDBattle.Models.Creatures.Token { Name = "Cleric" };
            var result = new SavingThrowResult
            {
                Target = token,
                D20Roll = 1,
                IsNaturalOne = true,
                Success = false,
                Total = 4,
                DC = 15
            };
            var text = result.ToString();
            Assert.Contains("Nat 1", text);
        }

        [Fact]
        public void ToString_LegendaryResistance_ShowsLegendary()
        {
            var token = new DnDBattle.Models.Creatures.Token { Name = "Dragon" };
            var result = new SavingThrowResult
            {
                Target = token,
                Success = true,
                UsedLegendaryResistance = true,
                Total = 5,
                DC = 20
            };
            var text = result.ToString();
            Assert.Contains("Legendary Resistance", text);
        }

        [Fact]
        public void ToString_NullTarget_UsesTargetDefault()
        {
            var result = new SavingThrowResult
            {
                D20Roll = 10,
                Modifier = 2,
                Total = 12,
                DC = 15,
                Success = false
            };
            var text = result.ToString();
            Assert.Contains("Target", text);
        }
    }
}
