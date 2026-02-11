using DnDBattle.Models.Environment;
using System.Windows;

namespace DnDBattle.Tests.Models.Environment
{
    public class WallTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var wall = new Wall();
            Assert.Equal(WallType.Solid, wall.WallType);
            Assert.False(wall.IsDoor);
            Assert.False(wall.IsOpen);
            Assert.NotEqual(Guid.Empty, wall.Id);
        }

        #region Blocking Properties

        [Theory]
        [InlineData(WallType.Solid, true)]
        [InlineData(WallType.Window, false)]
        [InlineData(WallType.Halfwall, false)]
        [InlineData(WallType.Invisible, false)]
        public void BlocksLight_CorrectForType(WallType type, bool expected)
        {
            var wall = new Wall { WallType = type };
            Assert.Equal(expected, wall.BlocksLight);
        }

        [Fact]
        public void BlocksLight_Door_DependsOnOpenState()
        {
            var wall = new Wall { WallType = WallType.Door };
            Assert.True(wall.BlocksLight);
            wall.IsOpen = true;
            Assert.False(wall.BlocksLight);
        }

        [Theory]
        [InlineData(WallType.Solid, true)]
        [InlineData(WallType.Window, false)]
        [InlineData(WallType.Halfwall, false)]
        [InlineData(WallType.Invisible, false)]
        public void BlocksSight_CorrectForType(WallType type, bool expected)
        {
            var wall = new Wall { WallType = type };
            Assert.Equal(expected, wall.BlocksSight);
        }

        [Theory]
        [InlineData(WallType.Solid, true)]
        [InlineData(WallType.Window, true)]       // Windows block movement
        [InlineData(WallType.Halfwall, false)]
        [InlineData(WallType.Invisible, true)]    // Invisible walls block movement
        public void BlocksMovement_CorrectForType(WallType type, bool expected)
        {
            var wall = new Wall { WallType = type };
            Assert.Equal(expected, wall.BlocksMovement);
        }

        [Fact]
        public void BlocksMovement_Door_DependsOnOpenState()
        {
            var wall = new Wall { WallType = WallType.Door };
            Assert.True(wall.BlocksMovement);
            wall.IsOpen = true;
            Assert.False(wall.BlocksMovement);
        }

        #endregion

        #region Length

        [Fact]
        public void Length_HorizontalWall()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(5, 0)
            };
            Assert.Equal(5.0, wall.Length);
        }

        [Fact]
        public void Length_VerticalWall()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 3)
            };
            Assert.Equal(3.0, wall.Length);
        }

        [Fact]
        public void Length_DiagonalWall()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(3, 4)
            };
            Assert.Equal(5.0, wall.Length, 5);
        }

        [Fact]
        public void Length_ZeroLength()
        {
            var wall = new Wall
            {
                StartPoint = new Point(3, 3),
                EndPoint = new Point(3, 3)
            };
            Assert.Equal(0, wall.Length);
        }

        #endregion

        #region Distance to Point

        [Fact]
        public void DistanceToPoint_PointOnWall_Zero()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(10, 0)
            };
            double dist = wall.DistanceToPoint(new Point(5, 0));
            Assert.Equal(0, dist, 5);
        }

        [Fact]
        public void DistanceToPoint_PointAboveWall()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(10, 0)
            };
            double dist = wall.DistanceToPoint(new Point(5, 3));
            Assert.Equal(3, dist, 5);
        }

        [Fact]
        public void IsPointNear_ClosePoint_True()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(10, 0)
            };
            Assert.True(wall.IsPointNear(new Point(5, 0.2)));
        }

        [Fact]
        public void IsPointNear_FarPoint_False()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(10, 0)
            };
            Assert.False(wall.IsPointNear(new Point(5, 5)));
        }

        #endregion

        #region Line Intersection

        [Fact]
        public void IntersectsLine_CrossingLines_True()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 5),
                EndPoint = new Point(10, 5)
            };
            bool intersects = wall.IntersectsLine(new Point(5, 0), new Point(5, 10), out Point intersection);
            Assert.True(intersects);
            Assert.Equal(5, intersection.X, 3);
            Assert.Equal(5, intersection.Y, 3);
        }

        [Fact]
        public void IntersectsLine_ParallelLines_False()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(10, 0)
            };
            bool intersects = wall.IntersectsLine(new Point(0, 5), new Point(10, 5), out _);
            Assert.False(intersects);
        }

        [Fact]
        public void IntersectsLine_NonIntersectingSegments_False()
        {
            var wall = new Wall
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(5, 0)
            };
            // Line from (10,0) to (10,10) - doesn't reach the wall segment
            bool intersects = wall.IntersectsLine(new Point(10, 0), new Point(10, 10), out _);
            Assert.False(intersects);
        }

        #endregion
    }
}
