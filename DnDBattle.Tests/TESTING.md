# DnDBattle Test Project Guide

## Overview

The **DnDBattle.Tests** project is an [xUnit](https://xunit.net/) test project that provides comprehensive unit tests for the DnDBattle backend. It tests all the core models, services, and utilities that power the application's combat system, dice rolling, encounter building, grid management, and more.

## Project Setup

### Requirements

- **.NET 6.0 SDK** (with Windows Desktop workload for WPF support)
- **Visual Studio 2022** or later (recommended) with the "Test Explorer" window
- Alternatively, you can use the **dotnet CLI** to run tests from the command line

### How to Build

```bash
# From the solution root directory
dotnet build DnDBattle.Tests/DnDBattle.Tests.csproj
```

### How to Run Tests

```bash
# Run all tests
dotnet test DnDBattle.Tests/DnDBattle.Tests.csproj

# Run with detailed output
dotnet test DnDBattle.Tests/DnDBattle.Tests.csproj --verbosity normal

# Run a specific test class
dotnet test DnDBattle.Tests/DnDBattle.Tests.csproj --filter "FullyQualifiedName~TokenTests"

# Run a specific test method
dotnet test DnDBattle.Tests/DnDBattle.Tests.csproj --filter "FullyQualifiedName~TakeDamage_ReducesHP"
```

In **Visual Studio**, you can also use:
- **Test > Run All Tests** (Ctrl+R, A)
- **Test Explorer** window to browse and run individual tests
- Right-click a test class or method and choose "Run Tests"

## Project Structure

The test project mirrors the structure of the main `DnDBattle` project:

```
DnDBattle.Tests/
├── DnDBattle.Tests.csproj          # Project file with xUnit references
├── Models/
│   ├── Combat/
│   │   ├── ActionTests.cs          # Tests for combat actions and damage type detection
│   │   ├── AttackResultTests.cs    # Tests for attack result formatting
│   │   ├── DamageTypeTests.cs      # Tests for damage type parsing, icons, colors
│   │   └── SavingThrowResultTests.cs # Tests for saving throw display
│   ├── Creatures/
│   │   └── TokenTests.cs           # Tests for Token (the core creature model)
│   ├── Effects/
│   │   ├── ConditionTests.cs       # Tests for D&D 5e condition flags
│   │   └── LightSourceTests.cs     # Tests for light source and cone calculations
│   ├── Encounters/
│   │   └── InitiativeEntryTests.cs # Tests for initiative tracking
│   ├── Environment/
│   │   └── WallTests.cs            # Tests for wall blocking, intersection, distance
│   ├── Spells/
│   │   └── SpellSlotsTests.cs      # Tests for spell slot management
│   └── Tiles/
│       ├── TileMapTests.cs         # Tests for tile map operations
│       └── TileTests.cs            # Tests for tiles, layers, metadata types
├── Services/
│   ├── Combat/
│   │   ├── CombatConditionHelperTests.cs  # Tests for condition effects on combat
│   │   └── InitiativeManagerTests.cs      # Tests for initiative rolling and ordering
│   ├── Dice/
│   │   └── DiceHistoryServiceTests.cs     # Tests for dice roll history and statistics
│   ├── Encounters/
│   │   └── EncounterBuilderServiceTests.cs # Tests for encounter difficulty calculations
│   ├── Grid/
│   │   ├── ElevationServiceTests.cs       # Tests for 2.5D elevation system
│   │   ├── GridServiceTests.cs            # Tests for grid-to-world coordinate conversion
│   │   ├── MeasurementServiceTests.cs     # Tests for distance/area measurements
│   │   └── SpatialIndexTests.cs           # Tests for light source spatial indexing
│   └── UI/
│       ├── CombatStatisticsServiceTests.cs # Tests for combat stat tracking
│       └── UndoManagerTests.cs             # Tests for undo/redo system
└── Utils/
    └── DiceRollerTests.cs          # Tests for dice expression parsing and rolling
```

## What Each Test File Covers

### Models

| Test File | What It Tests |
|-----------|--------------|
| **ActionTests.cs** | Combat action creation, damage type detection from names/descriptions (e.g., "Fire Bolt" → Fire damage), the `ToString()` display |
| **AttackResultTests.cs** | Attack result formatting — hit/miss, critical hits, fumbles, cover bonuses |
| **DamageTypeTests.cs** | The `DamageType` flags enum — parsing from strings, display names, icons, colors, combining multiple types |
| **SavingThrowResultTests.cs** | Saving throw display — success/failure, natural 1/20, auto-fail, legendary resistance |
| **TokenTests.cs** | The core `Token` model — HP management, damage with resistance/immunity/vulnerability, temp HP absorption, conditions, legendary actions, death saves, concentration, movement tracking, combat notes, tags |
| **ConditionTests.cs** | D&D 5e conditions — flags combination, display strings, exhaustion levels, icons, descriptions |
| **LightSourceTests.cs** | Light sources — radius calculation, directional cone detection |
| **InitiativeEntryTests.cs** | Initiative entries — roll calculation, display name, status text, round reset |
| **WallTests.cs** | Walls — light/sight/movement blocking by type, door open/close state, line intersection, distance-to-point calculation |
| **SpellSlotsTests.cs** | Spell slot management — use/restore, long rest, caster level progression (full/half/third caster), display formatting |
| **TileMapTests.cs** | Tile map — add/remove tiles, bounds checking, grid scale changes, map notes, speed-in-squares conversion |
| **TileTests.cs** | Tiles — metadata, z-index, tile layers, tile metadata types |

### Services

| Test File | What It Tests |
|-----------|--------------|
| **CombatConditionHelperTests.cs** | How conditions affect combat — movement restrictions, attack advantage/disadvantage, auto-fail saves, auto-crit detection, cover AC/DEX bonuses |
| **InitiativeManagerTests.cs** | Initiative system — rolling for all participants, turn ordering, cycling turns, reset |
| **DiceHistoryServiceTests.cs** | Dice history — recording rolls, filtering by roller/type, statistics (crit rate, averages), history size limits |
| **EncounterBuilderServiceTests.cs** | Encounter balancing — CR-to-XP conversion, encounter multipliers, party thresholds, difficulty calculation |
| **ElevationServiceTests.cs** | 2.5D elevation — terrain elevation, token elevation, 3D distance, falling damage, line-of-sight |
| **GridServiceTests.cs** | Grid coordinate conversion — grid-to-world and world-to-grid transformations |
| **MeasurementServiceTests.cs** | Map measurements — distance (with D&D diagonal rules), area (rectangle/radius/polygon), visibility toggling |
| **SpatialIndexTests.cs** | Spatial indexing — light source indexing and bounds-based querying |
| **CombatStatisticsServiceTests.cs** | Combat statistics — attack tracking, damage dealt/taken, healing, kills, saving throws, combat summaries |
| **UndoManagerTests.cs** | Undo/redo system — recording actions, undo, redo, stack limits, state change events |

### Utils

| Test File | What It Tests |
|-----------|--------------|
| **DiceRollerTests.cs** | Dice expression parsing — `1d20`, `2d6+3`, `d20-3`, plain numbers, invalid inputs, large dice counts |

## How to Add New Tests

### 1. Create a New Test File

Create a new `.cs` file in the appropriate directory under `DnDBattle.Tests/`. The directory structure should mirror the main project.

For example, to test a new service at `DnDBattle/Services/Pathfinding/PathfindingService.cs`, create:
```
DnDBattle.Tests/Services/Pathfinding/PathfindingServiceTests.cs
```

### 2. Write Your Test Class

```csharp
using DnDBattle.Services.Pathfinding; // Reference the class you're testing

namespace DnDBattle.Tests.Services.Pathfinding
{
    public class PathfindingServiceTests
    {
        [Fact]
        public void FindPath_StraightLine_ReturnsShortestPath()
        {
            // Arrange - set up the test
            var service = new PathfindingService();

            // Act - call the method being tested
            var path = service.FindPath(0, 0, 5, 0);

            // Assert - verify the result
            Assert.NotNull(path);
            Assert.Equal(6, path.Count); // Start + 5 steps
        }

        [Theory]
        [InlineData(0, 0, 1, 0, 1)]    // Adjacent
        [InlineData(0, 0, 3, 4, 5)]    // 3-4-5 triangle
        public void CalculateDistance_ReturnsCorrectDistance(
            int x1, int y1, int x2, int y2, int expected)
        {
            var service = new PathfindingService();
            Assert.Equal(expected, service.CalculateDistance(x1, y1, x2, y2));
        }
    }
}
```

### 3. Key xUnit Attributes

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[Fact]` | A single test case | `[Fact] public void MyTest() { ... }` |
| `[Theory]` | A parameterized test (runs multiple times with different data) | Used with `[InlineData]` |
| `[InlineData]` | Provides data for a `[Theory]` test | `[InlineData(1, 2, 3)]` |

### 4. Common Assert Methods

```csharp
Assert.Equal(expected, actual);           // Value equality
Assert.NotEqual(unexpected, actual);      // Not equal
Assert.True(condition);                   // Boolean true
Assert.False(condition);                  // Boolean false
Assert.Null(value);                       // Is null
Assert.NotNull(value);                    // Not null
Assert.Contains(item, collection);        // Collection contains
Assert.DoesNotContain(item, collection);  // Collection doesn't contain
Assert.Contains("substr", fullString);    // String contains
Assert.Empty(collection);                 // Collection is empty
Assert.Single(collection);               // Collection has exactly 1 item
Assert.InRange(value, low, high);         // Value in range
Assert.Throws<Exception>(() => ...);      // Exception thrown
```

### 5. Test Naming Convention

Tests in this project follow the pattern:
```
MethodName_Scenario_ExpectedResult
```

Examples:
- `TakeDamage_WithResistance_HalvesDamage`
- `RollExpression_1d20Plus5_AddsModifier`
- `GetXPForCR_NullOrEmpty_ReturnsZero`

### 6. Test Organization Tips

- **One test class per production class** — keeps things organized
- **Use `#region` blocks** to group related tests within a class
- **Create helper methods** like `CreateTestToken()` to reduce boilerplate
- **Test edge cases** — null inputs, empty strings, boundary values, max/min limits

## Important Notes

### WPF Dependency

This test project targets `net6.0-windows` and includes WPF support because the main DnDBattle project uses WPF types like `System.Windows.Point`, `System.Windows.Media.Color`, and `ImageSource`. This means:

- **Tests must be run on Windows** (they won't run on Linux/macOS)
- The Visual Studio Test Explorer is the recommended way to run tests
- If using the CLI, make sure you have the .NET 6.0 Windows Desktop runtime installed

### Static State

Some classes like `UndoManager` and `Options` use static state. Tests that modify static state should:
- Reset the state in the constructor or a setup method
- Use `try/finally` blocks to restore original values after test

Example from `ElevationServiceTests.cs`:
```csharp
[Fact]
public void SetTerrainElevation_SetsCorrectly()
{
    Options.EnableElevationSystem = true;
    try
    {
        var service = CreateService();
        service.SetTerrainElevation(5, 5, 30);
        Assert.Equal(30, service.GetTerrainElevation(5, 5));
    }
    finally
    {
        Options.EnableElevationSystem = false;
    }
}
```

### Randomness in Tests

Some methods like `DiceRoller.RollExpression` produce random results. Tests for these methods:
- Use `Assert.InRange()` to verify results are within expected bounds
- Run multiple iterations to increase confidence
- Test deterministic aspects (number of dice, modifier application) rather than specific random values

## Quick Reference

| Command | Description |
|---------|-------------|
| `dotnet build DnDBattle.Tests` | Build the test project |
| `dotnet test DnDBattle.Tests` | Run all tests |
| `dotnet test --filter "TokenTests"` | Run tests matching a name |
| `dotnet test --verbosity detailed` | Run with full output |
| `dotnet test --collect:"XPlat Code Coverage"` | Run with code coverage |

## Further Reading

- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/visual-studio)
- [.NET Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Assert Methods Reference](https://xunit.net/docs/comparisons)
