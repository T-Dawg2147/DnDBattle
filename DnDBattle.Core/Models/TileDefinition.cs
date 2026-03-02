namespace DnDBattle.Core.Models;

public record TileDefinition(
    Guid Id,
    string Name,
    string ImagePath,
    bool IsWalkable = true,
    bool BlocksVision = false,
    bool IsWall = false,
    bool IsDoor = false,
    bool IsDifficultTerrain = false,
    double MovementCostMultiplier = 1.0
);
