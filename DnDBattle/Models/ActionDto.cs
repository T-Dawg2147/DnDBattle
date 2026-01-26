using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
    public class ActionDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Range { get; set; }
        public string Damage { get; set; }
        public int? Cost { get; set; }
        public int? AttackBonus { get; set; }
        public string Description { get; set; }
    }
}
