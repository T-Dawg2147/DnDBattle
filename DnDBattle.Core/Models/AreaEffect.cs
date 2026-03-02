using DnDBattle.Core.Enums;

namespace DnDBattle.Core.Models;

public record AreaEffect(
    Guid Id,
    string Name,
    AreaEffectShape Shape,
    double RadiusFeet,
    System.Windows.Point Center,
    string DamageDice,
    DamageType DamageType,
    Ability? SaveAbility,
    int SaveDC,
    int DurationRounds,
    int RemainingRounds,
    Guid? CasterId
);
