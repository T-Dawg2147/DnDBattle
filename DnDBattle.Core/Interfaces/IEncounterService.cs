using DnDBattle.Core.Models;

namespace DnDBattle.Core.Interfaces;

public interface IEncounterService
{
    void StartEncounter(IEnumerable<Combatant> combatants);
    void NextTurn();
    void EndEncounter();
    Combatant? ActiveCombatant { get; }
    int CurrentRound { get; }
    IReadOnlyList<Combatant> InitiativeOrder { get; }
}
