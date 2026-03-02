using DnDBattle.Core.Interfaces;
using DnDBattle.Networking.Protocol;
using System.Net.Sockets;
using System.Text;

namespace DnDBattle.Networking.Client;

public sealed class GameClient : INetworkingService, IAsyncDisposable
{
    private TcpClient? _client;
    private System.IO.StreamWriter? _writer;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _client?.Connected == true;
    public bool IsHost => false;
    public string PlayerName { get; private set; } = string.Empty;

    public event EventHandler<string>? MessageReceived;

    public Task<bool> HostGameAsync(int port, CancellationToken ct = default) =>
        Task.FromResult(false); // Client doesn't host

    public async Task<bool> JoinGameAsync(string host, int port, string playerName, CancellationToken ct = default)
    {
        try
        {
            PlayerName = playerName;
            _client = new TcpClient();
            await _client.ConnectAsync(host, port, ct);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _writer = new System.IO.StreamWriter(_client.GetStream(), Encoding.UTF8) { AutoFlush = true };
            _ = ReadLoopAsync(_cts.Token);

            // Announce join
            var joinMsg = NetworkMessage.Create(MessageTypes.PlayerJoined, playerName, playerName);
            await SendMessageAsync(joinMsg.Serialize(), ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        _writer?.Close();
        _client?.Close();
        await Task.CompletedTask;
    }

    public async Task SendMessageAsync(string messageJson, CancellationToken ct = default)
    {
        if (_writer != null)
            await _writer.WriteLineAsync(messageJson);
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        using var reader = new System.IO.StreamReader(_client!.GetStream(), Encoding.UTF8);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                MessageReceived?.Invoke(this, line);
            }
            catch { break; }
        }
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
