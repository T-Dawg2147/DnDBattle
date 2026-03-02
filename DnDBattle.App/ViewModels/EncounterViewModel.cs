using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using DnDBattle.GameLogic.Encounters;
using System.Collections.ObjectModel;

namespace DnDBattle.App.ViewModels;

public sealed partial class EncounterViewModel : ObservableObject
{
    private readonly IEncounterService _encounter;
    private readonly EncounterBuilder _builder;

    [ObservableProperty] private string _encounterName = "New Encounter";
    [ObservableProperty] private int _currentRound;
    [ObservableProperty] private Combatant? _activeCombatant;

    public ObservableCollection<Combatant> Combatants { get; } = new();

    public EncounterViewModel(IEncounterService encounter, EncounterBuilder builder)
    {
        _encounter = encounter;
        _builder = builder;
    }

    [RelayCommand]
    private void StartEncounter()
    {
        _encounter.StartEncounter(Combatants);
        ActiveCombatant = _encounter.ActiveCombatant;
        CurrentRound = _encounter.CurrentRound;
    }

    [RelayCommand]
    private void NextTurn()
    {
        _encounter.NextTurn();
        ActiveCombatant = _encounter.ActiveCombatant;
        CurrentRound = _encounter.CurrentRound;
    }

    [RelayCommand]
    private void EndEncounter() => _encounter.EndEncounter();
}
