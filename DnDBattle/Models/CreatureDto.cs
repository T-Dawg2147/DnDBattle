using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
    public class CreatureDto
    {
        public Guid Id { get; set; }
        public Guid? TokenId { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string Allignment { get; set; }
        public string ChallengeRating { get; set; }
        public int ArmorClass { get; set; }
        public int MaxHP { get; set; }
        public int CurrentHP { get; set; }
        public string HitDice { get; set; }
        public int InitiativeMod { get; set; }
        public string Speed { get; set; }
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Con { get; set; }
        public int Int { get; set; }
        public int Wis { get; set; }
        public int Cha { get; set; }
        public List<object> Skills { get; set; }
        public string Senses { get; set; }
        public string Languages { get; set; }
        public string Immunities { get; set; }
        public string Resistances { get; set; }
        public string Vulnerabilities { get; set; }
        public string Traits { get; set; }
        public List<object> Actions { get; set; }
        public List<object> BonusActions { get; set; }
        public List<object> Reactions { get; set; }
        public List<object> LegendaryActions { get; set; }
        public string Notes { get; set; }
        public string IconPath { get;set; }
    }
}
