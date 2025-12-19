using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DnDBattle.Models
{
    public enum ObstacleActionType { Add, Remove, MoveVertex }

    public class ObstacleAction
    {
        public ObstacleActionType ActionType { get; set; }
        public Obstacle Obstacle { get; set; }
        public int VertexIndex { get; set; }
        public Point OldPosition { get; set; }
        public Point NewPosition { get; set; }
    }
}
