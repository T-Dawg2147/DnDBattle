using DnDBattle.Core.Enums;

namespace DnDBattle.Core.Models;

public record SpellDefinition(
    Guid Id,
    string Name,
    SpellSchool School,
    int Level,
    string CastingTime,
    string Range,
    string Components,
    string Duration,
    bool RequiresConcentration,
    AreaEffectShape? AreaShape,
    double? AreaSizeFeet,
    string DamageDice,
    DamageType DamageType,
    Ability? SaveAbility,
    int SaveDC,
    string Description
);
