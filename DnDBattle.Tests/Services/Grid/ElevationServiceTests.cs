using DnDBattle.Services.Grid;

namespace DnDBattle.Tests.Services.Grid
{
    public class ElevationServiceTests
    {
        private ElevationService CreateService(int width = 20, int height = 20)
        {
            var service = new ElevationService();
            service.Initialize(width, height);
            return service;
        }

        [Fact]
        public void Initialize_AllTerrainAtZero()
        {
            var service = CreateService();
            Assert.Equal(0, service.GetTerrainElevation(0, 0));
            Assert.Equal(0, service.GetTerrainElevation(10, 10));
        }

        [Fact]
        public void SetTerrainElevation_SetsCorrectly()
        {
            Options.EnableElevationSystem = true;
            try
            {
                var service = CreateService();
                service.SetTerrainElevation(5, 5, 30);
                Assert.Equal(30, service.GetTerrainElevation(5, 5));
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Fact]
        public void SetTerrainElevation_OutOfBounds_Ignored()
        {
            var service = CreateService(10, 10);
            service.SetTerrainElevation(-1, 0, 10);
            service.SetTerrainElevation(0, -1, 10);
            service.SetTerrainElevation(10, 0, 10);
            service.SetTerrainElevation(0, 10, 10);
            // Should not throw
        }

        [Fact]
        public void SetTerrainElevation_ClampedToMax()
        {
            Options.EnableElevationSystem = true;
            try
            {
                var service = CreateService();
                service.SetTerrainElevation(5, 5, 500);
                Assert.Equal(300, service.GetTerrainElevation(5, 5));
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Fact]
        public void RaiseTerrain_IncrementsByStep()
        {
            Options.EnableElevationSystem = true;
            try
            {
                var service = CreateService();
                service.RaiseTerrain(5, 5);
                Assert.Equal(10, service.GetTerrainElevation(5, 5));
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Fact]
        public void LowerTerrain_DecrementsByStep()
        {
            Options.EnableElevationSystem = true;
            try
            {
                var service = CreateService();
                service.SetTerrainElevation(5, 5, 30);
                service.LowerTerrain(5, 5);
                Assert.Equal(20, service.GetTerrainElevation(5, 5));
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Fact]
        public void LowerTerrain_AtZero_StaysAtZero()
        {
            var service = CreateService();
            service.LowerTerrain(5, 5);
            // GetTerrainElevation returns 0 when elevation system disabled, but terrain should not go negative
            Options.EnableElevationSystem = true;
            try
            {
                Assert.Equal(0, service.GetTerrainElevation(5, 5));
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Fact]
        public void SetTokenElevation_AndGet_Works()
        {
            var service = CreateService();
            service.SetTokenElevation("token1", 20);
            Assert.Equal(20, service.GetTokenElevation("token1"));
        }

        [Fact]
        public void GetTokenElevation_Unknown_ReturnsZero()
        {
            var service = CreateService();
            Assert.Equal(0, service.GetTokenElevation("unknown"));
        }

        [Fact]
        public void GetTotalHeight_CombinesTerrainAndToken()
        {
            Options.EnableElevationSystem = true;
            try
            {
                var service = CreateService();
                service.SetTerrainElevation(5, 5, 20);
                service.SetTokenElevation("token1", 10);
                Assert.Equal(30, service.GetTotalHeight("token1", 5, 5));
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(5, 0)]
        [InlineData(10, 1)]
        [InlineData(30, 3)]
        [InlineData(200, 20)]
        [InlineData(500, 20)] // Capped at 20d6
        public void CalculateFallingDamageDice_CorrectDice(int heightFeet, int expectedDice)
        {
            var service = CreateService();
            Assert.Equal(expectedDice, service.CalculateFallingDamageDice(heightFeet));
        }

        [Fact]
        public void CalculateFallingDamageDice_NegativeHeight_ReturnsZero()
        {
            var service = CreateService();
            Assert.Equal(0, service.CalculateFallingDamageDice(-10));
        }

        [Fact]
        public void Calculate3DDistance_SamePosition_ReturnsZero()
        {
            var service = CreateService();
            Assert.Equal(0, service.Calculate3DDistance(5, 5, 0, 5, 5, 0));
        }

        [Fact]
        public void Calculate3DDistance_FlatDistance_EqualsPlaneDistance()
        {
            var service = CreateService();
            double distance = service.Calculate3DDistance(0, 0, 0, 3, 4, 0);
            Assert.Equal(5.0, distance, 5);
        }

        [Fact]
        public void Calculate3DDistance_WithElevation_IncludesVertical()
        {
            var service = CreateService();
            // 3-4-5 triangle but with elevation
            double flat = service.Calculate3DDistance(0, 0, 0, 3, 4, 0);
            double elevated = service.Calculate3DDistance(0, 0, 0, 3, 4, 25);
            Assert.True(elevated > flat);
        }

        [Fact]
        public void RemoveToken_RemovesElevation()
        {
            var service = CreateService();
            service.SetTokenElevation("token1", 20);
            service.RemoveToken("token1");
            Assert.Equal(0, service.GetTokenElevation("token1"));
        }

        [Fact]
        public void Reset_ClearsEverything()
        {
            Options.EnableElevationSystem = true;
            try
            {
                var service = CreateService();
                service.SetTerrainElevation(5, 5, 30);
                service.SetTokenElevation("token1", 20);
                service.Reset();
                Assert.Equal(0, service.GetTerrainElevation(5, 5));
                Assert.Equal(0, service.GetTokenElevation("token1"));
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Fact]
        public void GetDistinctElevationLevels_ReturnsUniqueValues()
        {
            Options.EnableElevationSystem = true;
            try
            {
                var service = CreateService(5, 5);
                service.SetTerrainElevation(0, 0, 10);
                service.SetTerrainElevation(1, 1, 20);
                service.SetTerrainElevation(2, 2, 10); // Duplicate elevation

                var levels = service.GetDistinctElevationLevels();
                Assert.Contains(0, levels);
                Assert.Contains(10, levels);
                Assert.Contains(20, levels);
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Fact]
        public void GetDistinctElevationLevels_Uninitialized_ReturnsEmpty()
        {
            var service = new ElevationService();
            var levels = service.GetDistinctElevationLevels();
            Assert.Empty(levels);
        }

        [Fact]
        public void HasElevationLineOfSight_ShortRange_AlwaysTrue()
        {
            Options.EnableElevationSystem = true;
            try
            {
                var service = CreateService();
                Assert.True(service.HasElevationLineOfSight(0, 50, 1));
            }
            finally
            {
                Options.EnableElevationSystem = false;
            }
        }

        [Fact]
        public void GetTerrainElevation_ElevationSystemDisabled_ReturnsZero()
        {
            Options.EnableElevationSystem = false;
            var service = CreateService();
            service.SetTerrainElevation(5, 5, 30);
            Assert.Equal(0, service.GetTerrainElevation(5, 5));
        }
    }
}
