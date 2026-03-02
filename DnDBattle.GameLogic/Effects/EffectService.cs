using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;

namespace DnDBattle.GameLogic.Effects;

public sealed class EffectService : IEffectService
{
    private readonly IDiceService _dice;
    private readonly List<AreaEffect> _activeEffects = new();

    public EffectService(IDiceService dice) => _dice = dice;

    public IReadOnlyList<AreaEffect> ActiveEffects => _activeEffects;

    public void ApplyAreaEffect(AreaEffect effect, IEnumerable<Combatant> targets)
    {
        _activeEffects.Add(effect);
        foreach (var target in targets)
        {
            int damage = _dice.ParseAndRoll(effect.DamageDice);
            target.CurrentHitPoints = Math.Max(0, target.CurrentHitPoints - damage);
        }
    }

    public void TickEffects(IEnumerable<Combatant> combatants)
    {
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            var effect = _activeEffects[i];
            // Decrement remaining rounds each tick
            _activeEffects[i] = effect with { RemainingRounds = effect.RemainingRounds - 1 };
        }
    }

    public void RemoveExpiredEffects(IEnumerable<Combatant> combatants)
    {
        _activeEffects.RemoveAll(e => e.RemainingRounds <= 0);
    }
}
