using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
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
        Hidden = 1 << 22
    }

    public static class ConditionExtensions
    {
        public static string ToDisplayString(this Condition condition)
        {
            if (condition == Condition.None)
                return "None";

            var conditions = new List<string>();

            foreach (Condition c in Enum.GetValues(typeof(Condition)))
            {
                if (c != Condition.None && condition.HasFlag(c))
                {
                    conditions.Add(c.ToString());
                }
            }

            return string.Join(", ", conditions);
        }
    }
}
