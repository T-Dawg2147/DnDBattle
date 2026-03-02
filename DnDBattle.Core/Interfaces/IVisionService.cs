namespace DnDBattle.Core.Interfaces;

public interface IVisionService
{
    bool HasLineOfSight(System.Windows.Point from, System.Windows.Point to);
    double GetVisibleRadius(Models.Combatant viewer);
    bool IsInFogOfWar(int col, int row, Guid viewerId);
    void RevealArea(int col, int row, double radiusFeet, Guid viewerId);
}
