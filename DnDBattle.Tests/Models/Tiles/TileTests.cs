using DnDBattle.Models.Tiles;

namespace DnDBattle.Tests.Models.Tiles
{
    public class TileTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var tile = new Tile();
            Assert.NotEqual(Guid.Empty, tile.InstanceId);
            Assert.Equal(0, tile.Rotation);
            Assert.False(tile.FlipHorizontal);
            Assert.False(tile.FlipVertical);
            Assert.Null(tile.ZIndex);
            Assert.Empty(tile.Metadata);
            Assert.False(tile.HasMetadata);
        }

        [Fact]
        public void HasMetadata_TrueWhenHasItems()
        {
            var tile = new Tile();
            tile.Metadata.Add(new TestTileMetadata());
            Assert.True(tile.HasMetadata);
        }

        [Fact]
        public void HasMetadataType_CorrectlyFilters()
        {
            var tile = new Tile();
            tile.Metadata.Add(new TestTileMetadata(TileMetadataType.Trap));
            Assert.True(tile.HasMetadataType(TileMetadataType.Trap));
            Assert.False(tile.HasMetadataType(TileMetadataType.Hazard));
        }

        [Fact]
        public void GetMetadata_ReturnsCorrectType()
        {
            var tile = new Tile();
            tile.Metadata.Add(new TestTileMetadata(TileMetadataType.Trap));
            tile.Metadata.Add(new TestTileMetadata(TileMetadataType.Secret));
            tile.Metadata.Add(new TestTileMetadata(TileMetadataType.Trap));

            var traps = tile.GetMetadata(TileMetadataType.Trap);
            Assert.Equal(2, traps.Count);
        }

        [Fact]
        public void GetEffectiveZIndex_WithExplicitZIndex_ReturnsExplicit()
        {
            var tile = new Tile { ZIndex = 99 };
            Assert.Equal(99, tile.GetEffectiveZIndex(null));
        }

        [Fact]
        public void GetEffectiveZIndex_NoExplicit_UsesDefinition()
        {
            var tile = new Tile();
            var def = new TileDefinition { Layer = TileLayer.Wall };
            Assert.Equal((int)TileLayer.Wall, tile.GetEffectiveZIndex(def));
        }

        [Fact]
        public void GetEffectiveZIndex_NullDefinition_ReturnsZero()
        {
            var tile = new Tile();
            Assert.Equal(0, tile.GetEffectiveZIndex(null));
        }

        // Helper metadata class for testing
        private class TestTileMetadata : TileMetadata
        {
            private readonly TileMetadataType _type;
            public override TileMetadataType Type => _type;
            public TestTileMetadata(TileMetadataType type = TileMetadataType.None)
            {
                _type = type;
            }
        }
    }

    public class TileLayerTests
    {
        [Theory]
        [InlineData(TileLayer.Floor, 0)]
        [InlineData(TileLayer.Terrain, 10)]
        [InlineData(TileLayer.Wall, 20)]
        [InlineData(TileLayer.Door, 30)]
        [InlineData(TileLayer.Furniture, 40)]
        [InlineData(TileLayer.Props, 50)]
        [InlineData(TileLayer.Effects, 60)]
        [InlineData(TileLayer.Roof, 70)]
        public void GetDefaultZIndex_ReturnsEnumValue(TileLayer layer, int expected)
        {
            Assert.Equal(expected, layer.GetDefaultZIndex());
        }

        [Fact]
        public void GetDisplayName_ReturnsNonEmpty()
        {
            foreach (TileLayer layer in Enum.GetValues(typeof(TileLayer)))
            {
                Assert.NotEmpty(layer.GetDisplayName());
            }
        }

        [Fact]
        public void GetIcon_ReturnsEmoji()
        {
            foreach (TileLayer layer in Enum.GetValues(typeof(TileLayer)))
            {
                Assert.NotEmpty(layer.GetIcon());
            }
        }
    }

    public class TileMetadataTypeTests
    {
        [Theory]
        [InlineData(TileMetadataType.Trap, "Trap")]
        [InlineData(TileMetadataType.Hazard, "Environmental Hazard")]
        [InlineData(TileMetadataType.Secret, "Secret")]
        [InlineData(TileMetadataType.Teleporter, "Teleporter")]
        [InlineData(TileMetadataType.Healing, "Healing Zone")]
        public void GetDisplayName_ReturnsCorrectName(TileMetadataType type, string expected)
        {
            Assert.Equal(expected, type.GetDisplayName());
        }

        [Fact]
        public void GetIcon_ReturnsNonEmpty()
        {
            foreach (TileMetadataType type in Enum.GetValues(typeof(TileMetadataType)))
            {
                if (type == TileMetadataType.None) continue;
                Assert.NotEmpty(type.GetIcon());
            }
        }
    }
}
