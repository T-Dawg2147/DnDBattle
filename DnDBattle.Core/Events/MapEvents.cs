using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DnDBattle.Core.Events;

public sealed class MapLoadedMessage(Guid MapId, string MapName)
    : ValueChangedMessage<string>(MapName)
{
    public Guid MapId { get; } = MapId;
}

public sealed class TilePlacedMessage(int Col, int Row, int Layer)
    : ValueChangedMessage<(int, int, int)>((Col, Row, Layer))
{
    public int Col { get; } = Col;
    public int Row { get; } = Row;
    public int Layer { get; } = Layer;
}
