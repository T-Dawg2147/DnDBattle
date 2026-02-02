using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    public class TileMapDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double CellSize { get; set; }
        public string BackgroundColor { get; set; }
        public bool ShowGrid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public List<TileDto> Tiles { get; set; } = new List<TileDto>();
    }

    public class TileDto
    {
        public Guid InstanceId { get; set; }
        public string TileDefinitionId { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int Rotation { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }
        public int? ZIndex { get; set; }
        public string Notes { get; set; }
    }
}
