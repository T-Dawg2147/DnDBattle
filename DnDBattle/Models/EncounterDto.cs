using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
    public class EncounterDto
    {
        public string MapImagePath { get; set; }
        public List<TokenDto> Tokens { get; set; } = new List<TokenDto>();
        public List<ObstacleDto> Obstacles { get; set; } = new List<ObstacleDto>();
        public List<LightDto> Lights { get; set; } = new List<LightDto>();
    }
}
