using System.Text.RegularExpressions;

namespace DnDBattle.Utils
{
    public class DiceResult
    {
        public int Total { get; set; }
        public List<int> Individual { get; set; } = new List<int>();
    }

    public static class DiceRoller
    {
        // Pre-compiled regex patterns - parsed once at startup, not on every roll!
        private static readonly Regex DiceExpressionRegex = new Regex(
            @"^(\d*)d(\d+)([+-]\d+)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ModifierRegex = new Regex(
            @"^(\d+)([+-]\d+)?$",
            RegexOptions.Compiled);

        private static readonly Random _rng = new Random();

        public static DiceResult RollExpression(string expr)
        {
            var res = new DiceResult();

            if (string.IsNullOrWhiteSpace(expr))
            {
                res.Total = 1;
                res.Individual.Add(1);
                return res;
            }

            expr = expr.Trim().ToLowerInvariant();

            // Fast path: plain number (no regex needed)
            if (int.TryParse(expr, out int plain))
            {
                res.Total = plain;
                res.Individual.Add(plain);
                return res;
            }

            // Use pre-compiled regex instead of Regex.Match()
            var m = DiceExpressionRegex.Match(expr);
            if (!m.Success)
            {
                // Fallback parsing for edge cases
                int dIndex = expr.IndexOf('d');
                if (dIndex > 0 && dIndex < expr.Length - 1)
                {
                    string countPart = expr.Substring(0, dIndex);
                    string rest = expr.Substring(dIndex + 1);

                    // Use pre-compiled regex
                    var subMatch = ModifierRegex.Match(rest);
                    if (subMatch.Success && int.TryParse(countPart, out int altCount))
                    {
                        int sidesSub = int.Parse(subMatch.Groups[1].Value);
                        int modSub = 0;
                        if (subMatch.Groups[2].Success)
                            int.TryParse(subMatch.Groups[2].Value, out modSub);
                        return RollDice(altCount, sidesSub, modSub);
                    }
                }

                res.Total = 1;
                res.Individual.Add(1);
                return res;
            }

            int count = 1;
            if (m.Groups[1].Success && !string.IsNullOrEmpty(m.Groups[1].Value))
                if (!int.TryParse(m.Groups[1].Value, out count) || count < 1)
                    count = 1;

            int sides = 20;
            if (m.Groups[2].Success && !string.IsNullOrEmpty(m.Groups[2].Value))
                if (!int.TryParse(m.Groups[2].Value, out sides) || sides < 1)
                    sides = 20;

            int mod = 0;
            if (m.Groups[3].Success && !string.IsNullOrEmpty(m.Groups[3].Value))
                int.TryParse(m.Groups[3].Value, out mod);

            return RollDice(count, sides, mod);
        }

        private static DiceResult RollDice(int count, int sides, int modifier)
        {
            var res = new DiceResult();

            // Clamp values to reasonable limits
            count = Math.Min(Math.Max(1, count), 100);
            sides = Math.Min(Math.Max(1, sides), 10000);

            for (int i = 0; i < count; i++)
            {
                int roll = _rng.Next(1, sides + 1);
                res.Individual.Add(roll);
            }
            res.Total = res.Individual.Sum() + modifier;
            return res;
        }
    }
}