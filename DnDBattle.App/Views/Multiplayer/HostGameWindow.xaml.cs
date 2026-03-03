using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
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
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;

namespace DnDBattle.Views.Multiplayer
{
    public partial class HostGameWindow : Window
    {
        private readonly GameServer _server = new();
        private readonly ObservableCollection<PlayerInfo> _players = new();

        public HostGameWindow()
        {
            InitializeComponent();
            PortInput.Text = Options.NetworkDefaultPort.ToString();
            PlayersListView.ItemsSource = _players;
            PlayerCombo.ItemsSource = _players;
            PlayerCombo.DisplayMemberPath = "Username";

            _server.MessageLogged += OnMessageLogged;
            _server.PlayerConnected += OnPlayerConnected;
            _server.PlayerDisconnected += OnPlayerDisconnected;
            _server.ChatMessageReceived += OnChatMessageReceived;
        }

        /// <summary>
        /// Provide available tokens for assignment.
        /// </summary>
        public void SetTokens(IEnumerable<Token> tokens)
        {
            TokenCombo.ItemsSource = tokens;
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server.IsRunning)
            {
                _server.Stop();
                StartStopButton.Content = "Start Server";
                StatusText.Text = "Stopped";
            }
            else
            {
                if (!int.TryParse(PortInput.Text, out int port))
                    port = Options.NetworkDefaultPort;

                _server.Start(port);
                StartStopButton.Content = "Stop Server";
                StatusText.Text = $"Running on port {port}";
            }
        }

        private void AssignToken_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerCombo.SelectedItem is not PlayerInfo player) return;
            if (TokenCombo.SelectedItem is not Token token) return;

            _server.AssignTokenToPlayer(player.Id, token.Id);
            AppendLog($"Assigned token '{token.Name}' to {player.Username}");
            PlayersListView.Items.Refresh();
        }

        private void SendChat_Click(object sender, RoutedEventArgs e) => SendChatMessage();

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendChatMessage();
        }

        private void SendChatMessage()
        {
            var msg = ChatInput.Text?.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            var chatData = new ChatMessageData
            {
                Username = "DM",
                Message = msg,
                Timestamp = DateTime.UtcNow
            };

            _ = _server.BroadcastPacketAsync(new NetworkPacket
            {
                Type = PacketType.ChatMessage,
                Payload = System.Text.Json.JsonSerializer.Serialize(chatData)
            });

            ChatHistory.Items.Add($"[{chatData.Timestamp.ToLocalTime():HH:mm}] DM: {msg}");
            ChatInput.Clear();
        }

        private void OnMessageLogged(string source, string message)
        {
            Dispatcher.Invoke(() => AppendLog($"[{source}] {message}"));
        }

        private void OnPlayerConnected(PlayerInfo info)
        {
            Dispatcher.Invoke(() =>
            {
                _players.Add(info);
                AppendLog($"Player connected: {info.Username}");
            });
        }

        private void OnPlayerDisconnected(PlayerInfo info)
        {
            Dispatcher.Invoke(() =>
            {
                _players.Remove(info);
                AppendLog($"Player disconnected: {info.Username}");
            });
        }

        private void OnChatMessageReceived(ChatMessageData data)
        {
            Dispatcher.Invoke(() =>
            {
                ChatHistory.Items.Add($"[{data.Timestamp:HH:mm}] {data.Username}: {data.Message}");
            });
        }

        // VISUAL REFRESH
        private void AppendLog(string message)
        {
            ServerLogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            ServerLogBox.ScrollToEnd();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _server.Dispose();
        }
    }
}
