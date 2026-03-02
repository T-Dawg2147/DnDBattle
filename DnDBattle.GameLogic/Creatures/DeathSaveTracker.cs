namespace DnDBattle.GameLogic.Creatures;

public sealed class DeathSaveTracker
{
    public int Successes { get; private set; }
    public int Failures { get; private set; }
    public bool IsStabilized => Successes >= 3;
    public bool IsDead => Failures >= 3;

    public void RecordSuccess()
    {
        if (!IsStabilized && !IsDead) Successes++;
    }

    public void RecordFailure()
    {
        if (!IsStabilized && !IsDead) Failures++;
    }

    public void Reset() { Successes = 0; Failures = 0; }
}
