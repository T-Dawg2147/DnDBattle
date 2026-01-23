using System;
using System.Collections.Generic;
using System.Text;

namespace DnDBattle.Models
{
    /// <summary>
    /// D&D 5e Conditions as flags (allows multiple conditions at once)
    /// </summary>
    [Flags]
    public enum Condition
    {
        None = 0,
        Blinded = 1 << 0,
        Charmed = 1 << 1,
        Deafened = 1 << 2,
        Frightened = 1 << 3,
        Grappled = 1 << 4,
        Incapacitated = 1 << 5,
        Invisible = 1 << 6,
        Paralyzed = 1 << 7,
        Petrified = 1 << 8,
        Poisoned = 1 << 9,
        Prone = 1 << 10,
        Restrained = 1 << 11,
        Stunned = 1 << 12,
        Unconscious = 1 << 13,
        Exhaustion1 = 1 << 14,
        Exhaustion2 = 1 << 15,
        Exhaustion3 = 1 << 16,
        Exhaustion4 = 1 << 17,
        Exhaustion5 = 1 << 18,
        Exhaustion6 = 1 << 19,
        Concentrating = 1 << 20,
        Dodging = 1 << 21,
        Hidden = 1 << 22,
        Blessed = 1 << 23,
        Cursed = 1 << 24,
        Hasted = 1 << 25,
        Slowed = 1 << 26,
        Flying = 1 << 27,
        Raging = 1 << 28,
        Marked = 1 << 29,
        HuntersMark = 1 << 30
    }

    /// <summary>
    /// Extension methods for Condition enum
    /// </summary>
    public static class ConditionExtensions
    {
        /// <summary>
        /// Gets a display-friendly string of all active conditions
        /// </summary>
        public static string ToDisplayString(this Condition conditions)
        {
            if (conditions == Condition.None)
                return "";

            var parts = new List<string>();
            foreach (Condition c in Enum.GetValues(typeof(Condition)))
            {
                if (c != Condition.None && conditions.HasFlag(c))
                {
                    parts.Add(GetConditionName(c));
                }
            }
            return string.Join(", ", parts);
        }

        /// <summary>
        /// Gets the display name for a condition
        /// </summary>
        public static string GetConditionName(Condition condition)
        {
            return condition switch
            {
                Condition.Exhaustion1 => "Exhaustion (1)",
                Condition.Exhaustion2 => "Exhaustion (2)",
                Condition.Exhaustion3 => "Exhaustion (3)",
                Condition.Exhaustion4 => "Exhaustion (4)",
                Condition.Exhaustion5 => "Exhaustion (5)",
                Condition.Exhaustion6 => "Exhaustion (6)",
                Condition.HuntersMark => "Hunter's Mark",
                _ => condition.ToString()
            };
        }

        /// <summary>
        /// Gets the icon/emoji for a condition
        /// </summary>
        public static string GetConditionIcon(Condition condition)
        {
            return condition switch
            {
                Condition.Blinded => "👁️‍🗨️",
                Condition.Charmed => "💕",
                Condition.Deafened => "🔇",
                Condition.Frightened => "😨",
                Condition.Grappled => "🤼",
                Condition.Incapacitated => "🚫",
                Condition.Invisible => "👻",
                Condition.Paralyzed => "⚡",
                Condition.Petrified => "🗿",
                Condition.Poisoned => "☠️",
                Condition.Prone => "🔽",
                Condition.Restrained => "⛓️",
                Condition.Stunned => "💫",
                Condition.Unconscious => "💤",
                Condition.Exhaustion1 => "😓",
                Condition.Exhaustion2 => "😓",
                Condition.Exhaustion3 => "😓",
                Condition.Exhaustion4 => "😓",
                Condition.Exhaustion5 => "😓",
                Condition.Exhaustion6 => "💀",
                Condition.Concentrating => "🎯",
                Condition.Dodging => "🏃",
                Condition.Hidden => "🥷",
                Condition.Blessed => "✨",
                Condition.Cursed => "🔮",
                Condition.Hasted => "⚡",
                Condition.Slowed => "🐌",
                Condition.Flying => "🦅",
                Condition.Raging => "😤",
                Condition.Marked => "🎯",
                Condition.HuntersMark => "🏹",
                _ => "❓"
            };
        }

