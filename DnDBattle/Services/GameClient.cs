using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DnDBattle.Models;

namespace DnDBattle.Services
{
    /// <summary>
    /// Player-side game client for Phase 9 multiplayer.
    /// Connects to a DM-hosted GameServer via TCP.
    /// Supports client-side prediction for token movement.
    /// </summary>
    public sealed class GameClient : IDisposable
    {
        private TcpClient? _client;
        private StreamWriter? _writer;
        private CancellationTokenSource? _cts;
        private string _username = "Player";
        private bool _disposed;

        private readonly ConcurrentDictionary<Guid, MovementPrediction> _predictions = new();

        /// <summary>Raised when a message should be logged.</summary>
        public event Action<string, string>? MessageLogged;

        /// <summary>Raised when disconnected from the server.</summary>
        public event System.Action? Disconnected;

        /// <summary>Raised when the full game state is received from the server.</summary>
        public event Action<string>? GameStateReceived;

        /// <summary>Raised when a token move update is received.</summary>
        public event Action<TokenMoveData>? TokenMoveReceived;

        /// <summary>Raised when a chat message is received.</summary>
        public event Action<ChatMessageData>? ChatMessageReceived;

        /// <summary>Raised when a dice roll result is received.</summary>
        public event Action<DiceRollData>? DiceRollReceived;

        /// <summary>Raised when a fog update is received.</summary>
        public event Action<FogUpdateData>? FogUpdateReceived;

        /// <summary>Raised when an attack result is received.</summary>
        public event Action<AttackResultData>? AttackResultReceived;

        /// <summary>Raised when a player joins the session.</summary>
        public event Action<string>? PlayerJoinedSession;

        /// <summary>Raised when a player leaves the session.</summary>
        public event Action<string>? PlayerLeftSession;

        /// <summary>Whether the client is connected to a server.</summary>
        public bool IsConnected => _client?.Connected == true;

        /// <summary>The username used for this session.</summary>
        public string Username => _username;

        /// <summary>
        /// Connect to a game server.
        /// </summary>
        public async Task<bool> ConnectAsync(string host, int port, string username)
        {
            if (IsConnected) return true;
            if (!Options.EnableNetworking) return false;

            _username = username;

            try
            {
                _client = new TcpClient();

                using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(Options.NetworkConnectionTimeoutSeconds));
                await _client.ConnectAsync(host, port).ConfigureAwait(false);

                if (!_client.Connected)
                    return false;

                _writer = new StreamWriter(_client.GetStream(), Encoding.UTF8) { AutoFlush = true };
                _cts = new CancellationTokenSource();

                // Send handshake
                await SendPacketAsync(new NetworkPacket
                {
                    Type = PacketType.Handshake,
                    Payload = JsonSerializer.Serialize(new { Username = username })
                }).ConfigureAwait(false);

                // Start receive loop
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                Log("Client", $"Connected to {host}:{port} as {username}");
                return true;
            }
            catch (Exception ex)
            {
                Log("Client", $"Connection failed: {ex.Message}");
                _client?.Close();
                _client = null;
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the server.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (!IsConnected) return;

            try
            {
                await SendPacketAsync(new NetworkPacket { Type = PacketType.Disconnect }).ConfigureAwait(false);
            }
            catch { /* best effort */ }

            _cts?.Cancel();
            _client?.Close();
            _client = null;
            _writer = null;

            Log("Client", "Disconnected");
            Disconnected?.Invoke();
        }

