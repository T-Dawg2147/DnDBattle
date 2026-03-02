using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Core.Interfaces;
using DnDBattle.GameLogic.Dice;
using System.Collections.ObjectModel;

namespace DnDBattle.App.ViewModels;

public sealed partial class DiceViewModel : ObservableObject
{
    private readonly IDiceService _dice;
    private readonly DiceHistoryService _history;

    [ObservableProperty] private string _expression = "1d20";
    [ObservableProperty] private string _lastResult = string.Empty;

    public ObservableCollection<DiceHistoryEntry> History { get; } = new();

    public DiceViewModel(IDiceService dice, DiceHistoryService history)
    {
        _dice = dice;
        _history = history;
    }

    [RelayCommand]
    private void RollExpression()
    {
        if (string.IsNullOrWhiteSpace(Expression)) return;
        int result = _dice.ParseAndRoll(Expression);
        LastResult = $"{Expression} = {result}";
    }

    [RelayCommand]
    private void RollD4() { LastResult = $"d4: {_dice.Roll(4)}"; }

    [RelayCommand]
    private void RollD6() { LastResult = $"d6: {_dice.Roll(6)}"; }

    [RelayCommand]
    private void RollD8() { LastResult = $"d8: {_dice.Roll(8)}"; }

    [RelayCommand]
    private void RollD10() { LastResult = $"d10: {_dice.Roll(10)}"; }

    [RelayCommand]
    private void RollD12() { LastResult = $"d12: {_dice.Roll(12)}"; }

    [RelayCommand]
    private void RollD20() { LastResult = $"d20: {_dice.Roll(20)}"; }

    [RelayCommand]
    private void RollD100() { LastResult = $"d100: {_dice.Roll(100)}"; }
}
