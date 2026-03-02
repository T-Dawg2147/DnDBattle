using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using System.Collections.ObjectModel;

namespace DnDBattle.App.ViewModels;

public sealed partial class CreatureViewModel : ObservableObject
{
    private readonly ICreatureRepository _repository;

    [ObservableProperty] private CreatureRecord? _selectedCreature;
    [ObservableProperty] private string _searchText = string.Empty;

    public ObservableCollection<CreatureRecord> Creatures { get; } = new();

    public CreatureViewModel(ICreatureRepository repository) => _repository = repository;

    [RelayCommand]
    private async Task LoadCreaturesAsync()
    {
        var all = await _repository.GetAllAsync();
        Creatures.Clear();
        foreach (var c in all) Creatures.Add(c);
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedCreature is null) return;
        await _repository.DeleteAsync(SelectedCreature.Id);
        Creatures.Remove(SelectedCreature);
        SelectedCreature = null;
    }
}
