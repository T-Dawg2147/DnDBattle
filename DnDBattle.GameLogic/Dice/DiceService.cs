using DnDBattle.Core.Interfaces;
using System.Text.RegularExpressions;

namespace DnDBattle.GameLogic.Dice;

public sealed class DiceService : IDiceService
{
    private static readonly Random Rng = Random.Shared;
    private static readonly Regex DicePattern =
        new(@"(?:(\d+)?d(\d+))([+-]\d+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public int Roll(int sides) => sides <= 0 ? 0 : Rng.Next(1, sides + 1);

    public int Roll(int count, int sides)
    {
        if (count <= 0 || sides <= 0) return 0;
        var total = 0;
        for (var i = 0; i < count; i++) total += Roll(sides);
        return total;
    }

    public IReadOnlyList<int> RollAll(int count, int sides)
    {
        var results = new int[Math.Max(count, 0)];
        for (var i = 0; i < results.Length; i++) results[i] = Roll(sides);
        return results;
    }

    public int ParseAndRoll(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return 0;

        if (int.TryParse(expression.Trim(), out var flat)) return flat;

        var match = DicePattern.Match(expression.Trim());
        if (!match.Success) return 0;

        int count = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1;
        int sides = int.Parse(match.Groups[2].Value);
        int modifier = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        return Roll(count, sides) + modifier;
    }
}
