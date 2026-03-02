using DnDBattle.Core.Models;

namespace DnDBattle.Core.Interfaces;

public interface ISpellService
{
    bool TryCastSpell(Combatant caster, SpellDefinition spell, IEnumerable<Combatant> targets);
    bool HasSpellSlot(Combatant caster, int level);
    void UseSpellSlot(Combatant caster, int level);
    void RestoreSpellSlots(Combatant caster);
    IReadOnlyList<SpellDefinition> GetAvailableSpells(Combatant caster);
}
