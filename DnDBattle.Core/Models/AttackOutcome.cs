using DnDBattle.Core.Enums;

namespace DnDBattle.Core.Models;

public record AttackOutcome(
    bool Hit,
    bool IsCritical,
    int AttackRoll,
    int TotalDamage,
    DamageType DamageType,
    string Description
);
