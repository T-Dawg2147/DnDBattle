using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Models
{
    /// <summary>
    /// Represents a custom polygon-shaped area effect with arbitrary vertices
    /// </summary>
    public class PolygonAreaEffect : AreaEffect
    {
        public List<Point> Vertices { get; set; } = new List<Point>();

        /// <summary>
        /// Gets the WPF geometry for this polygon at the given cell size
        /// </summary>
        public Geometry GetPolygonGeometry(double cellSize)
        {
            if (Vertices.Count < 3) return Geometry.Empty;

            var figure = new PathFigure
            {
                StartPoint = new Point(Vertices[0].X * cellSize, Vertices[0].Y * cellSize),
                IsClosed = true,
                IsFilled = true
            };

            for (int i = 1; i < Vertices.Count; i++)
            {
                figure.Segments.Add(new LineSegment(
                    new Point(Vertices[i].X * cellSize, Vertices[i].Y * cellSize), true));
            }

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        /// <summary>
        /// Ray-casting point-in-polygon test
        /// </summary>
        public bool ContainsGridPoint(double gridX, double gridY)
        {
            if (Vertices.Count < 3) return false;

            int intersections = 0;
            int count = Vertices.Count;

            for (int i = 0; i < count; i++)
            {
                var v1 = Vertices[i];
                var v2 = Vertices[(i + 1) % count];

                if ((v1.Y > gridY) != (v2.Y > gridY))
                {
                    double xIntersect = (v2.X - v1.X) * (gridY - v1.Y) / (v2.Y - v1.Y) + v1.X;
                    if (gridX < xIntersect)
                        intersections++;
                }
            }

            return (intersections % 2) == 1;
        }
    }
}
