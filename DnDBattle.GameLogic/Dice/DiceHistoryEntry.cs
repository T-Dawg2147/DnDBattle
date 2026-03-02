using DnDBattle.Core.Enums;

namespace DnDBattle.GameLogic.Dice;

public sealed record DiceHistoryEntry(
    DateTime RolledAt,
    string Expression,
    int Result,
    DiceType DieType,
    int Count,
    IReadOnlyList<int> IndividualRolls,
    string? Context = null
);
