namespace DnDBattle.Core.Extensions;

public static class DiceExtensions
{
    private static readonly Random Rng = Random.Shared;

    public static int Roll(this Enums.DiceType die) => Rng.Next(1, (int)die + 1);

    public static string ToNotation(this Enums.DiceType die) => $"d{(int)die}";
}
