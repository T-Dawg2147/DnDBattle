using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Services
{
    /// <summary>
    /// Service for managing and rendering area effects
    /// </summary>
    public class AreaEffectService
    {
        private readonly List<AreaEffect> _activeEffects = new List<AreaEffect>();

        public event System.Action EffectsChanged;

        public IReadOnlyList<AreaEffect> ActiveEffects => _activeEffects.AsReadOnly();

        /// <summary>
        /// Adds an area effect to the battlefield
        /// </summary>
        public void AddEffect(AreaEffect effect)
        {
            effect.IsPreview = false;
            _activeEffects.Add(effect);
            EffectsChanged?.Invoke();
        }

        /// <summary>
        /// Removes an area effect from the battlefield
        /// </summary>
        public void RemoveEffect(AreaEffect effect)
        {
            _activeEffects.Remove(effect);
            EffectsChanged?.Invoke();
        }

        /// <summary>
        /// Removes an effect by ID
        /// </summary>
        public void RemoveEffect(Guid effectId)
        {
            _activeEffects.RemoveAll(e => e.Id == effectId);
            EffectsChanged?.Invoke();
        }

        /// <summary>
        /// Clears all area effects
        /// </summary>
        public void ClearAllEffects()
        {
            _activeEffects.Clear();
            EffectsChanged?.Invoke();
        }

        /// <summary>
        /// Gets the geometry for rendering an area effect
        /// </summary>
        public static Geometry GetEffectGeometry(AreaEffect effect, double gridCellSize)
        {
            double originX = effect.Origin.X * gridCellSize;
            double originY = effect.Origin.Y * gridCellSize;
            double sizeInPixels = effect.SizeInSquares * gridCellSize;

            switch (effect.Shape)
            {
                case AreaEffectShape.Sphere:
                case AreaEffectShape.Cylinder:
                    return CreateCircleGeometry(originX, originY, sizeInPixels);

                case AreaEffectShape.Cube:
                case AreaEffectShape.Square:
                    return CreateSquareGeometry(originX, originY, sizeInPixels);

                case AreaEffectShape.Cone:
                    return CreateConeGeometry(originX, originY, sizeInPixels, effect.DirectionAngle);

                case AreaEffectShape.Line:
                    double widthInPixels = effect.WidthInSquares * gridCellSize;
                    return CreateLineGeometry(originX, originY, sizeInPixels, widthInPixels, effect.DirectionAngle);

                default:
                    return CreateCircleGeometry(originX, originY, sizeInPixels);
            }
        }

        private static Geometry CreateCircleGeometry(double centerX, double centerY, double radius)
        {
            return new EllipseGeometry(new Point(centerX, centerY), radius, radius);
        }

        private static Geometry CreateSquareGeometry(double centerX, double centerY, double size)
        {
            // Cube/Square is centered on the origin point
            double halfSize = size / 2;
            return new RectangleGeometry(new Rect(
                centerX - halfSize,
                centerY - halfSize,
                size,
                size));
        }

        private static Geometry CreateConeGeometry(double originX, double originY, double length, double angleInDegrees)
        {
            // Cone has a 53-degree spread (standard D&D cone)
            double coneAngle = 53.0 / 2.0; // Half angle
            double radians = angleInDegrees * Math.PI / 180.0;
            double leftRadians = (angleInDegrees - coneAngle) * Math.PI / 180.0;
            double rightRadians = (angleInDegrees + coneAngle) * Math.PI / 180.0;

            // Calculate the two end points of the cone
            Point leftPoint = new Point(
                originX + length * Math.Cos(leftRadians),
                originY + length * Math.Sin(leftRadians));

            Point rightPoint = new Point(
                originX + length * Math.Cos(rightRadians),
                originY + length * Math.Sin(rightRadians));

            // Create a path geometry for the cone (triangle)
            var figure = new PathFigure
            {
                StartPoint = new Point(originX, originY),
                IsClosed = true,
                IsFilled = true
            };

            // Add arc for the curved end of the cone
            figure.Segments.Add(new LineSegment(leftPoint, true));
            figure.Segments.Add(new ArcSegment(
                rightPoint,
                new Size(length, length),
                0,
                false,
                SweepDirection.Clockwise,
                true));
            figure.Segments.Add(new LineSegment(new Point(originX, originY), true));

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(figure);
            return pathGeometry;
        }

        private static Geometry CreateLineGeometry(double originX, double originY, double length, double width, double angleInDegrees)
        {
            double radians = angleInDegrees * Math.PI / 180.0;
            double perpRadians = radians + Math.PI / 2;

            double halfWidth = width / 2;

            // Calculate the four corners of the rectangle
            double dx = halfWidth * Math.Cos(perpRadians);
            double dy = halfWidth * Math.Sin(perpRadians);

            Point p1 = new Point(originX - dx, originY - dy);
            Point p2 = new Point(originX + dx, originY + dy);
            Point p3 = new Point(originX + length * Math.Cos(radians) + dx, originY + length * Math.Sin(radians) + dy);
            Point p4 = new Point(originX + length * Math.Cos(radians) - dx, originY + length * Math.Sin(radians) - dy);

            var figure = new PathFigure
            {
                StartPoint = p1,
                IsClosed = true,
                IsFilled = true
            };

            figure.Segments.Add(new LineSegment(p2, true));
            figure.Segments.Add(new LineSegment(p3, true));
            figure.Segments.Add(new LineSegment(p4, true));

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(figure);
            return pathGeometry;
        }

        /// <summary>
        /// Checks if a grid cell is within an area effect
        /// </summary>
        public static bool IsCellInEffect(AreaEffect effect, int cellX, int cellY, double gridCellSize)
        {
            // Get the center of the cell
            double cellCenterX = (cellX + 0.5) * gridCellSize;
            double cellCenterY = (cellY + 0.5) * gridCellSize;

            var geometry = GetEffectGeometry(effect, gridCellSize);
            return geometry.FillContains(new Point(cellCenterX, cellCenterY));
        }

        /// <summary>
        /// Gets all cells affected by an area effect
        /// </summary>
        public static List<(int x, int y)> GetAffectedCells(AreaEffect effect, double gridCellSize, int maxGridSize = 100)
        {
            var cells = new List<(int x, int y)>();
            var geometry = GetEffectGeometry(effect, gridCellSize);
            var bounds = geometry.Bounds;

            // Calculate cell range to check
            int minX = Math.Max(0, (int)Math.Floor(bounds.Left / gridCellSize));
            int maxX = Math.Min(maxGridSize, (int)Math.Ceiling(bounds.Right / gridCellSize));
            int minY = Math.Max(0, (int)Math.Floor(bounds.Top / gridCellSize));
            int maxY = Math.Min(maxGridSize, (int)Math.Ceiling(bounds.Bottom / gridCellSize));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    double cellCenterX = (x + 0.5) * gridCellSize;
                    double cellCenterY = (y + 0.5) * gridCellSize;

                    if (geometry.FillContains(new Point(cellCenterX, cellCenterY)))
                    {
                        cells.Add((x, y));
                    }
                }
            }

            return cells;
        }
    }
}