        /// <summary>
        /// Gets the color associated with a condition (for UI)
        /// </summary>
        public static System.Windows.Media.Color GetConditionColor(Condition condition)
        {
            return condition switch
            {
                Condition.Blinded => System.Windows.Media.Color.FromRgb(64, 64, 64),
                Condition.Charmed => System.Windows.Media.Color.FromRgb(255, 105, 180),
                Condition.Deafened => System.Windows.Media.Color.FromRgb(128, 128, 128),
                Condition.Frightened => System.Windows.Media.Color.FromRgb(148, 0, 211),
                Condition.Grappled => System.Windows.Media.Color.FromRgb(139, 69, 19),
                Condition.Incapacitated => System.Windows.Media.Color.FromRgb(128, 0, 0),
                Condition.Invisible => System.Windows.Media.Color.FromRgb(173, 216, 230),
                Condition.Paralyzed => System.Windows.Media.Color.FromRgb(255, 215, 0),
                Condition.Petrified => System.Windows.Media.Color.FromRgb(169, 169, 169),
                Condition.Poisoned => System.Windows.Media.Color.FromRgb(0, 128, 0),
                Condition.Prone => System.Windows.Media.Color.FromRgb(165, 42, 42),
                Condition.Restrained => System.Windows.Media.Color.FromRgb(139, 90, 43),
                Condition.Stunned => System.Windows.Media.Color.FromRgb(255, 255, 0),
                Condition.Unconscious => System.Windows.Media.Color.FromRgb(25, 25, 112),
                Condition.Exhaustion1 or Condition.Exhaustion2 or Condition.Exhaustion3 or 
                Condition.Exhaustion4 or Condition.Exhaustion5 or Condition.Exhaustion6 
                    => System.Windows.Media.Color.FromRgb(255, 140, 0),
                Condition.Concentrating => System.Windows.Media.Color.FromRgb(0, 191, 255),
                Condition.Dodging => System.Windows.Media.Color.FromRgb(50, 205, 50),
                Condition.Hidden => System.Windows.Media.Color.FromRgb(47, 79, 79),
                Condition.Blessed => System.Windows.Media.Color.FromRgb(255, 223, 0),
                Condition.Cursed => System.Windows.Media.Color.FromRgb(75, 0, 130),
                Condition.Hasted => System.Windows.Media.Color.FromRgb(0, 255, 127),
                Condition.Slowed => System.Windows.Media.Color.FromRgb(100, 149, 237),
                Condition.Flying => System.Windows.Media.Color.FromRgb(135, 206, 250),
                Condition.Raging => System.Windows.Media.Color.FromRgb(220, 20, 60),
                Condition.Marked => System.Windows.Media.Color.FromRgb(255, 69, 0),
                Condition.HuntersMark => System.Windows.Media.Color.FromRgb(34, 139, 34),
                _ => System.Windows.Media.Color.FromRgb(128, 128, 128)
            };
        }

