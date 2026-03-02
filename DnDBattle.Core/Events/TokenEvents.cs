using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DnDBattle.Core.Events;

public sealed class TokenMovedMessage(Guid CombatantId, System.Windows.Point OldPosition, System.Windows.Point NewPosition)
    : ValueChangedMessage<System.Windows.Point>(NewPosition)
{
    public Guid CombatantId { get; } = CombatantId;
    public System.Windows.Point OldPosition { get; } = OldPosition;
}

public sealed class TokenSelectedMessage(Guid? CombatantId)
    : ValueChangedMessage<Guid?>(CombatantId)
{
    public Guid? CombatantId { get; } = CombatantId;
}

public sealed class TokenAddedMessage(Guid CombatantId)
    : ValueChangedMessage<Guid>(CombatantId)
{
    public Guid CombatantId { get; } = CombatantId;
}

public sealed class TokenRemovedMessage(Guid CombatantId)
    : ValueChangedMessage<Guid>(CombatantId)
{
    public Guid CombatantId { get; } = CombatantId;
}