        /// <summary>
        /// Send a packet to the server.
        /// </summary>
        public async Task SendPacketAsync(NetworkPacket packet)
        {
            if (_writer == null) return;

            try
            {
                var json = JsonSerializer.Serialize(packet);
                await _writer.WriteLineAsync(json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log("Client", $"Send error: {ex.Message}");
            }
        }

        /// <summary>
        /// Move a token with client-side prediction. The move is applied locally
        /// immediately and sent to the server. If the server rejects it, the
        /// position is rolled back.
        /// </summary>
        public async Task MoveTokenAsync(Guid tokenId, int oldX, int oldY, int newX, int newY)
        {
            // Register prediction
            _predictions[tokenId] = new MovementPrediction
            {
                TokenId = tokenId,
                OldX = oldX,
                OldY = oldY,
                PredictedX = newX,
                PredictedY = newY
            };

            await SendPacketAsync(new NetworkPacket
            {
                Type = PacketType.TokenMove,
                Payload = JsonSerializer.Serialize(new TokenMoveData
                {
                    TokenId = tokenId,
                    NewX = newX,
                    NewY = newY
                })
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a chat message.
        /// </summary>
        public async Task SendChatMessageAsync(string message)
        {
            await SendPacketAsync(new NetworkPacket
            {
                Type = PacketType.ChatMessage,
                Payload = JsonSerializer.Serialize(new ChatMessageData
                {
                    Username = _username,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                })
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Request a dice roll (server may re-roll for fairness).
        /// </summary>
        public async Task RollDiceAsync(string expression)
        {
            await SendPacketAsync(new NetworkPacket
            {
                Type = PacketType.DiceRoll,
                Payload = JsonSerializer.Serialize(new DiceRollData
                {
                    Username = _username,
                    Expression = expression,
                    Timestamp = DateTime.UtcNow
                })
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Request an attack action from the server.
        /// </summary>
        public async Task RequestAttackAsync(Guid attackerTokenId, Guid defenderTokenId, string attackName)
        {
            await SendPacketAsync(new NetworkPacket
            {
                Type = PacketType.AttackRequest,
                Payload = JsonSerializer.Serialize(new AttackRequestData
                {
                    AttackerTokenId = attackerTokenId,
                    DefenderTokenId = defenderTokenId,
                    AttackName = attackName
                })
            }).ConfigureAwait(false);
        }

        // ── Private helpers ──

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            if (_client == null) return;

            var reader = new StreamReader(_client.GetStream(), Encoding.UTF8);

            while (!ct.IsCancellationRequested && _client.Connected)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null) break;
                }
                catch { break; }

                NetworkPacket? packet;
                try
                {
                    packet = JsonSerializer.Deserialize<NetworkPacket>(line);
                }
                catch
                {
                    Log("Client", "Bad packet received");
                    continue;
                }

                if (packet != null)
                    HandlePacket(packet);
            }

            Log("Client", "Disconnected from server");
            Disconnected?.Invoke();
        }

        private void HandlePacket(NetworkPacket packet)
        {
            switch (packet.Type)
            {
                case PacketType.FullSync:
                    GameStateReceived?.Invoke(packet.Payload);
                    break;

                case PacketType.TokenMove:
                    var moveData = JsonSerializer.Deserialize<TokenMoveData>(packet.Payload);
                    if (moveData != null)
                    {
                        ReconcilePrediction(moveData);
                        TokenMoveReceived?.Invoke(moveData);
                    }
                    break;

                case PacketType.ChatMessage:
                    var chatData = JsonSerializer.Deserialize<ChatMessageData>(packet.Payload);
                    if (chatData != null) ChatMessageReceived?.Invoke(chatData);
                    break;

                case PacketType.DiceRoll:
                    var rollData = JsonSerializer.Deserialize<DiceRollData>(packet.Payload);
                    if (rollData != null) DiceRollReceived?.Invoke(rollData);
                    break;

                case PacketType.FogUpdate:
                    var fogData = JsonSerializer.Deserialize<FogUpdateData>(packet.Payload);
                    if (fogData != null) FogUpdateReceived?.Invoke(fogData);
                    break;

                case PacketType.AttackResult:
                    var atkResult = JsonSerializer.Deserialize<AttackResultData>(packet.Payload);
                    if (atkResult != null) AttackResultReceived?.Invoke(atkResult);
                    break;

                case PacketType.PlayerJoined:
                    PlayerJoinedSession?.Invoke(packet.Payload);
                    break;

                case PacketType.PlayerLeft:
                    PlayerLeftSession?.Invoke(packet.Payload);
                    break;
            }
        }

        /// <summary>
        /// Check if server-confirmed position matches our prediction.
        /// If not, the prediction was wrong and we log a correction.
        /// </summary>
        private void ReconcilePrediction(TokenMoveData serverData)
        {
            if (_predictions.TryRemove(serverData.TokenId, out var prediction))
            {
                if (serverData.NewX != prediction.PredictedX || serverData.NewY != prediction.PredictedY)
                {
                    Log("Network", "⚠️ Move corrected by server");
                }
            }
        }

        private void Log(string source, string message) =>
            MessageLogged?.Invoke(source, message);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _client?.Close();
            _cts?.Dispose();
        }
    }
}
