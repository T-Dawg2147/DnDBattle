using System.IO;
using DnDBattle.App.Configuration;
using DnDBattle.App.ViewModels;
using DnDBattle.Core.Interfaces;
using DnDBattle.Data;
using DnDBattle.Data.Repositories;
using DnDBattle.Data.Storage;
using DnDBattle.GameLogic.Combat;
using DnDBattle.GameLogic.Dice;
using DnDBattle.GameLogic.Effects;
using DnDBattle.GameLogic.Encounters;
using DnDBattle.GameLogic.Movement;
using DnDBattle.GameLogic.Spells;
using DnDBattle.MapEngine.Environment;
using DnDBattle.MapEngine.Grid;
using DnDBattle.MapEngine.Lighting;
using DnDBattle.MapEngine.Rendering;
using DnDBattle.MapEngine.Tiles;
using DnDBattle.MapEngine.Vision;
using DnDBattle.Networking.Chat;
using DnDBattle.Networking.Client;
using DnDBattle.Networking.Server;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DnDBattle.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Services = BuildServiceProvider();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // Configuration / Settings
        services.AddSingleton<CombatSettings>();
        services.AddSingleton<GridSettings>();
        services.AddSingleton<LightingSettings>();
        services.AddSingleton<NetworkSettings>();
        services.AddSingleton<AccessibilitySettings>();
        services.AddSingleton<AutosaveSettings>();

        // Data Layer
        services.AddSingleton(sp =>
        {
            var saveDir = sp.GetRequiredService<AutosaveSettings>().SaveDirectory;
            Directory.CreateDirectory(saveDir);
            return new DatabaseContext(Path.Combine(saveDir, "dndbattle.db"));
        });
        services.AddSingleton<ICreatureRepository, CreatureRepository>();
        services.AddSingleton<IPersistenceService>(sp =>
        {
            var db = sp.GetRequiredService<DatabaseContext>();
            var settings = sp.GetRequiredService<AutosaveSettings>();
            return new PersistenceService(db, settings.SaveDirectory);
        });

        // Game Logic
        services.AddSingleton<IDiceService, DiceService>();
        services.AddSingleton<DiceHistoryService>();
        services.AddSingleton<ICombatService, CombatService>();
        services.AddSingleton<IEncounterService, InitiativeManager>();
        services.AddSingleton<IEffectService, EffectService>();
        services.AddSingleton<ISpellService, SpellService>();
        services.AddSingleton<ConcentrationTracker>();
        services.AddSingleton<EncounterBuilder>();
        services.AddSingleton<PathfindingService>();

        // Map Engine
        services.AddSingleton<IGridService>(sp =>
        {
            var settings = sp.GetRequiredService<GridSettings>();
            var pathfinding = sp.GetRequiredService<PathfindingService>();
            return new GridService(pathfinding, settings.CellSizePx, settings.GridType);
        });
        services.AddSingleton<ITileService, TileService>();
        services.AddSingleton<TilePalette>(_ => TilePalette.CreateDefault());
        services.AddSingleton<LightingService>();
        services.AddSingleton<LineOfSightService>();
        services.AddSingleton<FogOfWarService>();
        services.AddSingleton<WallService>();
        services.AddSingleton<ElevationService>();
        services.AddSingleton<MapRenderer>();

        // Networking — default to client; host switches at runtime
        services.AddTransient<GameServer>();
        services.AddTransient<GameClient>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<PlayerClientViewModel>();
        services.AddTransient<CombatViewModel>();
        services.AddTransient<CreatureViewModel>();
        services.AddTransient<EncounterViewModel>();
        services.AddTransient<DiceViewModel>();
        services.AddTransient<SpellViewModel>();
        services.AddTransient<MapViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
