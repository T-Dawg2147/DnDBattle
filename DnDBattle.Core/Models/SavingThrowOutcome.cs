using DnDBattle.Core.Enums;

namespace DnDBattle.Core.Models;

public record SavingThrowOutcome(
    bool Succeeded,
    int Roll,
    int Total,
    int DC,
    Ability Ability
);
