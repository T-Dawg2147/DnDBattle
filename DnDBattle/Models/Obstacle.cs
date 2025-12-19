using System.Windows;

namespace DnDBattle.Models
{
    public class Obstacle
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public List<Point> PolygonGridPoints { get; set; } = new List<Point>();

        public string? Label { get; set; }
    }
}
