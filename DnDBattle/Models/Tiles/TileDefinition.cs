using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DnDBattle.Models.Tiles
{
    public class TileDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
        public string Category { get; set; } = "Uncategorized";
        public string FilePath { get; set; }

        public BitmapImage CachedImage { get; set; }

        public int WidthInSquares { get; set; } = 1;

        public int HeightInSquares { get; set; } = 1;

        public bool BlockMovement { get; set; } = false;

        public bool BlockLineOfSight { get; set; } = false;

        public override string ToString() => Name ?? "Unknown Tile";
    }
}
