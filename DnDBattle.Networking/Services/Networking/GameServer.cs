using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.TileService;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Networking
{
    /// <summary>
    /// DM-side authoritative game server for Phase 9 multiplayer.
    /// Uses TCP with newline-delimited JSON for message framing.
    /// </summary>
    public sealed class GameServer : IDisposable
    {
        private TcpListener? _listener;
        private readonly ConcurrentDictionary<string, PlayerConnection> _players = new();
        private CancellationTokenSource? _cts;
        private bool _disposed;

        /// <summary>Raised when a message should be logged.</summary>
        public event Action<string, string>? MessageLogged;

        /// <summary>Raised when a player connects.</summary>
        public event Action<PlayerInfo>? PlayerConnected;

        /// <summary>Raised when a player disconnects.</summary>
        public event Action<PlayerInfo>? PlayerDisconnected;

        /// <summary>Raised when a chat message is received.</summary>
        public event Action<ChatMessageData>? ChatMessageReceived;

        /// <summary>Raised when a dice roll is received from a client.</summary>
        public event Action<DiceRollData>? DiceRollReceived;

        /// <summary>Raised when a token move is received from a client.</summary>
        public event Action<TokenMoveData>? TokenMoveReceived;

        /// <summary>Raised when an attack request is received from a client.</summary>
        public event Action<string, AttackRequestData>? AttackRequestReceived;

        /// <summary>Whether the server is currently running.</summary>
        public bool IsRunning => _listener != null && _cts != null && !_cts.IsCancellationRequested;

        /// <summary>Number of connected players.</summary>
        public int PlayerCount => _players.Count;

        /// <summary>Gets snapshot of connected players.</summary>
        public IReadOnlyList<PlayerInfo> ConnectedPlayers =>
            _players.Values.Select(p => p.Info).ToArray();

        /// <summary>
        /// Start the server on the given port.
        /// </summary>
        public void Start(int port)
        {
            if (IsRunning) return;
            if (!Options.EnableNetworking) return;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            Task.Run(() => AcceptClientsLoopAsync(_cts.Token));

            Log("Server", $"Started on port {port}");
        }

        /// <summary>
        /// Stop the server and disconnect all players.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            _listener?.Stop();

            foreach (var player in _players.Values)
            {
                try { player.Client.Close(); }
                catch { /* best effort */ }
            }
            _players.Clear();

            _listener = null;
            _cts = null;

            Log("Server", "Stopped");
        }

        /// <summary>
        /// Assign a token to a specific player so they can control it.
        /// </summary>
        public void AssignTokenToPlayer(string playerId, Guid tokenId)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                if (!player.Info.AssignedTokenIds.Contains(tokenId))
                    player.Info.AssignedTokenIds.Add(tokenId);
            }
        }

        /// <summary>
        /// Broadcast a packet to all connected players.
        /// </summary>
        public async Task BroadcastPacketAsync(NetworkPacket packet)
        {
            var json = JsonSerializer.Serialize(packet) + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);

            var tasks = _players.Values
                .Where(p => p.Info.IsConnected)
                .Select(p => SendBytesAsync(p, bytes));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a packet to a specific player.
        /// </summary>
        public async Task SendPacketToPlayerAsync(string playerId, NetworkPacket packet)
        {
            if (!_players.TryGetValue(playerId, out var player)) return;

            var json = JsonSerializer.Serialize(packet) + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);
            await SendBytesAsync(player, bytes).ConfigureAwait(false);
        }

        /// <summary>
        /// Broadcast a fog update to all players or a specific player.
        /// </summary>
        public async Task BroadcastFogUpdateAsync(FogUpdateData fogData, string? targetPlayerId = null)
        {
            var packet = new NetworkPacket
            {
                Type = PacketType.FogUpdate,
                Payload = JsonSerializer.Serialize(fogData)
            };

            if (targetPlayerId != null)
                await SendPacketToPlayerAsync(targetPlayerId, packet).ConfigureAwait(false);
            else
                await BroadcastPacketAsync(packet).ConfigureAwait(false);
        }

        /// <summary>
        /// Send the full game state JSON to a player (for initial sync).
        /// </summary>
        public async Task SendFullSyncAsync(string playerId, string gameStateJson)
        {
            var packet = new NetworkPacket
            {
                Type = PacketType.FullSync,
                Payload = gameStateJson
            };
            await SendPacketToPlayerAsync(playerId, packet).ConfigureAwait(false);
        }

        // ── Private helpers ──

        private async Task AcceptClientsLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _listener!.AcceptTcpClientAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleNewClientAsync(tcpClient, ct), ct);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
            }
        }

        private async Task HandleNewClientAsync(TcpClient tcpClient, CancellationToken ct)
        {
            var stream = tcpClient.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);

            // Wait for handshake
            string? handshakeLine;
            try
            {
                handshakeLine = await reader.ReadLineAsync().ConfigureAwait(false);
            }
            catch
            {
                tcpClient.Close();
                return;
            }

            if (handshakeLine == null)
            {
                tcpClient.Close();
                return;
            }

            NetworkPacket? handshake;
            try
            {
                handshake = JsonSerializer.Deserialize<NetworkPacket>(handshakeLine);
            }
            catch
            {
                tcpClient.Close();
                return;
            }

            if (handshake == null || handshake.Type != PacketType.Handshake)
            {
                tcpClient.Close();
                return;
            }

            string username = "Player";
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(handshake.Payload);
                if (payload != null && payload.TryGetValue("Username", out var u))
                    username = u;
            }
            catch { /* use default */ }

            var info = new PlayerInfo { Username = username };
            var conn = new PlayerConnection(tcpClient, info);

            _players[info.Id] = conn;
            Log("Server", $"Player connected: {username} ({info.Id})");
            PlayerConnected?.Invoke(info);

            // Notify other players
            await BroadcastPacketAsync(new NetworkPacket
            {
                Type = PacketType.PlayerJoined,
                Payload = JsonSerializer.Serialize(new { info.Id, info.Username })
            }).ConfigureAwait(false);

            // Read loop
            await ReadPlayerMessagesAsync(conn, reader, ct).ConfigureAwait(false);

            // Cleanup on disconnect
            _players.TryRemove(info.Id, out _);
            info.IsConnected = false;
            tcpClient.Close();

            Log("Server", $"Player disconnected: {username} ({info.Id})");
            PlayerDisconnected?.Invoke(info);

            await BroadcastPacketAsync(new NetworkPacket
            {
                Type = PacketType.PlayerLeft,
                Payload = JsonSerializer.Serialize(new { info.Id, info.Username })
            }).ConfigureAwait(false);
        }

        private async Task ReadPlayerMessagesAsync(PlayerConnection conn, StreamReader reader, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && conn.Client.Connected)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break; // stream closed
                }
                catch { break; }

                NetworkPacket? packet;
                try
                {
                    packet = JsonSerializer.Deserialize<NetworkPacket>(line);
                }
                catch
                {
                    Log("Server", $"Bad packet from {conn.Info.Username}");
                    continue;
                }

                if (packet != null)
                    ProcessPacket(conn, packet);
            }
        }

        private void ProcessPacket(PlayerConnection sender, NetworkPacket packet)
        {
            switch (packet.Type)
            {
                case PacketType.TokenMove:
                    var moveData = JsonSerializer.Deserialize<TokenMoveData>(packet.Payload);
                    if (moveData == null) return;

                    if (!ValidateTokenOwnership(sender, moveData.TokenId))
                    {
                        Log("Server", $"⚠️ Unauthorized move by {sender.Info.Username}");
                        return;
                    }

                    TokenMoveReceived?.Invoke(moveData);
                    _ = BroadcastPacketAsync(packet);
                    break;

                case PacketType.ChatMessage:
                    var chatData = JsonSerializer.Deserialize<ChatMessageData>(packet.Payload);
                    if (chatData != null) ChatMessageReceived?.Invoke(chatData);
                    _ = BroadcastPacketAsync(packet);
                    break;

                case PacketType.DiceRoll:
                    var rollData = JsonSerializer.Deserialize<DiceRollData>(packet.Payload);
                    if (rollData != null) DiceRollReceived?.Invoke(rollData);
                    _ = BroadcastPacketAsync(packet);
                    break;

                case PacketType.AttackRequest:
                    var atkData = JsonSerializer.Deserialize<AttackRequestData>(packet.Payload);
                    if (atkData != null) AttackRequestReceived?.Invoke(sender.Info.Id, atkData);
                    break;

                case PacketType.Disconnect:
                    try { sender.Client.Close(); }
                    catch { /* best effort */ }
                    break;
            }
        }

        private bool ValidateTokenOwnership(PlayerConnection sender, Guid tokenId)
        {
            return sender.Info.AssignedTokenIds.Contains(tokenId);
        }

        private static async Task SendBytesAsync(PlayerConnection player, byte[] bytes)
        {
            try
            {
                var stream = player.Client.GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
            catch
            {
                player.Info.IsConnected = false;
            }
        }

        private void Log(string source, string message) =>
            MessageLogged?.Invoke(source, message);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _cts?.Dispose();
        }

        // ── Inner helper ──

        private sealed class PlayerConnection
        {
            public TcpClient Client { get; }
            public PlayerInfo Info { get; }

            public PlayerConnection(TcpClient client, PlayerInfo info)
            {
                Client = client;
                Info = info;
            }
        }
    }
}
