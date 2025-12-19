using System.Windows;

namespace DnDBattle.Models
{
    public class LightSource
    {
        public Point CenterGrid { get; set; }
        public double RadiusSquares { get; set; } = 6;
        public double Intensity { get; set; } = 1.0;
    }
}
