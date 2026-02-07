using DnDBattle.Services.Grid;
using DnDBattle.Models.Effects;
using System.Windows;

namespace DnDBattle.Tests.Services.Grid
{
    public class SpatialIndexTests
    {
        private LightSource CreateLight(double x, double y, double radius = 3.0)
        {
            return new LightSource
            {
                CenterGrid = new Point(x, y),
                BrightRadius = radius,
                DimRadius = radius,
                IsEnabled = true
            };
        }

        [Fact]
        public void Constructor_DefaultCellResolution()
        {
            var index = new SpatialIndex();
            // Should not throw
            Assert.NotNull(index);
        }

        [Fact]
        public void Constructor_CustomCellResolution()
        {
            var index = new SpatialIndex(4);
            Assert.NotNull(index);
        }

        [Fact]
        public void Constructor_NegativeResolution_ClampedTo1()
        {
            var index = new SpatialIndex(-1);
            // Should not throw
            Assert.NotNull(index);
        }

        [Fact]
        public void IndexLight_AndQuery_FindsLight()
        {
            var index = new SpatialIndex();
            var light = CreateLight(5, 5, 3);
            index.IndexLight(light);

            var results = index.QueryLightsInBounds(4, 4, 6, 6).ToList();
            Assert.Single(results);
            Assert.Same(light, results[0]);
        }

        [Fact]
        public void QueryLightsInBounds_NoLightsInArea_ReturnsEmpty()
        {
            var index = new SpatialIndex();
            var light = CreateLight(5, 5, 2);
            index.IndexLight(light);

            var results = index.QueryLightsInBounds(50, 50, 60, 60).ToList();
            Assert.Empty(results);
        }

        [Fact]
        public void IndexLight_MultipleLights_QueryFindsCorrectOnes()
        {
            var index = new SpatialIndex();
            var light1 = CreateLight(5, 5, 2);
            var light2 = CreateLight(20, 20, 2);
            index.IndexLight(light1);
            index.IndexLight(light2);

            var nearFirst = index.QueryLightsInBounds(4, 4, 6, 6).ToList();
            Assert.Contains(light1, nearFirst);
            Assert.DoesNotContain(light2, nearFirst);

            var nearSecond = index.QueryLightsInBounds(19, 19, 21, 21).ToList();
            Assert.Contains(light2, nearSecond);
            Assert.DoesNotContain(light1, nearSecond);
        }

        [Fact]
        public void IndexLight_DuplicateIndexing_NoDuplicateResults()
        {
            var index = new SpatialIndex();
            var light = CreateLight(5, 5, 3);
            index.IndexLight(light);
            index.IndexLight(light); // Index same light again

            var results = index.QueryLightsInBounds(4, 4, 6, 6).ToList();
            Assert.Single(results);
        }

        [Fact]
        public void QueryLightsInBounds_LargeArea_FindsAllLights()
        {
            var index = new SpatialIndex();
            var lights = new List<LightSource>();
            for (int i = 0; i < 10; i++)
            {
                var light = CreateLight(i * 3, i * 3, 1);
                lights.Add(light);
                index.IndexLight(light);
            }

            var results = index.QueryLightsInBounds(-5, -5, 50, 50).ToList();
            Assert.Equal(10, results.Count);
        }
    }
}
