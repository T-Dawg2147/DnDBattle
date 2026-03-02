using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using DnDBattle.Core.Events;
using CommunityToolkit.Mvvm.Messaging;

namespace DnDBattle.GameLogic.Combat;

public sealed class InitiativeManager : IEncounterService
{
    private readonly IDiceService _dice;
    private readonly IMessenger _messenger;
    private readonly List<Combatant> _order = new();
    private int _currentIndex;

    public InitiativeManager(IDiceService dice, IMessenger messenger)
    {
        _dice = dice;
        _messenger = messenger;
    }

    public Combatant? ActiveCombatant =>
        _order.Count > 0 && _currentIndex < _order.Count ? _order[_currentIndex] : null;

    public int CurrentRound { get; private set; }

    public IReadOnlyList<Combatant> InitiativeOrder => _order;

    public void StartEncounter(IEnumerable<Combatant> combatants)
    {
        _order.Clear();
        _currentIndex = 0;
        CurrentRound = 1;

        foreach (var c in combatants)
        {
            c.InitiativeRoll = _dice.Roll(20) + c.GetAbilityModifier(Core.Enums.Ability.Dexterity);
            _order.Add(c);
        }

        _order.Sort((a, b) => b.InitiativeRoll.CompareTo(a.InitiativeRoll));
        _messenger.Send(new EncounterStartedMessage(_order));
    }

    public void NextTurn()
    {
        _currentIndex++;
        if (_currentIndex >= _order.Count)
        {
            _currentIndex = 0;
            CurrentRound++;
        }

        while (_order.Count > 0 && ActiveCombatant is { IsAlive: false })
        {
            _currentIndex = (_currentIndex + 1) % _order.Count;
        }

        if (ActiveCombatant is { } active)
            _messenger.Send(new TurnChangedMessage(active.Id, CurrentRound));
    }

    public void EndEncounter()
    {
        var rounds = CurrentRound;
        _order.Clear();
        _currentIndex = 0;
        CurrentRound = 0;
        _messenger.Send(new EncounterEndedMessage(rounds));
    }
}
