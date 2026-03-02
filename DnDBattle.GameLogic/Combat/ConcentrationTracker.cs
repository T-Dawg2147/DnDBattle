using DnDBattle.Core.Events;
using DnDBattle.Core.Models;
using CommunityToolkit.Mvvm.Messaging;

namespace DnDBattle.GameLogic.Combat;

public sealed class ConcentrationTracker
{
    private readonly Dictionary<Guid, string> _concentratingCombatants = new();
    private readonly IMessenger _messenger;

    public ConcentrationTracker(IMessenger messenger) => _messenger = messenger;

    public void BeginConcentrating(Combatant combatant, string spellName)
    {
        _concentratingCombatants[combatant.Id] = spellName;
        combatant.IsConcentrating = true;
    }

    public void BreakConcentration(Combatant combatant)
    {
        if (_concentratingCombatants.TryGetValue(combatant.Id, out var spell))
        {
            _concentratingCombatants.Remove(combatant.Id);
            combatant.IsConcentrating = false;
            _messenger.Send(new ConcentrationBrokenMessage(combatant.Id, spell));
        }
    }

    public void CheckConcentration(Combatant combatant, int damageTaken, Core.Interfaces.IDiceService dice)
    {
        if (!combatant.IsConcentrating) return;
        int dc = Math.Max(10, damageTaken / 2);
        int roll = dice.Roll(20) + combatant.GetAbilityModifier(Core.Enums.Ability.Constitution);
        if (roll < dc) BreakConcentration(combatant);
    }

    public bool IsConcentrating(Guid combatantId) => _concentratingCombatants.ContainsKey(combatantId);
    public string? GetConcentrationSpell(Guid combatantId) =>
        _concentratingCombatants.GetValueOrDefault(combatantId);
}
