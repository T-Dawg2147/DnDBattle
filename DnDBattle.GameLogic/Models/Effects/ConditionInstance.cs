using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
namespace DnDBattle.Models.Effects
{
    /// <summary>
    /// Tracks a specific condition applied to a token, including duration and save-to-end information.
    /// </summary>
    public class ConditionInstance
    {
        public Condition Type { get; set; }
        public int DurationRounds { get; set; }
        public int RoundsRemaining { get; set; }
        public Ability? SaveToEndAbility { get; set; }
        public int SaveDC { get; set; }
        public Guid? SourceTokenId { get; set; }

        /// <summary>
        /// Ticks down one round. Returns true if the condition has expired.
        /// </summary>
        public bool TickRound()
        {
            if (DurationRounds <= 0) return false; // Permanent conditions
            RoundsRemaining--;
            return RoundsRemaining <= 0;
        }
    }
}
