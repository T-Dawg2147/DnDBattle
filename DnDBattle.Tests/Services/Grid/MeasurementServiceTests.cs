using DnDBattle.Services.Grid;
using DnDBattle.Models.Environment;

namespace DnDBattle.Tests.Services.Grid
{
    public class MeasurementServiceTests
    {
        private MeasurementService CreateService()
        {
            Options.EnableMeasurements = true;
            return new MeasurementService();
        }

        [Fact]
        public void AddMeasurement_StoresMeasurement()
        {
            var service = CreateService();
            var measurement = new Measurement { Label = "Test" };
            var id = service.AddMeasurement(measurement);
            Assert.NotEmpty(id);
            Assert.Equal(1, service.Count);
        }

        [Fact]
        public void AddMeasurement_WhenDisabled_ReturnsEmpty()
        {
            Options.EnableMeasurements = false;
            try
            {
                var service = new MeasurementService();
                var measurement = new Measurement { Label = "Test" };
                var id = service.AddMeasurement(measurement);
                Assert.Empty(id);
            }
            finally
            {
                Options.EnableMeasurements = true;
            }
        }

        [Fact]
        public void AddDistanceMeasurement_CreatesCorrectType()
        {
            var service = CreateService();
            var id = service.AddDistanceMeasurement("Test Distance", 0, 0, 5, 5);
            var measurement = service.GetMeasurement(id);
            Assert.NotNull(measurement);
            Assert.Equal(MeasurementType.Distance, measurement.Type);
            Assert.Equal("Test Distance", measurement.Label);
        }

        [Fact]
        public void AddRadiusMeasurement_CreatesCorrectType()
        {
            var service = CreateService();
            var id = service.AddRadiusMeasurement("Fireball", 5, 5, 4);
            var measurement = service.GetMeasurement(id);
            Assert.NotNull(measurement);
            Assert.Equal(MeasurementType.Radius, measurement.Type);
            Assert.Equal(MeasurementPurpose.Spell, measurement.Purpose);
        }

        [Fact]
        public void RemoveMeasurement_RemovesCorrectly()
        {
            var service = CreateService();
            var id = service.AddDistanceMeasurement("Test", 0, 0, 5, 5);
            Assert.True(service.RemoveMeasurement(id));
            Assert.Equal(0, service.Count);
        }

        [Fact]
        public void RemoveMeasurement_NonExistent_ReturnsFalse()
        {
            var service = CreateService();
            Assert.False(service.RemoveMeasurement("nonexistent"));
        }

        [Fact]
        public void GetMeasurement_Exists_ReturnsMeasurement()
        {
            var service = CreateService();
            var m = new Measurement { Label = "Test" };
            service.AddMeasurement(m);
            Assert.NotNull(service.GetMeasurement(m.Id));
        }

        [Fact]
        public void GetMeasurement_NotExists_ReturnsNull()
        {
            var service = CreateService();
            Assert.Null(service.GetMeasurement("nonexistent"));
        }

        [Fact]
        public void ToggleVisibility_TogglesFlag()
        {
            var service = CreateService();
            var m = new Measurement { Label = "Test", IsVisible = true };
            service.AddMeasurement(m);
            service.ToggleVisibility(m.Id);
            Assert.False(service.GetMeasurement(m.Id)!.IsVisible);
            service.ToggleVisibility(m.Id);
            Assert.True(service.GetMeasurement(m.Id)!.IsVisible);
        }

        [Fact]
        public void GetVisibleMeasurements_FiltersHidden()
        {
            var service = CreateService();
            service.AddMeasurement(new Measurement { Label = "Visible", IsVisible = true });
            service.AddMeasurement(new Measurement { Label = "Hidden", IsVisible = false });

            var visible = service.GetVisibleMeasurements().ToList();
            Assert.Single(visible);
            Assert.Equal("Visible", visible[0].Label);
        }

        [Fact]
        public void GetVisibleMeasurements_WhenDisabled_ReturnsEmpty()
        {
            Options.EnableMeasurements = true;
            var service = new MeasurementService();
            service.AddMeasurement(new Measurement { Label = "Test", IsVisible = true });
            Options.EnableMeasurements = false;
            try
            {
                var visible = service.GetVisibleMeasurements().ToList();
                Assert.Empty(visible);
            }
            finally
            {
                Options.EnableMeasurements = true;
            }
        }

