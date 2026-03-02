namespace DnDBattle.MapEngine.Environment;

public sealed class ElevationService
{
    private readonly Dictionary<(int, int), int> _elevations = new();

    public void SetElevation(int col, int row, int elevationFeet) =>
        _elevations[(col, row)] = elevationFeet;

    public int GetElevation(int col, int row) =>
        _elevations.GetValueOrDefault((col, row), 0);

    public bool HasHigherGround(int attackerCol, int attackerRow, int targetCol, int targetRow) =>
        GetElevation(attackerCol, attackerRow) > GetElevation(targetCol, targetRow);
}
