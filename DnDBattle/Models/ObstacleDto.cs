using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
    public class ObstacleDto
    {
        public string Label { get; set; }
        public List<PointDto> Polygon { get; set; }
    }
}