        [Fact]
        public void ClearAll_RemovesEverything()
        {
            var service = CreateService();
            service.AddMeasurement(new Measurement());
            service.AddMeasurement(new Measurement());
            service.ClearAll();
            Assert.Equal(0, service.Count);
        }

        #region Distance Calculations

        [Fact]
        public void CalculateDistanceFeet_StraightLine_Correct()
        {
            var service = CreateService();
            var m = new Measurement
            {
                Points = new List<(int, int)> { (0, 0), (6, 0) },
                FeetPerSquare = 5
            };
            Assert.Equal(30, service.CalculateDistanceFeet(m)); // 6 squares * 5 ft
        }

        [Fact]
        public void CalculateDistanceFeet_Diagonal_Uses1_5xCost()
        {
            var service = CreateService();
            var m = new Measurement
            {
                Points = new List<(int, int)> { (0, 0), (3, 3) },
                FeetPerSquare = 5
            };
            // 3 diagonal squares = 3 * 1.5 = 4.5 squares * 5 = 22.5 ft
            Assert.Equal(22.5, service.CalculateDistanceFeet(m));
        }

        [Fact]
        public void CalculateDistanceFeet_TooFewPoints_ReturnsZero()
        {
            var service = CreateService();
            var m = new Measurement
            {
                Points = new List<(int, int)> { (0, 0) }
            };
            Assert.Equal(0, service.CalculateDistanceFeet(m));
        }

        [Fact]
        public void CalculateDistanceFeet_MultiplePoints_SumsSegments()
        {
            var service = CreateService();
            var m = new Measurement
            {
                Points = new List<(int, int)> { (0, 0), (5, 0), (5, 5) },
                FeetPerSquare = 5
            };
            // First segment: 5 squares = 25 ft
            // Second segment: 5 squares = 25 ft
            Assert.Equal(50, service.CalculateDistanceFeet(m));
        }

        #endregion

        #region Area Calculations

        [Fact]
        public void CalculateAreaSqFeet_Rectangle_Correct()
        {
            var service = CreateService();
            var m = new Measurement
            {
                Type = MeasurementType.Area,
                Points = new List<(int, int)> { (0, 0), (4, 3) },
                FeetPerSquare = 5
            };
            // 4*3 squares * 25 sq ft = 300 sq ft
            Assert.Equal(300, service.CalculateAreaSqFeet(m));
        }

        [Fact]
        public void CalculateAreaSqFeet_Radius_Correct()
        {
            var service = CreateService();
            var m = new Measurement
            {
                Type = MeasurementType.Radius,
                Points = new List<(int, int)> { (5, 5), (9, 5) }, // Radius 4 squares
                FeetPerSquare = 5
            };
            // PI * (20ft)^2 = ~1256.6
            double area = service.CalculateAreaSqFeet(m);
            Assert.InRange(area, 1256, 1258);
        }

        [Fact]
        public void CalculateAreaSqFeet_Polygon_UsesShoelace()
        {
            var service = CreateService();
            var m = new Measurement
            {
                Type = MeasurementType.Polygon,
                Points = new List<(int, int)> { (0, 0), (4, 0), (4, 3), (0, 3) }, // 4x3 rectangle
                FeetPerSquare = 5
            };
            // Should be 4*3 = 12 square units * 25 = 300
            double area = service.CalculateAreaSqFeet(m);
            Assert.Equal(300, area);
        }

        #endregion

        #region Purpose Colors

        [Theory]
        [InlineData(MeasurementPurpose.Info, "#4FC3F7")]
        [InlineData(MeasurementPurpose.Danger, "#F44336")]
        [InlineData(MeasurementPurpose.Safe, "#4CAF50")]
        [InlineData(MeasurementPurpose.Spell, "#BA68C8")]
        [InlineData(MeasurementPurpose.Movement, "#FFB74D")]
        [InlineData(MeasurementPurpose.Custom, "#FFFFFF")]
        public void GetPurposeColor_ReturnsCorrectColor(MeasurementPurpose purpose, string expected)
        {
            Assert.Equal(expected, MeasurementService.GetPurposeColor(purpose));
        }

        #endregion
    }
}
