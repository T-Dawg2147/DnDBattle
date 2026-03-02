using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.App.Configuration;

namespace DnDBattle.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly CombatSettings _combat;
    private readonly GridSettings _grid;
    private readonly LightingSettings _lighting;
    private readonly NetworkSettings _network;
    private readonly AccessibilitySettings _accessibility;

    [ObservableProperty] private bool _autoRollInitiative;
    [ObservableProperty] private bool _enableDeathSaves;
    [ObservableProperty] private bool _showGrid;
    [ObservableProperty] private double _cellSizePx;
    [ObservableProperty] private bool _enableFogOfWar;
    [ObservableProperty] private int _defaultPort;

    public SettingsViewModel(
        CombatSettings combat, GridSettings grid,
        LightingSettings lighting, NetworkSettings network,
        AccessibilitySettings accessibility)
    {
        _combat = combat; _grid = grid;
        _lighting = lighting; _network = network;
        _accessibility = accessibility;
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        AutoRollInitiative = _combat.AutoRollInitiative;
        EnableDeathSaves = _combat.EnableDeathSaves;
        ShowGrid = _grid.ShowGrid;
        CellSizePx = _grid.CellSizePx;
        EnableFogOfWar = _lighting.EnableFogOfWar;
        DefaultPort = _network.DefaultPort;
    }

    [RelayCommand]
    private void ApplySettings()
    {
        _combat.AutoRollInitiative = AutoRollInitiative;
        _combat.EnableDeathSaves = EnableDeathSaves;
        _grid.ShowGrid = ShowGrid;
        _grid.CellSizePx = CellSizePx;
        _lighting.EnableFogOfWar = EnableFogOfWar;
        _network.DefaultPort = DefaultPort;
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var defaults = new CombatSettings();
        AutoRollInitiative = defaults.AutoRollInitiative;
        EnableDeathSaves = defaults.EnableDeathSaves;
        ShowGrid = new GridSettings().ShowGrid;
        CellSizePx = new GridSettings().CellSizePx;
        EnableFogOfWar = new LightingSettings().EnableFogOfWar;
        DefaultPort = new NetworkSettings().DefaultPort;
    }
}
