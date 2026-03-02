using System.Text.Json;

namespace DnDBattle.Networking.Protocol;

public sealed class NetworkMessage
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static NetworkMessage Create<T>(string type, T payload, string senderId) => new()
    {
        Type = type,
        Payload = JsonSerializer.Serialize(payload),
        SenderId = senderId,
        Timestamp = DateTime.UtcNow
    };

    public T? GetPayload<T>() => JsonSerializer.Deserialize<T>(Payload);

    public string Serialize() => JsonSerializer.Serialize(this);
    public static NetworkMessage? Deserialize(string json) =>
        JsonSerializer.Deserialize<NetworkMessage>(json);
}

public static class MessageTypes
{
    public const string CombatantMoved = "combatant.moved";
    public const string CombatantDamaged = "combatant.damaged";
    public const string TurnChanged = "turn.changed";
    public const string EncounterStarted = "encounter.started";
    public const string EncounterEnded = "encounter.ended";
    public const string DiceRolled = "dice.rolled";
    public const string ChatMessage = "chat.message";
    public const string MapUpdated = "map.updated";
    public const string PlayerJoined = "player.joined";
    public const string PlayerLeft = "player.left";
    public const string Ping = "ping";
    public const string Pong = "pong";
}
