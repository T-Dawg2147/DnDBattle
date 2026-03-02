using DnDBattle.Core.Enums;

namespace DnDBattle.GameLogic.Dice;

public sealed class DiceHistoryService
{
    private readonly List<DiceHistoryEntry> _history = new();
    private readonly int _maxEntries;

    public DiceHistoryService(int maxEntries = 100) => _maxEntries = maxEntries;

    public IReadOnlyList<DiceHistoryEntry> History => _history;

    public void Record(DiceHistoryEntry entry)
    {
        _history.Insert(0, entry);
        if (_history.Count > _maxEntries) _history.RemoveAt(_history.Count - 1);
    }

    public void Clear() => _history.Clear();

    public double AverageResult(DiceType dieType) =>
        _history.Where(e => e.DieType == dieType).Select(e => (double)e.Result)
               .DefaultIfEmpty(0).Average();
}
