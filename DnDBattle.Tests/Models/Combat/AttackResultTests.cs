using DnDBattle.Models.Combat;

namespace DnDBattle.Tests.Models.Combat
{
    public class AttackResultTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var result = new AttackResult();
            Assert.False(result.Hit);
            Assert.False(result.IsCriticalHit);
            Assert.False(result.IsCriticalFumble);
            Assert.Equal(0, result.D20Roll);
            Assert.Equal(0, result.TotalAttack);
            Assert.Equal(0, result.TargetAC);
            Assert.Equal(0, result.DamageRoll);
            Assert.Equal(0, result.ActualDamage);
            Assert.Equal(AttackMode.Normal, result.Mode);
            Assert.Equal(CoverLevel.None, result.Cover);
        }

        [Fact]
        public void ToString_Hit_ContainsAttackerAndDefenderNames()
        {
            var attacker = new DnDBattle.Models.Creatures.Token { Name = "Fighter" };
            var defender = new DnDBattle.Models.Creatures.Token { Name = "Goblin", MaxHP = 20, HP = 12 };
            var result = new AttackResult
            {
                Attacker = attacker,
                Defender = defender,
                AttackBonus = 5,
                D20Roll = 15,
                TotalAttack = 20,
                TargetAC = 13,
                Hit = true,
                DamageRoll = 8,
                ActualDamage = 8
            };
            var text = result.ToString();
            Assert.Contains("Fighter", text);
            Assert.Contains("Goblin", text);
            Assert.Contains("HIT", text);
        }

        [Fact]
        public void ToString_Miss_ContainsMiss()
        {
            var result = new AttackResult
            {
                D20Roll = 5,
                TotalAttack = 10,
                TargetAC = 15,
                Hit = false
            };
            var text = result.ToString();
            Assert.Contains("MISS", text);
        }

        [Fact]
        public void ToString_CriticalHit_ContainsCritical()
        {
            var result = new AttackResult
            {
                D20Roll = 20,
                Hit = true,
                IsCriticalHit = true,
                DamageRoll = 16,
                ActualDamage = 16
            };
            var text = result.ToString();
            Assert.Contains("CRITICAL HIT", text);
        }

        [Fact]
        public void ToString_Fumble_ContainsFumble()
        {
            var result = new AttackResult
            {
                D20Roll = 1,
                Hit = false,
                IsCriticalFumble = true
            };
            var text = result.ToString();
            Assert.Contains("FUMBLE", text);
        }

        [Fact]
        public void ToString_WithCover_ShowsCoverBonus()
        {
            var result = new AttackResult
            {
                Hit = false,
                Cover = CoverLevel.Half,
                TargetAC = 15
            };
            var text = result.ToString();
            Assert.Contains("cover +2", text);
        }
    }
}

