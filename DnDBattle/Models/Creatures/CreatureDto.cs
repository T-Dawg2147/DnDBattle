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
using DnDBattle.Models.Creatures;

namespace DnDBattle.Models.Creatures
{
    public class CreatureDto
    {
        public Guid Id { get; set; }
        public Guid? TokenId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Allignment { get; set; } = string.Empty;
        public string ChallengeRating { get; set; } = string.Empty;
        public int ArmorClass { get; set; }
        public int MaxHP { get; set; }
        public int CurrentHP { get; set; }
        public string HitDice { get; set; } = string.Empty;
        public int InitiativeMod { get; set; }
        public string Speed { get; set; } = string.Empty;
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Con { get; set; }
        public int Int { get; set; }
        public int Wis { get; set; }
        public int Cha { get; set; }
        public List<object> Skills { get; set; }
        public string Senses { get; set; } = string.Empty;
        public string Languages { get; set; } = string.Empty;
        public string Immunities { get; set; } = string.Empty;
        public string Resistances { get; set; } = string.Empty;
        public string Vulnerabilities { get; set; } = string.Empty;
        public string Traits { get; set; } = string.Empty;
        public List<object> Actions { get; set; }
        public List<object> BonusActions { get; set; }
        public List<object> Reactions { get; set; }
        public List<object> LegendaryActions { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string IconPath { get;set; } = string.Empty;
    }
}
