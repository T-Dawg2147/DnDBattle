using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;

namespace DnDBattle.App.ViewModels;

public sealed partial class CombatViewModel : ObservableObject
{
    private readonly ICombatService _combat;
    private readonly IDiceService _dice;

    [ObservableProperty] private string _lastResult = string.Empty;
    [ObservableProperty] private Combatant? _attacker;
    [ObservableProperty] private Combatant? _target;

    public CombatViewModel(ICombatService combat, IDiceService dice)
    {
        _combat = combat;
        _dice = dice;
    }

    [RelayCommand]
    private void ResolveAttack()
    {
        if (Attacker is null || Target is null) return;
        var outcome = _combat.ResolveAttack(Attacker, Target);
        LastResult = outcome.Description;
    }

    [RelayCommand]
    private void RollD20() => LastResult = $"d20: {_dice.Roll(20)}";

    [RelayCommand]
    private void RollDamage(string expression) =>
        LastResult = $"{expression}: {_dice.ParseAndRoll(expression)}";
}
