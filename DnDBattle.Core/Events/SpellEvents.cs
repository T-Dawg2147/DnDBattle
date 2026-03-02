using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DnDBattle.Core.Events;

public sealed class SpellCastMessage(Guid CasterId, Guid SpellId, string SpellName)
    : ValueChangedMessage<string>(SpellName)
{
    public Guid CasterId { get; } = CasterId;
    public Guid SpellId { get; } = SpellId;
}

public sealed class ConcentrationBrokenMessage(Guid CombatantId, string SpellName)
    : ValueChangedMessage<string>(SpellName)
{
    public Guid CombatantId { get; } = CombatantId;
}
