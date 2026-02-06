using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.Models.Combat;

namespace DnDBattle.Models.Combat
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
