namespace DnDBattle.Core.Interfaces;

public interface IDiceService
{
    int Roll(int sides);
    int Roll(int count, int sides);
    int ParseAndRoll(string expression);
    IReadOnlyList<int> RollAll(int count, int sides);
}
