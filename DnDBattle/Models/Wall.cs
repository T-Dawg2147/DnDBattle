using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DnDBattle.Models
{
    public enum WallType
    {
        Solid,
        Door,
        Window,
        Halfwall,
        Invisible
    }

    public class Wall : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; } = Guid.NewGuid();

        private Point _startPoint;
        public Point StartPoint
        {
            get => _startPoint;
            set { _startPoint = value; OnPropertyChanged(nameof(StartPoint)); }
        }

        private Point _endPoint;
        public Point EndPoint
        {
            get => _endPoint;
            set { _endPoint = value; OnPropertyChanged(nameof(EndPoint)); }
        }

        private WallType _wallType = WallType.Solid;
        public WallType WallType
        {
            get => _wallType;
            set { _wallType = value; OnPropertyChanged(nameof(WallType)); }
        }

        private bool _isDoor = false;
        public bool IsDoor
        {
            get => _isDoor;
            set { _isDoor = value; OnPropertyChanged(nameof(IsDoor)); }
        }

        private bool _isOpen = false;
        public bool IsOpen
        {
            get => _isOpen;
            set { _isOpen = value; OnPropertyChanged(nameof(IsOpen)); }
        }

        private string _label;
        public string Label
        {
            get => _label;
            set { _label = value; OnPropertyChanged(nameof(Label)); }
        }

        public bool BlocksLight => WallType switch
        {
            WallType.Solid => true,
            WallType.Door => !IsOpen,
            WallType.Window => false,
            WallType.Halfwall => false,
            WallType.Invisible => false,
            _ => true
        };

        public bool BlocksSight => WallType switch
        {
            WallType.Solid => true,
            WallType.Door => !IsOpen,
            WallType.Window => false,
            WallType.Halfwall => false,
            WallType.Invisible => false,
            _ => true
        };

        public bool BlocksMovement => WallType switch
        {
            WallType.Solid => true,
            WallType.Door => !IsOpen,
            WallType.Window => true,
            WallType.Halfwall => false,
            WallType.Invisible => true,
            _ => true
        };

        public double Length
        {
            get
            {
                var dx = EndPoint.X - StartPoint.X;
                var dy = EndPoint.Y - StartPoint.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        public bool IsPointNear(Point point, double threshold = 0.3) =>
            DistanceToPoint(point) <= threshold;

        public double DistanceToPoint(Point point)
        {
            var dx = EndPoint.X - StartPoint.X;
            var dy = EndPoint.Y - StartPoint.Y;
            var lengthSquared = dx * dx + dy * dy;

            if (lengthSquared == 0)
                return Math.Sqrt(Math.Pow(point.X - StartPoint.X, 2) + Math.Pow(point.Y - StartPoint.Y, 2));

            var t = Math.Max(0, Math.Min(1, ((point.X - StartPoint.X) * dx + (point.Y - StartPoint.Y) * dy) / lengthSquared));
            var projectionX = StartPoint.X + t * dx;
            var projectionY = StartPoint.Y + t * dy;

            return Math.Sqrt(Math.Pow(point.X - projectionX, 2) + Math.Pow(point.Y - projectionY, 2));
        }

        public bool IntersectsLine(Point lineStart, Point lineEnd, out Point intersection)
        {
            intersection = new Point();

            var p1 = StartPoint;
            var p2 = EndPoint;
            var p3 = lineStart;
            var p4 = lineEnd;

            var d = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
            if (Math.Abs(d) < 0.0001) return false;

            var t = ((p1.X - p3.X) * (p3.Y - p4.Y) - (p1.Y - p3.Y) * (p3.X - p4.X)) / d;
            var u = -((p1.X - p2.X) * (p1.Y - p3.Y) - (p1.Y - p2.Y) * (p1.X - p3.X)) / d;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                intersection = new Point(
                    p1.X + t * (p2.X - p1.X),
                    p1.Y + t * (p2.Y - p1.Y));  // FIXED: Was p2.Y
                return true;
            }
            return false;
        }
    }
}
