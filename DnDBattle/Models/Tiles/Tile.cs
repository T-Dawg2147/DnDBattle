using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    public class Tile
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string TileDefinitionId { get; set; }

        public int GridX { get; set; }

        public int GridY { get; set; }

        public int Rotation { get; set; } = 0;

        public int Layer { get; set; } = 0;
    }
}
