using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
    public class Action
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? Cost { get; set; }
        public int? AttackBonus { get; set; }
        public string DamageExpression { get; set; } = string.Empty;
        public string Range { get; set; } = string.Empty;
        public string? Description { get; set; }

        public override string ToString() =>
            Name ?? "Unknown Action";

        public DamageType DamageType { get; set; } = DamageType.None;

        public DamageType GetEffectiveDamageType()
        {
            if (DamageType != DamageType.None)
                return DamageType;

            return DetectDamageType();
        }

        private DamageType DetectDamageType()
        {
            string text = $"{Name} {Description} {DamageExpression}".ToLower();

            // Check for explicit damage type mentions first (most reliable)
            if (text.Contains("fire damage") || text.Contains("fire.")) return DamageType.Fire;
            if (text.Contains("cold damage") || text.Contains("cold.")) return DamageType.Cold;
            if (text.Contains("lightning damage") || text.Contains("lightning.")) return DamageType.Lightning;
            if (text.Contains("thunder damage") || text.Contains("thunder.")) return DamageType.Thunder;
            if (text.Contains("acid damage") || text.Contains("acid.")) return DamageType.Acid;
            if (text.Contains("poison damage") || text.Contains("poison.")) return DamageType.Poison;
            if (text.Contains("necrotic damage") || text.Contains("necrotic.")) return DamageType.Necrotic;
            if (text.Contains("radiant damage") || text.Contains("radiant.")) return DamageType.Radiant;
            if (text.Contains("force damage") || text.Contains("force.")) return DamageType.Force;
            if (text.Contains("psychic damage") || text.Contains("psychic.")) return DamageType.Psychic;
            if (text.Contains("bludgeoning damage") || text.Contains("bludgeoning.")) return DamageType.Bludgeoning;
            if (text.Contains("piercing damage") || text.Contains("piercing.")) return DamageType.Piercing;
            if (text.Contains("slashing damage") || text.Contains("slashing.")) return DamageType.Slashing;

            // Check for keywords that suggest damage type
            if (text.Contains("fire") || text.Contains("flame") || text.Contains("burn") || text.Contains("inferno"))
                return DamageType.Fire;
            if (text.Contains("cold") || text.Contains("frost") || text.Contains("ice") || text.Contains("freeze"))
                return DamageType.Cold;
            if (text.Contains("lightning") || text.Contains("electric") || text.Contains("shock") || text.Contains("bolt"))
                return DamageType.Lightning;
            if (text.Contains("thunder") || text.Contains("sonic") || text.Contains("boom"))
                return DamageType.Thunder;
            if (text.Contains("acid") || text.Contains("corrosive") || text.Contains("dissolve"))
                return DamageType.Acid;
            if (text.Contains("poison") || text.Contains("venom") || text.Contains("toxic"))
                return DamageType.Poison;
            if (text.Contains("necrotic") || text.Contains("wither") || text.Contains("decay") || text.Contains("life drain"))
                return DamageType.Necrotic;
            if (text.Contains("radiant") || text.Contains("holy") || text.Contains("divine") || text.Contains("smite"))
                return DamageType.Radiant;
            if (text.Contains("force") || text.Contains("magic missile"))
                return DamageType.Force;
            if (text.Contains("psychic") || text.Contains("mental") || text.Contains("mind"))
                return DamageType.Psychic;

            // Check weapon types for physical damage
            if (text.Contains("bite") || text.Contains("claw") || text.Contains("talon") ||
                text.Contains("sword") || text.Contains("axe") || text.Contains("scimitar") ||
                text.Contains("glaive") || text.Contains("halberd"))
                return DamageType.Slashing;

            if (text.Contains("arrow") || text.Contains("spear") || text.Contains("javelin") ||
                text.Contains("rapier") || text.Contains("dagger") || text.Contains("pike") ||
                text.Contains("horn") || text.Contains("tusk") || text.Contains("sting"))
                return DamageType.Piercing;

            if (text.Contains("club") || text.Contains("mace") || text.Contains("hammer") ||
                text.Contains("staff") || text.Contains("fist") || text.Contains("slam") ||
                text.Contains("tail") || text.Contains("rock") || text.Contains("boulder") ||
                text.Contains("crush") || text.Contains("stomp"))
                return DamageType.Bludgeoning;

            // Default to slashing for generic melee attacks
            if (text.Contains("melee") || text.Contains("attack"))
                return DamageType.Slashing;

            // Default
            return DamageType.Slashing;
        }
    }

    public enum ActionType
    {
        Action,
        BonusAction,
        Reaction,
        LegendaryAction
    }
}
