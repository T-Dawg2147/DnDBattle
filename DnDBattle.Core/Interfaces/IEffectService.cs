using DnDBattle.Core.Models;

namespace DnDBattle.Core.Interfaces;

public interface IEffectService
{
    void ApplyAreaEffect(AreaEffect effect, IEnumerable<Combatant> targets);
    void TickEffects(IEnumerable<Combatant> combatants);
    void RemoveExpiredEffects(IEnumerable<Combatant> combatants);
    IReadOnlyList<AreaEffect> ActiveEffects { get; }
}
