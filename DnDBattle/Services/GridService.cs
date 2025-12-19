using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Services
{
    public static class GridService
    {
        public static double GridToWorldX(int gridX, double cellSize) => gridX * cellSize;
        public static double GridToWorldY(int gridY, double cellSize) => gridY * cellSize;

        public static double WorldToGridX(int worldX, double cellSize) => (int)Math.Round(worldX / cellSize);
        public static double WorldToGridY(int worldY, double cellSize) => (int)Math.Round(worldY / cellSize);
    }
}
