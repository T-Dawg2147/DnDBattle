using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
    public class TokenDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string Alignment { get; set; }
        public string ChallengeRating { get; set; }
        public int AC { get; set; }
        public int MaxHP { get; set; }
        public string HitDice { get; set; }
        public int InitiativeMod { get; set; }
        public string Speed { get; set; }
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Con { get; set; }
        public int Int { get; set; }
        public int Wis { get; set; }
        public int Cha { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public string Senses { get; set; }
        public string Languages { get; set; }
        public string Immunities { get; set; }
        public string Resistances { get; set; }
        public string Vulnerabilities { get; set; }
        public string Traits { get; set; }
        public List<object> Actions { get; set; } = new List<object>();
        public List<object> BonusActions { get; set; } = new List<object>();
        public List<object> Reactions { get; set; } = new List<object>();
        public List<object> LegendaryActions { get; set; } = new List<object>();
        public string Notes { get; set; }
        public string IconPath { get; set; }

        // Placement
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int SizeInSquares { get; set; } = 1;
        public string ImagePath { get; set; }

        // Other
        public int Initiative { get; set; }
        public bool IsPlayer { get; set; }
    }
}
