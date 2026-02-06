using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Models
{
    /// <summary>
    /// Represents a single particle in an effect animation
    /// </summary>
    public class Particle
    {
        public Point Position { get; set; }
        public Vector Velocity { get; set; }
        public Color Color { get; set; }
        public double Size { get; set; }
        public double Lifetime { get; set; }
        public double Opacity { get; set; }
    }
}
