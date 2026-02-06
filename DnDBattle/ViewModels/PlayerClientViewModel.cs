using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Models;
using DnDBattle.Services;

namespace DnDBattle.ViewModels
{
    /// <summary>
    /// ViewModel for the Phase 9 player client.
    /// Manages connection to the DM server, chat, dice rolling,
    /// and displays only what the player is allowed to see.
    /// </summary>
    public partial class PlayerClientViewModel : ObservableObject
    {
        private readonly GameClient _client = new();

        [ObservableProperty]
        private string _serverHost = "localhost";

        [ObservableProperty]
        private int _serverPort = 7777;

        [ObservableProperty]
        private string _playerName = "Player";

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private string _connectionStatus = "Disconnected";

        [ObservableProperty]
        private string _chatInput = string.Empty;

        [ObservableProperty]
        private string _diceExpression = "1d20";

        /// <summary>Chat message log visible to the player.</summary>
        public ObservableCollection<string> ChatMessages { get; } = new();

        /// <summary>Dice roll results visible to the player.</summary>
        public ObservableCollection<string> DiceResults { get; } = new();

        /// <summary>Notifications from the server (attacks, effects, etc.).</summary>
        public ObservableCollection<string> Notifications { get; } = new();

        public PlayerClientViewModel()
        {
            _client.ChatMessageReceived += OnChatMessageReceived;
            _client.DiceRollReceived += OnDiceRollReceived;
            _client.AttackResultReceived += OnAttackResultReceived;
            _client.Disconnected += OnDisconnected;
            _client.MessageLogged += OnMessageLogged;
            _client.PlayerJoinedSession += OnPlayerJoined;
            _client.PlayerLeftSession += OnPlayerLeft;

            ServerPort = Options.NetworkDefaultPort;
        }

        /// <summary>
        /// Connect to the DM's game server.
        /// </summary>
        [RelayCommand]
        private async Task ConnectAsync()
        {
            if (IsConnected) return;

            ConnectionStatus = $"Connecting to {ServerHost}:{ServerPort}...";
            var success = await _client.ConnectAsync(ServerHost, ServerPort, PlayerName);

            if (success)
            {
                IsConnected = true;
                ConnectionStatus = $"Connected as {PlayerName}";
                AddNotification($"✅ Connected to {ServerHost}:{ServerPort}");
            }
            else
            {
                ConnectionStatus = "Connection failed";
                AddNotification("❌ Failed to connect to server");
            }
        }

        /// <summary>
        /// Disconnect from the server.
        /// </summary>
        [RelayCommand]
        private async Task DisconnectAsync()
        {
            if (!IsConnected) return;
            await _client.DisconnectAsync();
        }

        /// <summary>
        /// Send a chat message.
        /// </summary>
        [RelayCommand]
        private async Task SendChatAsync()
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(ChatInput)) return;

            await _client.SendChatMessageAsync(ChatInput);
            ChatInput = string.Empty;
        }

        /// <summary>
        /// Roll dice and send to the server.
        /// </summary>
        [RelayCommand]
        private async Task RollDiceAsync()
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(DiceExpression)) return;
            await _client.RollDiceAsync(DiceExpression);
        }

        /// <summary>
        /// Request an attack through the server.
        /// </summary>
        public async Task RequestAttackAsync(Guid attackerTokenId, Guid defenderTokenId, string attackName)
        {
            if (!IsConnected) return;
            await _client.RequestAttackAsync(attackerTokenId, defenderTokenId, attackName);
            AddNotification($"⚔️ Requested attack: {attackName}...");
        }

        /// <summary>
        /// Move a token with client-side prediction.
        /// </summary>
        public async Task MoveTokenAsync(Guid tokenId, int oldX, int oldY, int newX, int newY)
        {
            if (!IsConnected) return;
            await _client.MoveTokenAsync(tokenId, oldX, oldY, newX, newY);
        }

        // ── Event handlers ──

        private void OnChatMessageReceived(ChatMessageData data)
        {
            var formatted = $"[{data.Timestamp:HH:mm}] {data.Username}: {data.Message}";
            AddChatMessage(formatted);
        }

        private void OnDiceRollReceived(DiceRollData data)
        {
            var result = $"🎲 {data.Username} rolled {data.Expression} = {data.Result}";
            AddDiceResult(result);
            AddChatMessage(result);
        }

        private void OnAttackResultReceived(AttackResultData data)
        {
            var msg = data.Hit
                ? $"⚔️ HIT! {data.AttackerName} → {data.DefenderName}: {data.Damage} damage"
                : $"❌ MISS! {data.AttackerName} → {data.DefenderName}";
            AddNotification(msg);
            AddChatMessage(msg);
        }

        private void OnDisconnected()
        {
            IsConnected = false;
            ConnectionStatus = "Disconnected";
            AddNotification("🔌 Disconnected from server");
        }

        private void OnMessageLogged(string source, string message)
        {
            AddNotification($"[{source}] {message}");
        }

        private void OnPlayerJoined(string payload)
        {
            AddNotification($"👋 A player joined the session");
            AddChatMessage("👋 A player joined the session");
        }

        private void OnPlayerLeft(string payload)
        {
            AddNotification($"👋 A player left the session");
            AddChatMessage("👋 A player left the session");
        }

        // ── UI thread helpers ──

        private void AddChatMessage(string msg)
        {
            if (System.Windows.Application.Current?.Dispatcher != null)
                System.Windows.Application.Current.Dispatcher.Invoke(() => ChatMessages.Add(msg));
            else
                ChatMessages.Add(msg);
        }

        private void AddDiceResult(string msg)
        {
            if (System.Windows.Application.Current?.Dispatcher != null)
                System.Windows.Application.Current.Dispatcher.Invoke(() => DiceResults.Add(msg));
            else
                DiceResults.Add(msg);
        }

        private void AddNotification(string msg)
        {
            if (System.Windows.Application.Current?.Dispatcher != null)
                System.Windows.Application.Current.Dispatcher.Invoke(() => Notifications.Add(msg));
            else
                Notifications.Add(msg);
        }
    }
}
