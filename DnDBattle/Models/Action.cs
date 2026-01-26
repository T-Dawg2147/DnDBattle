using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
    public class Action
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int? Cost { get; set; }
        public int? AttackBonus { get; set; }
        public string DamageExpression { get; set; }
        public string Range { get; set; }
        public string? Description { get; set; }

        public override string ToString() =>
            Name ?? "Unknown Action";
    }

    public enum ActionType
    {
        Action,
        BonusAction,
        Reaction,
        LegendaryAction
    }
}
