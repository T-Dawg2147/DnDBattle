using DnDBattle.Core.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DnDBattle.Core.Events;

public sealed class CreatureDamagedMessage(Guid CombatantId, int Damage, Enums.DamageType DamageType)
    : ValueChangedMessage<int>(Damage)
{
    public Guid CombatantId { get; } = CombatantId;
    public Enums.DamageType DamageType { get; } = DamageType;
}

public sealed class CreatureHealedMessage(Guid CombatantId, int Amount)
    : ValueChangedMessage<int>(Amount)
{
    public Guid CombatantId { get; } = CombatantId;
}

public sealed class CreatureDiedMessage(Guid CombatantId)
    : ValueChangedMessage<Guid>(CombatantId)
{
    public Guid CombatantId { get; } = CombatantId;
}

public sealed class TurnChangedMessage(Guid ActiveCombatantId, int Round)
    : ValueChangedMessage<int>(Round)
{
    public Guid ActiveCombatantId { get; } = ActiveCombatantId;
}

public sealed class EncounterStartedMessage(IReadOnlyList<Combatant> InitiativeOrder)
    : ValueChangedMessage<IReadOnlyList<Combatant>>(InitiativeOrder)
{
    public IReadOnlyList<Combatant> InitiativeOrder { get; } = InitiativeOrder;
}

public sealed class EncounterEndedMessage(int TotalRounds)
    : ValueChangedMessage<int>(TotalRounds)
{
    public int TotalRounds { get; } = TotalRounds;
}
