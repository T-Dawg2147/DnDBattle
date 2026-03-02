using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Models.Creatures
{
    public class TokenDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Alignment { get; set; } = string.Empty;
        public string ChallengeRating { get; set; } = string.Empty;
        public int AC { get; set; }
        public int MaxHP { get; set; }
        public string HitDice { get; set; } = string.Empty;
        public int InitiativeMod { get; set; }
        public string Speed { get; set; } = string.Empty;
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Con { get; set; }
        public int Int { get; set; }
        public int Wis { get; set; }
        public int Cha { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public string Senses { get; set; } = string.Empty;
        public string Languages { get; set; } = string.Empty;
        public string Immunities { get; set; } = string.Empty;
        public string Resistances { get; set; } = string.Empty;
        public string Vulnerabilities { get; set; } = string.Empty;
        public string Traits { get; set; } = string.Empty;
        public List<object> Actions { get; set; } = new List<object>();
        public List<object> BonusActions { get; set; } = new List<object>();
        public List<object> Reactions { get; set; } = new List<object>();
        public List<object> LegendaryActions { get; set; } = new List<object>();
        public string Notes { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;

        // Placement
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int SizeInSquares { get; set; } = 1;
        public string ImagePath { get; set; } = string.Empty;

        // Other
        public int Initiative { get; set; }
        public bool IsPlayer { get; set; }
    }
}
