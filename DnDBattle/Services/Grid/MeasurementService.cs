using System;
using System.Collections.Generic;
using System.Linq;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Grid
{
    /// <summary>
    /// Manages persistent measurements on the battle map.
    /// Provides O(1) lookup by ID and efficient distance/area calculations.
    /// </summary>
    public sealed class MeasurementService
    {
        private readonly Dictionary<string, Measurement> _measurements = new();

        /// <summary>
        /// Adds a new measurement. Returns the measurement ID.
        /// </summary>
        public string AddMeasurement(Measurement measurement)
        {
            if (!Options.EnableMeasurements) return string.Empty;
            _measurements[measurement.Id] = measurement;
            return measurement.Id;
        }

        /// <summary>
        /// Creates and adds a simple distance measurement between two points.
        /// </summary>
        public string AddDistanceMeasurement(string label, int x1, int y1, int x2, int y2, string colorHex = "#4FC3F7", int feetPerSquare = 5)
        {
            var m = new Measurement
            {
                Label = label,
                Type = MeasurementType.Distance,
                Points = new List<(int, int)> { (x1, y1), (x2, y2) },
                ColorHex = colorHex,
                FeetPerSquare = feetPerSquare,
                Purpose = MeasurementPurpose.Info
            };
            return AddMeasurement(m);
        }

        /// <summary>
        /// Creates and adds a radius measurement from a center point.
        /// </summary>
        public string AddRadiusMeasurement(string label, int centerX, int centerY, int radiusSquares, string colorHex = "#FF9800")
        {
            var m = new Measurement
            {
                Label = label,
                Type = MeasurementType.Radius,
                Points = new List<(int, int)> { (centerX, centerY), (centerX + radiusSquares, centerY) },
                ColorHex = colorHex,
                Purpose = MeasurementPurpose.Spell
            };
            return AddMeasurement(m);
        }

        /// <summary>
        /// Removes a measurement by ID.
        /// </summary>
        public bool RemoveMeasurement(string id) => _measurements.Remove(id);

        /// <summary>
        /// Gets a measurement by ID.
        /// </summary>
        public Measurement? GetMeasurement(string id)
        {
            return _measurements.TryGetValue(id, out var m) ? m : null;
        }

        /// <summary>
        /// Toggles visibility of a measurement.
        /// </summary>
        public void ToggleVisibility(string id)
        {
            if (_measurements.TryGetValue(id, out var m))
                m.IsVisible = !m.IsVisible;
        }

        /// <summary>
        /// Gets all measurements (visible and hidden).
        /// </summary>
        public IReadOnlyCollection<Measurement> GetAllMeasurements() => _measurements.Values;

        /// <summary>
        /// Gets only visible measurements for rendering.
        /// </summary>
        public IEnumerable<Measurement> GetVisibleMeasurements()
        {
            if (!Options.EnableMeasurements) return Enumerable.Empty<Measurement>();
            return _measurements.Values.Where(m => m.IsVisible);
        }

        /// <summary>
        /// Calculates the distance in feet for a measurement.
        /// Uses D&D grid distance: diagonal = 1.5 squares (5e variant rule).
        /// </summary>
        public double CalculateDistanceFeet(Measurement measurement)
        {
            if (measurement.Points.Count < 2) return 0;

            double totalSquares = 0;

            for (int i = 0; i < measurement.Points.Count - 1; i++)
            {
                var (x1, y1) = measurement.Points[i];
                var (x2, y2) = measurement.Points[i + 1];

                int dx = Math.Abs(x2 - x1);
                int dy = Math.Abs(y2 - y1);

                // D&D distance: straight + 0.5 per diagonal
                int straight = Math.Abs(dx - dy);
                int diagonal = Math.Min(dx, dy);
                totalSquares += straight + diagonal * 1.5;
            }

            return totalSquares * measurement.FeetPerSquare;
        }

        /// <summary>
        /// Calculates the area in square feet for an area/polygon measurement.
        /// Uses the Shoelace formula for polygon area.
        /// </summary>
        public double CalculateAreaSqFeet(Measurement measurement)
        {
            if (measurement.Type == MeasurementType.Area && measurement.Points.Count >= 2)
            {
                var (x1, y1) = measurement.Points[0];
                var (x2, y2) = measurement.Points[1];
                int w = Math.Abs(x2 - x1);
                int h = Math.Abs(y2 - y1);
                return w * h * measurement.FeetPerSquare * measurement.FeetPerSquare;
            }

            if (measurement.Type == MeasurementType.Radius && measurement.Points.Count >= 2)
            {
                var (cx, cy) = measurement.Points[0];
                var (rx, ry) = measurement.Points[1];
                double radius = Math.Sqrt(Math.Pow(rx - cx, 2) + Math.Pow(ry - cy, 2));
                double radiusFeet = radius * measurement.FeetPerSquare;
                return Math.PI * radiusFeet * radiusFeet;
            }

            if (measurement.Type == MeasurementType.Polygon && measurement.Points.Count >= 3)
            {
                return Math.Abs(ShoelaceArea(measurement.Points)) * measurement.FeetPerSquare * measurement.FeetPerSquare;
            }

            return 0;
        }

        /// <summary>
        /// Gets the default color for a measurement purpose.
        /// </summary>
        public static string GetPurposeColor(MeasurementPurpose purpose)
        {
            return purpose switch
            {
                MeasurementPurpose.Info => "#4FC3F7",
                MeasurementPurpose.Danger => "#F44336",
                MeasurementPurpose.Safe => "#4CAF50",
                MeasurementPurpose.Spell => "#BA68C8",
                MeasurementPurpose.Movement => "#FFB74D",
                MeasurementPurpose.Custom => "#FFFFFF",
                _ => "#4FC3F7"
            };
        }

        /// <summary>
        /// Clears all measurements.
        /// </summary>
        public void ClearAll() => _measurements.Clear();

        /// <summary>
        /// Gets count of measurements.
        /// </summary>
        public int Count => _measurements.Count;

        // Shoelace formula for polygon area
        private static double ShoelaceArea(List<(int X, int Y)> points)
        {
            double area = 0;
            int n = points.Count;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += points[i].X * points[j].Y;
                area -= points[j].X * points[i].Y;
            }
            return area / 2.0;
        }
    }
}
