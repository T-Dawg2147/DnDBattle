namespace DnDBattle.App.Configuration;

public sealed class CombatSettings
{
    public bool AutoRollInitiative { get; set; } = true;
    public bool EnableDeathSaves { get; set; } = true;
    public bool UseDiagonalMovement { get; set; } = false;
    public bool EnableFlanking { get; set; } = false;
    public bool EnableOpportunityAttacks { get; set; } = true;
    public bool TrackConcentration { get; set; } = true;
    public bool EnableCoverSystem { get; set; } = true;
    public int TurnTimerSeconds { get; set; } = 0; // 0 = disabled
}
