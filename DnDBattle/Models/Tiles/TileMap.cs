using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    public class TileMap
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = "Untitled Map";

        public int WidthInSquares { get; set; } = 30;

        public int HeightInSquares { get; set; } = 30;

        public double GridCellSize { get; set; } = 48.0;

        public List<Tile> Tiles { get; set; } = new List<Tile>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        public string BackgroundColor { get; set; } = "#1E1E1E";
    }
}
