using DnDBattle.Core.Interfaces;
using DnDBattle.Networking.Protocol;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DnDBattle.Networking.Server;

public sealed class GameServer : INetworkingService, IAsyncDisposable
{
    private TcpListener? _listener;
    private readonly List<TcpClient> _clients = new();
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsConnected => _isRunning;
    public bool IsHost => true;

    public event EventHandler<string>? MessageReceived;

    public async Task<bool> HostGameAsync(int port, CancellationToken ct = default)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = AcceptClientsAsync(_cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> JoinGameAsync(string host, int port, string playerName, CancellationToken ct = default) =>
        Task.FromResult(false); // Server doesn't join

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        _isRunning = false;
        foreach (var client in _clients) client.Close();
        _clients.Clear();
        _listener?.Stop();
        await Task.CompletedTask;
    }

    public async Task SendMessageAsync(string messageJson, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(messageJson + "\n");
        var deadClients = new List<TcpClient>();
        foreach (var client in _clients)
        {
            try
            {
                await client.GetStream().WriteAsync(bytes, ct);
            }
            catch
            {
                deadClients.Add(client);
            }
        }
        deadClients.ForEach(c => { _clients.Remove(c); c.Close(); });
    }

    private async Task AcceptClientsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct);
                _clients.Add(client);
                _ = ReadClientAsync(client, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch { /* ignore accept errors */ }
        }
    }

    private async Task ReadClientAsync(TcpClient client, CancellationToken ct)
    {
        using var reader = new System.IO.StreamReader(client.GetStream(), Encoding.UTF8);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                MessageReceived?.Invoke(this, line);
                await SendMessageAsync(line, ct); // broadcast
            }
            catch { break; }
        }
        _clients.Remove(client);
        client.Close();
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
