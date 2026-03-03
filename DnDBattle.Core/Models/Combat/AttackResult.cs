using DnDBattle.Models;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
namespace DnDBattle.Models.Combat
{
    /// <summary>
    /// Contains the full result of a single attack roll including hit/miss, damage, and crits.
    /// </summary>
    public class AttackResult
    {
        public Token Attacker { get; set; }
        public Token Defender { get; set; }
        public Action Attack { get; set; }
        public int D20Roll { get; set; }
        public int AttackBonus { get; set; }
        public int TotalAttack { get; set; }
        public int TargetAC { get; set; }
        public bool Hit { get; set; }
        public bool IsCriticalHit { get; set; }
        public bool IsCriticalFumble { get; set; }
        public int DamageRoll { get; set; }
        public int ActualDamage { get; set; }
        public bool IsCriticalDamage { get; set; }
        public string? DamageDescription { get; set; }
        public AttackMode Mode { get; set; }
        public CoverLevel Cover { get; set; }

        public override string ToString()
        {
            string attackName = Attack?.Name ?? "Attack";
            string attackerName = Attacker?.Name ?? "Attacker";
            string defenderName = Defender?.Name ?? "Target";

            string result = $"{attackerName} attacks {defenderName} with {attackName}: ";
            result += $"{D20Roll}+{AttackBonus}={TotalAttack} vs AC {TargetAC}";

            if (Cover != CoverLevel.None)
            {
                int coverBonus = Cover == CoverLevel.Half ? 2 : Cover == CoverLevel.ThreeQuarters ? 5 : 0;
                result += $" (cover +{coverBonus})";
            }

            if (IsCriticalHit)
                result += " CRITICAL HIT!";
            else if (IsCriticalFumble)
                result += " FUMBLE!";

            if (Hit)
            {
                result += $" HIT for {DamageRoll} damage";
                if (ActualDamage != DamageRoll)
                    result += $" ({ActualDamage} after modifiers)";
                if (Defender != null)
                    result += $". {defenderName} HP: {Defender.HP}/{Defender.MaxHP}";
            }
            else
            {
                result += " MISS!";
            }

            return result;
        }
    }
}
