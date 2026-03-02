using DnDBattle.Core.Models;

namespace DnDBattle.GameLogic.Encounters;

public sealed class EncounterBuilder
{
    private readonly List<Combatant> _combatants = new();
    private string _name = "New Encounter";

    public EncounterBuilder WithName(string name) { _name = name; return this; }

    public EncounterBuilder AddCombatant(Combatant c) { _combatants.Add(c); return this; }

    public EncounterBuilder AddCombatants(IEnumerable<Combatant> combatants)
    {
        _combatants.AddRange(combatants);
        return this;
    }

    public (string Name, IReadOnlyList<Combatant> Combatants) Build() =>
        (_name, _combatants.AsReadOnly());
}
