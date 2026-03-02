using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using System.Collections.ObjectModel;

namespace DnDBattle.App.ViewModels;

public sealed partial class SpellViewModel : ObservableObject
{
    private readonly ISpellService _spells;

    [ObservableProperty] private SpellDefinition? _selectedSpell;
    [ObservableProperty] private Combatant? _caster;
    [ObservableProperty] private string _lastCastResult = string.Empty;

    public ObservableCollection<SpellDefinition> AvailableSpells { get; } = new();

    public SpellViewModel(ISpellService spells) => _spells = spells;

    [RelayCommand]
    private void LoadSpells()
    {
        if (Caster is null) return;
        AvailableSpells.Clear();
        foreach (var s in _spells.GetAvailableSpells(Caster))
            AvailableSpells.Add(s);
    }

    [RelayCommand]
    private void CastOnTarget(Combatant target)
    {
        if (SelectedSpell is null || Caster is null) return;
        bool success = _spells.TryCastSpell(Caster, SelectedSpell, new[] { target });
        LastCastResult = success
            ? $"{Caster.Name} casts {SelectedSpell.Name}!"
            : $"Not enough spell slots!";
    }
}
