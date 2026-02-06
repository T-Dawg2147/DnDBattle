using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Utils;
using DnDBattle.Services;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Combat
{
    /// <summary>
    /// Manages spell slot usage and casting workflow, integrating with concentration tracking.
    /// </summary>
    public class SpellCastingService
    {
        private readonly ConcentrationService _concentrationService;

        public SpellCastingService(ConcentrationService concentrationService)
        {
            _concentrationService = concentrationService;
        }

        /// <summary>
        /// Attempts to cast a spell using a slot of the specified level.
        /// Returns true if the slot was consumed, false if no slot available.
        /// </summary>
        public bool CastSpell(Token caster, string spellName, int spellLevel, bool requiresConcentration)
        {
            if (!Options.EnableSpellSlotTracking)
                return true; // If tracking disabled, always succeed

            if (spellLevel < 1 || spellLevel > 9)
                return true; // Cantrips and special abilities don't use slots

            if (!caster.SpellSlots.UseSlot(spellLevel))
                return false; // No slot available

            if (requiresConcentration)
            {
                _concentrationService.StartConcentration(caster, spellName);
            }

            return true;
        }

        /// <summary>
        /// Checks if the caster has a slot available at the given level.
        /// </summary>
        public static bool HasSlot(Token caster, int level)
        {
            return caster.SpellSlots.GetCurrentSlots(level) > 0;
        }

        /// <summary>
        /// Gets the highest available slot level for upcasting.
        /// </summary>
        public static int GetHighestAvailableSlot(Token caster, int minimumLevel)
        {
            for (int i = 9; i >= minimumLevel; i--)
            {
                if (caster.SpellSlots.GetCurrentSlots(i) > 0)
                    return i;
            }
            return 0;
        }
    }
}
