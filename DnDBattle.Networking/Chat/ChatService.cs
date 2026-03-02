using DnDBattle.Networking.Protocol;

namespace DnDBattle.Networking.Chat;

public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string SenderName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; init; } = DateTime.Now;
    public bool IsSystemMessage { get; init; }
    public string? DiceRollResult { get; init; }
}

public sealed class ChatService
{
    private readonly List<ChatMessage> _messages = new();
    private readonly Core.Interfaces.INetworkingService _network;

    public ChatService(Core.Interfaces.INetworkingService network)
    {
        _network = network;
        _network.MessageReceived += OnNetworkMessage;
    }

    public IReadOnlyList<ChatMessage> Messages => _messages;
    public event EventHandler<ChatMessage>? NewMessage;

    public async Task SendChatAsync(string senderName, string text, CancellationToken ct = default)
    {
        var msg = new ChatMessage { SenderName = senderName, Text = text };
        AddMessage(msg);
        var netMsg = NetworkMessage.Create(MessageTypes.ChatMessage,
            new { senderName, text }, senderName);
        await _network.SendMessageAsync(netMsg.Serialize(), ct);
    }

    public void AddSystemMessage(string text)
    {
        AddMessage(new ChatMessage { Text = text, IsSystemMessage = true });
    }

    private void AddMessage(ChatMessage msg)
    {
        _messages.Add(msg);
        NewMessage?.Invoke(this, msg);
    }

    private void OnNetworkMessage(object? sender, string json)
    {
        var msg = NetworkMessage.Deserialize(json);
        if (msg?.Type != MessageTypes.ChatMessage) return;
        var payload = msg.GetPayload<ChatPayload>();
        if (payload == null) return;
        AddMessage(new ChatMessage { SenderName = payload.SenderName, Text = payload.Text });
    }

    private sealed record ChatPayload(string SenderName, string Text);
}
