using DnDBattle.Models.Combat;

namespace DnDBattle.Tests.Models.Combat
{
    public class DamageTypeTests
    {
        [Theory]
        [InlineData(DamageType.Fire, "Fire")]
        [InlineData(DamageType.Cold, "Cold")]
        [InlineData(DamageType.Lightning, "Lightning")]
        [InlineData(DamageType.Bludgeoning, "Bludgeoning")]
        [InlineData(DamageType.Piercing, "Piercing")]
        [InlineData(DamageType.Slashing, "Slashing")]
        [InlineData(DamageType.Thunder, "Thunder")]
        [InlineData(DamageType.Acid, "Acid")]
        [InlineData(DamageType.Poison, "Poison")]
        [InlineData(DamageType.Necrotic, "Necrotic")]
        [InlineData(DamageType.Radiant, "Radiant")]
        [InlineData(DamageType.Force, "Force")]
        [InlineData(DamageType.Psychic, "Psychic")]
        public void GetDisplayName_ReturnsCorrectName(DamageType type, string expected)
        {
            Assert.Equal(expected, type.GetDisplayName());
        }

        [Theory]
        [InlineData(DamageType.Fire, "🔥")]
        [InlineData(DamageType.Cold, "❄️")]
        [InlineData(DamageType.Lightning, "⚡")]
        [InlineData(DamageType.Psychic, "🧠")]
        public void GetIcon_ReturnsCorrectEmoji(DamageType type, string expected)
        {
            Assert.Equal(expected, type.GetIcon());
        }

        [Fact]
        public void GetColor_ReturnsNonDefaultColorForEachType()
        {
            var types = DamageTypeExtensions.GetAllDamageTypes();
            foreach (var type in types)
            {
                var color = type.GetColor();
                Assert.True(color.R > 0 || color.G > 0 || color.B > 0);
            }
        }

        [Theory]
        [InlineData("fire damage", DamageType.Fire)]
        [InlineData("cold damage", DamageType.Cold)]
        [InlineData("lightning", DamageType.Lightning)]
        [InlineData("thunder", DamageType.Thunder)]
        [InlineData("acid", DamageType.Acid)]
        [InlineData("poison", DamageType.Poison)]
        [InlineData("necrotic", DamageType.Necrotic)]
        [InlineData("radiant", DamageType.Radiant)]
        [InlineData("force", DamageType.Force)]
        [InlineData("psychic", DamageType.Psychic)]
        [InlineData("bludgeoning", DamageType.Bludgeoning)]
        [InlineData("piercing", DamageType.Piercing)]
        [InlineData("slashing", DamageType.Slashing)]
        public void ParseFromString_ParsesCorrectTypes(string input, DamageType expected)
        {
            var result = DamageTypeExtensions.ParseFromString(input);
            Assert.True(result.HasFlag(expected));
        }

        [Fact]
        public void ParseFromString_EmptyString_ReturnsNone()
        {
            Assert.Equal(DamageType.None, DamageTypeExtensions.ParseFromString(""));
            Assert.Equal(DamageType.None, DamageTypeExtensions.ParseFromString(null!));
        }

        [Fact]
        public void ParseFromString_MultipleTypes_ReturnsFlags()
        {
            var result = DamageTypeExtensions.ParseFromString("fire and cold damage");
            Assert.True(result.HasFlag(DamageType.Fire));
            Assert.True(result.HasFlag(DamageType.Cold));
        }

        [Fact]
        public void GetIndividualTypes_SplitsFlags()
        {
            var combined = DamageType.Fire | DamageType.Cold;
            var individual = combined.GetIndividualTypes().ToList();
            Assert.Contains(DamageType.Fire, individual);
            Assert.Contains(DamageType.Cold, individual);
            Assert.Equal(2, individual.Count);
        }

        [Fact]
        public void GetIndividualTypes_ExcludesNoneAndPhysical()
        {
            var physical = DamageType.Physical;
            var individual = physical.GetIndividualTypes().ToList();
            Assert.DoesNotContain(DamageType.None, individual);
            Assert.DoesNotContain(DamageType.Physical, individual);
            // Physical should expand to Bludgeoning, Piercing, Slashing
            Assert.Contains(DamageType.Bludgeoning, individual);
            Assert.Contains(DamageType.Piercing, individual);
            Assert.Contains(DamageType.Slashing, individual);
        }

        [Fact]
        public void GetAllDamageTypes_Returns13Types()
        {
            var all = DamageTypeExtensions.GetAllDamageTypes();
            Assert.Equal(13, all.Count);
            Assert.DoesNotContain(DamageType.None, all);
            Assert.DoesNotContain(DamageType.Physical, all);
        }

        [Fact]
        public void Physical_IsCombinationOfBPSTypes()
        {
            Assert.True(DamageType.Physical.HasFlag(DamageType.Bludgeoning));
            Assert.True(DamageType.Physical.HasFlag(DamageType.Piercing));
            Assert.True(DamageType.Physical.HasFlag(DamageType.Slashing));
        }

        [Fact]
        public void DamageType_FlagsEnum_AllowsCombination()
        {
            var combined = DamageType.Fire | DamageType.Cold;
            Assert.True(combined.HasFlag(DamageType.Fire));
            Assert.True(combined.HasFlag(DamageType.Cold));
            Assert.False(combined.HasFlag(DamageType.Lightning));
        }
    }
}
