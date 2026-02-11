using DnDBattle.Models.Tiles;

namespace DnDBattle.Tests.Models.Tiles
{
    public class TileMapTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var map = new TileMap();
            Assert.Equal("Untitled Map", map.Name);
            Assert.Equal(50, map.Width);
            Assert.Equal(50, map.Height);
            Assert.Equal(48.0, map.CellSize);
            Assert.True(map.ShowGrid);
            Assert.Equal(5, map.FeetPerSquare);
            Assert.NotEqual(Guid.Empty, map.Id);
            Assert.Empty(map.PlacedTiles);
            Assert.Empty(map.Notes);
        }

        [Fact]
        public void AddTile_WithinBounds_AddsTile()
        {
            var map = new TileMap { Width = 10, Height = 10 };
            var tile = new Tile { GridX = 5, GridY = 5 };
            map.AddTile(tile);
            Assert.Single(map.PlacedTiles);
        }

        [Fact]
        public void AddTile_OutOfBounds_Rejected()
        {
            var map = new TileMap { Width = 10, Height = 10 };
            map.AddTile(new Tile { GridX = -1, GridY = 0 });
            map.AddTile(new Tile { GridX = 0, GridY = -1 });
            map.AddTile(new Tile { GridX = 10, GridY = 0 });
            map.AddTile(new Tile { GridX = 0, GridY = 10 });
            Assert.Empty(map.PlacedTiles);
        }

        [Fact]
        public void RemoveTile_RemovesTile()
        {
            var map = new TileMap();
            var tile = new Tile { GridX = 0, GridY = 0 };
            map.AddTile(tile);
            map.RemoveTile(tile);
            Assert.Empty(map.PlacedTiles);
        }

        [Fact]
        public void GetTilesAt_ReturnsCorrectTiles()
        {
            var map = new TileMap();
            map.AddTile(new Tile { GridX = 3, GridY = 3 });
            map.AddTile(new Tile { GridX = 3, GridY = 3 });
            map.AddTile(new Tile { GridX = 5, GridY = 5 });

            var tilesAt33 = map.GetTilesAt(3, 3).ToList();
            Assert.Equal(2, tilesAt33.Count);
        }

        [Fact]
        public void ClearTilesAt_RemovesAllAtPosition()
        {
            var map = new TileMap();
            map.AddTile(new Tile { GridX = 3, GridY = 3 });
            map.AddTile(new Tile { GridX = 3, GridY = 3 });
            map.AddTile(new Tile { GridX = 5, GridY = 5 });

            map.ClearTilesAt(3, 3);
            Assert.Single(map.PlacedTiles);
        }

        [Theory]
        [InlineData("30 ft", 6)]   // 30/5 = 6
        [InlineData("60", 12)]      // 60/5 = 12
        [InlineData("", 6)]         // Default
        [InlineData(null, 6)]       // Default
        public void GetSpeedInSquares_CalculatesCorrectly(string? speed, int expected)
        {
            var map = new TileMap { FeetPerSquare = 5 };
            Assert.Equal(expected, map.GetSpeedInSquares(speed!));
        }

        [Fact]
        public void GetSpeedInSquares_CustomFeetPerSquare()
        {
            var map = new TileMap { FeetPerSquare = 10 };
            Assert.Equal(3, map.GetSpeedInSquares("30"));
        }

        [Fact]
        public void ChangeGridScale_RescalesTiles()
        {
            var map = new TileMap { FeetPerSquare = 5, Width = 50, Height = 50 };
            var tile = new Tile { GridX = 10, GridY = 10 };
            map.AddTile(tile);

            map.ChangeGridScale(10); // 5ft -> 10ft scale

            Assert.Equal(10, map.FeetPerSquare);
            Assert.Equal(5, tile.GridX);  // 10 * (5/10) = 5
            Assert.Equal(5, tile.GridY);
            Assert.Equal(25, map.Width);
        }

        [Fact]
        public void ChangeGridScale_ZeroOrNegative_Ignored()
        {
            var map = new TileMap { FeetPerSquare = 5, Width = 50 };
            map.ChangeGridScale(0);
            Assert.Equal(5, map.FeetPerSquare);
            Assert.Equal(50, map.Width);
        }

        [Fact]
        public void ChangeGridScale_SameValue_Ignored()
        {
            var map = new TileMap { FeetPerSquare = 5 };
            var modified = map.ModifiedDate;
            map.ChangeGridScale(5);
            Assert.Equal(5, map.FeetPerSquare);
        }

        [Fact]
        public void AddNote_AddsNote()
        {
            var map = new TileMap();
            var note = new MapNote { Text = "Test" };
            map.AddNote(note);
            Assert.Single(map.Notes);
        }

        [Fact]
        public void AddNote_Null_Ignored()
        {
            var map = new TileMap();
            map.AddNote(null!);
            Assert.Empty(map.Notes);
        }

        [Fact]
        public void RemoveNote_ById_RemovesCorrectNote()
        {
            var map = new TileMap();
            var note = new MapNote { Text = "Remove me" };
            map.AddNote(note);
            Assert.True(map.RemoveNote(note.Id));
            Assert.Empty(map.Notes);
        }

        [Fact]
        public void RemoveNote_NonExistent_ReturnsFalse()
        {
            var map = new TileMap();
            Assert.False(map.RemoveNote(Guid.NewGuid()));
        }

        [Fact]
        public void GridType_Default_IsSquare()
        {
            var map = new TileMap();
            Assert.Equal(GridType.Square, map.GridType);
        }
    }
}
