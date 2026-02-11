using DnDBattle.Models.Effects;
using System.Windows;

namespace DnDBattle.Tests.Models.Effects
{
    public class LightSourceTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var light = new LightSource();
            Assert.Equal(4, light.BrightRadius);
            Assert.Equal(8, light.DimRadius);
            Assert.Equal(8, light.RadiusSquares);
            Assert.Equal(1.0, light.Intensity);
            Assert.True(light.IsEnabled);
            Assert.Equal(LightType.Point, light.Type);
            Assert.Equal(60, light.ConeWidth);
        }

        [Fact]
        public void RadiusSquares_MaxOfBrightAndDim()
        {
            var light = new LightSource { BrightRadius = 3, DimRadius = 6 };
            Assert.Equal(6, light.RadiusSquares);

            light.BrightRadius = 10;
            Assert.Equal(10, light.RadiusSquares);
        }

        [Fact]
        public void IsPointInCone_PointLight_AlwaysTrue()
        {
            var light = new LightSource { Type = LightType.Point, CenterGrid = new Point(5, 5) };
            Assert.True(light.IsPointInCone(new Point(10, 10)));
            Assert.True(light.IsPointInCone(new Point(0, 0)));
        }

        [Fact]
        public void IsPointInCone_DirectionalLight_InCone_True()
        {
            var light = new LightSource
            {
                Type = LightType.Directional,
                CenterGrid = new Point(5, 5),
                Direction = 0, // Facing right
                ConeWidth = 90
            };
            // Point directly to the right
            Assert.True(light.IsPointInCone(new Point(10, 5)));
        }

        [Fact]
        public void IsPointInCone_DirectionalLight_OutOfCone_False()
        {
            var light = new LightSource
            {
                Type = LightType.Directional,
                CenterGrid = new Point(5, 5),
                Direction = 0, // Facing right
                ConeWidth = 30 // Narrow cone
            };
            // Point directly behind (to the left)
            Assert.False(light.IsPointInCone(new Point(0, 5)));
        }

        [Fact]
        public void IsPointInCone_DirectionalLight_OnEdge()
        {
            var light = new LightSource
            {
                Type = LightType.Directional,
                CenterGrid = new Point(5, 5),
                Direction = 0,
                ConeWidth = 90
            };
            // Point at 45 degrees from center axis (within 90-degree cone)
            Assert.True(light.IsPointInCone(new Point(10, 0)));
        }

        [Fact]
        public void LightType_Values()
        {
            Assert.Equal(0, (int)LightType.Point);
            Assert.Equal(1, (int)LightType.Directional);
            Assert.Equal(2, (int)LightType.Ambient);
        }
    }
}
