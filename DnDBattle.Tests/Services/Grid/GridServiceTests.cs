using DnDBattle.Services.Grid;

namespace DnDBattle.Tests.Services.Grid
{
    public class GridServiceTests
    {
        [Theory]
        [InlineData(0, 48.0, 0)]
        [InlineData(1, 48.0, 48.0)]
        [InlineData(5, 48.0, 240.0)]
        [InlineData(3, 32.0, 96.0)]
        public void GridToWorldX_ConvertsCorrectly(int gridX, double cellSize, double expected)
        {
            Assert.Equal(expected, GridService.GridToWorldX(gridX, cellSize));
        }

        [Theory]
        [InlineData(0, 48.0, 0)]
        [InlineData(1, 48.0, 48.0)]
        [InlineData(5, 48.0, 240.0)]
        public void GridToWorldY_ConvertsCorrectly(int gridY, double cellSize, double expected)
        {
            Assert.Equal(expected, GridService.GridToWorldY(gridY, cellSize));
        }

        [Theory]
        [InlineData(0, 48.0, 0)]
        [InlineData(48, 48.0, 1)]
        [InlineData(240, 48.0, 5)]
        [InlineData(96, 32.0, 3)]
        public void WorldToGridX_ConvertsCorrectly(int worldX, double cellSize, double expected)
        {
            Assert.Equal(expected, GridService.WorldToGridX(worldX, cellSize));
        }

        [Theory]
        [InlineData(0, 48.0, 0)]
        [InlineData(48, 48.0, 1)]
        [InlineData(240, 48.0, 5)]
        public void WorldToGridY_ConvertsCorrectly(int worldY, double cellSize, double expected)
        {
            Assert.Equal(expected, GridService.WorldToGridY(worldY, cellSize));
        }

        [Fact]
        public void RoundTrip_GridToWorldToGrid()
        {
            double cellSize = 48.0;
            for (int i = 0; i < 20; i++)
            {
                double worldX = GridService.GridToWorldX(i, cellSize);
                double backToGrid = GridService.WorldToGridX((int)worldX, cellSize);
                Assert.Equal(i, backToGrid);
            }
        }
    }
}
