namespace DnDBattle.Core.Interfaces;

public interface ISoundService
{
    void PlayDiceRoll();
    void PlayAttack();
    void PlaySpellCast();
    void PlayCriticalHit();
    void PlayCreatureDeath();
    bool SoundEnabled { get; set; }
    double Volume { get; set; }
}
