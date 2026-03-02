using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DnDBattle.Core.Events;
using DnDBattle.Core.Interfaces;
using DnDBattle.Core.Models;
using System.Collections.ObjectModel;

namespace DnDBattle.App.ViewModels;

public sealed partial class MainViewModel : ObservableRecipient
{
    private readonly IEncounterService _encounter;
    private readonly ICombatService _combat;
    private readonly IDiceService _dice;
    private readonly IPersistenceService _persistence;

    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isEncounterActive;
    [ObservableProperty] private Combatant? _selectedCombatant;
    [ObservableProperty] private Combatant? _activeCombatant;
    [ObservableProperty] private int _currentRound;
    [ObservableProperty] private bool _isMultiplayerActive;
    [ObservableProperty] private ObservableObject? _currentView;

    public ObservableCollection<Combatant> Combatants { get; } = new();

    public IReadOnlyList<Combatant> InitiativeOrder => _encounter.InitiativeOrder;

    public MainViewModel(
        IEncounterService encounter,
        ICombatService combat,
        IDiceService dice,
        IPersistenceService persistence,
        IMessenger messenger) : base(messenger)
    {
        _encounter = encounter;
        _combat = combat;
        _dice = dice;
        _persistence = persistence;

        IsActive = true;
    }

    protected override void OnActivated()
    {
        Messenger.Register<MainViewModel, TurnChangedMessage>(this, (vm, msg) =>
        {
            vm.ActiveCombatant = vm.Combatants.FirstOrDefault(c => c.Id == msg.ActiveCombatantId);
            vm.CurrentRound = msg.Value;
        });
        Messenger.Register<MainViewModel, EncounterStartedMessage>(this, (vm, msg) =>
        {
            vm.IsEncounterActive = true;
            vm.StatusMessage = $"Encounter started — Round 1";
        });
        Messenger.Register<MainViewModel, EncounterEndedMessage>(this, (vm, msg) =>
        {
            vm.IsEncounterActive = false;
            vm.StatusMessage = $"Encounter ended after {msg.Value} rounds.";
        });
        Messenger.Register<MainViewModel, CreatureDiedMessage>(this, (vm, msg) =>
        {
            var dead = vm.Combatants.FirstOrDefault(c => c.Id == msg.Value);
            if (dead != null)
                vm.StatusMessage = $"{dead.Name} has fallen!";
        });
    }

    [RelayCommand]
    private void StartEncounter()
    {
        if (Combatants.Count == 0)
        {
            StatusMessage = "Add combatants before starting an encounter.";
            return;
        }
        _encounter.StartEncounter(Combatants);
        ActiveCombatant = _encounter.ActiveCombatant;
        CurrentRound = _encounter.CurrentRound;
        StatusMessage = $"Encounter started! {ActiveCombatant?.Name}'s turn.";
    }

    [RelayCommand]
    private void NextTurn()
    {
        if (!IsEncounterActive) return;
        _encounter.NextTurn();
        ActiveCombatant = _encounter.ActiveCombatant;
        CurrentRound = _encounter.CurrentRound;
        StatusMessage = $"Round {CurrentRound} — {ActiveCombatant?.Name}'s turn.";
    }

    [RelayCommand]
    private void EndEncounter()
    {
        _encounter.EndEncounter();
        IsEncounterActive = false;
        ActiveCombatant = null;
    }

    [RelayCommand]
    private void AddCombatant()
    {
        var combatant = new Combatant
        {
            Name = $"Creature {Combatants.Count + 1}",
            MaxHitPoints = 30,
            CurrentHitPoints = 30,
            ArmorClass = 13
        };
        Combatants.Add(combatant);
        StatusMessage = $"Added {combatant.Name}.";
    }

    [RelayCommand]
    private void RemoveCombatant()
    {
        if (SelectedCombatant != null)
        {
            Combatants.Remove(SelectedCombatant);
            SelectedCombatant = null;
        }
    }

    [RelayCommand]
    private void AttackSelected()
    {
        if (ActiveCombatant is null || SelectedCombatant is null) return;
        if (ActiveCombatant.Id == SelectedCombatant.Id) return;

        var outcome = _combat.ResolveAttack(ActiveCombatant, SelectedCombatant);
        StatusMessage = outcome.Description;
    }

    [RelayCommand]
    private async Task SaveEncounterAsync()
    {
        if (Combatants.Count == 0) return;
        var snapshot = BuildSnapshot();
        // Sanitize name to prevent path traversal: keep only safe filename characters
        var safeName = string.Concat(snapshot.Name
            .Where(c => char.IsLetterOrDigit(c) || c is '_' or '-' or ' '))
            .Trim().Replace(' ', '_');
        if (string.IsNullOrEmpty(safeName)) safeName = "Encounter";
        var fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.dnd";
        var saveDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DnDBattle");
        Directory.CreateDirectory(saveDir);
        var path = Path.Combine(saveDir, fileName);
        await _persistence.SaveEncounterAsync(snapshot, path);
        StatusMessage = $"Saved to {path}";
    }

    private EncounterSnapshot BuildSnapshot()
    {
        var snaps = Combatants.Select(c => new CombatantSnapshot(
            c.Id, c.Name, c.MaxHitPoints, c.CurrentHitPoints,
            c.TemporaryHitPoints, c.ArmorClass, c.InitiativeRoll,
            c.PositionX, c.PositionY, c.IsPlayer, c.ImagePath)).ToList();
        return new EncounterSnapshot(Guid.NewGuid(), "Encounter", DateTime.Now,
            snaps, CurrentRound, ActiveCombatant?.Id, null);
    }
}
