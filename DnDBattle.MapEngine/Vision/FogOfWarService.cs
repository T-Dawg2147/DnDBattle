namespace DnDBattle.MapEngine.Vision;

public sealed class FogOfWarService
{
    private readonly Dictionary<Guid, HashSet<(int, int)>> _revealedCells = new();

    public void RevealArea(Guid viewerId, int centerCol, int centerRow, int radiusCells)
    {
        if (!_revealedCells.TryGetValue(viewerId, out var revealed))
        {
            revealed = new HashSet<(int, int)>();
            _revealedCells[viewerId] = revealed;
        }

        for (int dc = -radiusCells; dc <= radiusCells; dc++)
            for (int dr = -radiusCells; dr <= radiusCells; dr++)
                if (dc * dc + dr * dr <= radiusCells * radiusCells)
                    revealed.Add((centerCol + dc, centerRow + dr));
    }

    public bool IsRevealed(Guid viewerId, int col, int row) =>
        _revealedCells.TryGetValue(viewerId, out var cells) && cells.Contains((col, row));

    public bool IsRevealedByAny(int col, int row) =>
        _revealedCells.Values.Any(cells => cells.Contains((col, row)));

    public void ClearAll() => _revealedCells.Clear();
    public void ClearFor(Guid viewerId) => _revealedCells.Remove(viewerId);
}
