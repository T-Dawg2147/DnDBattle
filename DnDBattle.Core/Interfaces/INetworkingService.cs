namespace DnDBattle.Core.Interfaces;

public interface INetworkingService
{
    bool IsConnected { get; }
    bool IsHost { get; }
    Task<bool> HostGameAsync(int port, CancellationToken ct = default);
    Task<bool> JoinGameAsync(string host, int port, string playerName, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task SendMessageAsync(string messageJson, CancellationToken ct = default);
    event EventHandler<string>? MessageReceived;
}