        /// <summary>
        /// Gets the description of a condition's effects
        /// </summary>
        public static string GetConditionDescription(Condition condition)
        {
            return condition switch
            {
                Condition.Blinded => "Can't see. Auto-fail sight checks. Attacks have disadvantage. Attacks against have advantage.",
                Condition.Charmed => "Can't attack the charmer. Charmer has advantage on social checks.",
                Condition.Deafened => "Can't hear. Auto-fail hearing checks.",
                Condition.Frightened => "Disadvantage on checks/attacks while source visible. Can't willingly move closer.",
                Condition.Grappled => "Speed is 0. Ends if grappler incapacitated or forced apart.",
                Condition.Incapacitated => "Can't take actions or reactions.",
                Condition.Invisible => "Impossible to see without special sense. Attacks have advantage. Attacks against have disadvantage.",
                Condition.Paralyzed => "Incapacitated, can't move or speak. Auto-fail STR/DEX saves. Attacks have advantage. Hits within 5ft are crits.",
                Condition.Petrified => "Transformed to stone. Weight x10. Incapacitated. Resistance to all damage. Immune to poison/disease.",
                Condition.Poisoned => "Disadvantage on attack rolls and ability checks.",
                Condition.Prone => "Can only crawl. Disadvantage on attacks. Melee attacks have advantage, ranged have disadvantage.",
                Condition.Restrained => "Speed 0. Attacks have disadvantage. Attacks against have advantage. Disadvantage on DEX saves.",
                Condition.Stunned => "Incapacitated, can't move, can only speak falteringly. Auto-fail STR/DEX saves. Attacks have advantage.",
                Condition.Unconscious => "Incapacitated, can't move or speak. Unaware. Drop held items, fall prone. Auto-fail STR/DEX saves. Attacks have advantage. Hits within 5ft are crits.",
                Condition.Exhaustion1 => "Disadvantage on ability checks.",
                Condition.Exhaustion2 => "Speed halved.",
                Condition.Exhaustion3 => "Disadvantage on attacks and saves.",
                Condition.Exhaustion4 => "HP maximum halved.",
                Condition.Exhaustion5 => "Speed reduced to 0.",
                Condition.Exhaustion6 => "Death.",
                Condition.Concentrating => "Maintaining concentration on a spell. CON save on damage or lose spell.",
                Condition.Dodging => "Attacks against have disadvantage. Advantage on DEX saves.",
                Condition.Hidden => "Can't be seen. Attacks have advantage.",
                Condition.Blessed => "+1d4 to attacks and saves.",
                Condition.Cursed => "Varies by curse effect.",
                Condition.Hasted => "Double speed, +2 AC, advantage on DEX saves, extra action.",
                Condition.Slowed => "Half speed, -2 AC, no reactions, limited actions.",
                Condition.Flying => "Currently airborne.",
                Condition.Raging => "Advantage on STR checks/saves, bonus damage, resistance to physical.",
                Condition.Marked => "Marked by another creature.",
                Condition.HuntersMark => "Extra 1d6 damage from marking creature.",
                _ => "Unknown condition."
            };
        }

        /// <summary>
        /// Gets all individual conditions from a combined flag value
        /// </summary>
        public static IEnumerable<Condition> GetActiveConditions(this Condition conditions)
        {
            foreach (Condition c in Enum.GetValues(typeof(Condition)))
            {
                if (c != Condition.None && conditions.HasFlag(c))
                {
                    yield return c;
                }
            }
        }

        /// <summary>
        /// Gets the current exhaustion level (0-6)
        /// </summary>
        public static int GetExhaustionLevel(this Condition conditions)
        {
            if (conditions.HasFlag(Condition.Exhaustion6)) return 6;
            if (conditions.HasFlag(Condition.Exhaustion5)) return 5;
            if (conditions.HasFlag(Condition.Exhaustion4)) return 4;
            if (conditions.HasFlag(Condition.Exhaustion3)) return 3;
            if (conditions.HasFlag(Condition.Exhaustion2)) return 2;
            if (conditions.HasFlag(Condition.Exhaustion1)) return 1;
            return 0;
        }

        /// <summary>
        /// Sets the exhaustion level, clearing other exhaustion flags
        /// </summary>
        public static Condition SetExhaustionLevel(this Condition conditions, int level)
        {
            // Clear all exhaustion flags
            conditions &= ~(Condition.Exhaustion1 | Condition.Exhaustion2 | Condition.Exhaustion3 |
                           Condition.Exhaustion4 | Condition.Exhaustion5 | Condition.Exhaustion6);

            // Set the appropriate flag
            return level switch
            {
                1 => conditions | Condition.Exhaustion1,
                2 => conditions | Condition.Exhaustion2,
                3 => conditions | Condition.Exhaustion3,
                4 => conditions | Condition.Exhaustion4,
                5 => conditions | Condition.Exhaustion5,
                6 => conditions | Condition.Exhaustion6,
                _ => conditions
            };
        }
    }
}