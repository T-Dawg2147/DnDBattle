using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace DnDBattle.Services.Grid
{
    public static class GridService
    {
        public static double GridToWorldX(int gridX, double cellSize) => gridX * cellSize;
        public static double GridToWorldY(int gridY, double cellSize) => gridY * cellSize;

        public static double WorldToGridX(int worldX, double cellSize) => (int)Math.Round(worldX / cellSize);
        public static double WorldToGridY(int worldY, double cellSize) => (int)Math.Round(worldY / cellSize);
    }
}
