using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDBattle.Networking.Chat;
using DnDBattle.Networking.Client;
using System.Collections.ObjectModel;

namespace DnDBattle.App.ViewModels;

public sealed partial class PlayerClientViewModel : ObservableObject
{
    private readonly GameClient _client;
    private readonly ChatService _chat;

    [ObservableProperty] private string _playerName = "Player";
    [ObservableProperty] private string _hostAddress = "localhost";
    [ObservableProperty] private int _port = 7777;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionStatus = "Disconnected";
    [ObservableProperty] private string _chatInput = string.Empty;

    public ObservableCollection<ChatMessage> ChatMessages { get; } = new();

    public PlayerClientViewModel(GameClient client, ChatService chat)
    {
        _client = client;
        _chat = chat;
        _chat.NewMessage += (_, msg) => ChatMessages.Add(msg);
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        ConnectionStatus = "Connecting...";
        bool success = await _client.JoinGameAsync(HostAddress, Port, PlayerName);
        IsConnected = success;
        ConnectionStatus = success ? $"Connected as {PlayerName}" : "Connection failed";
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _client.DisconnectAsync();
        IsConnected = false;
        ConnectionStatus = "Disconnected";
    }

    [RelayCommand]
    private async Task SendChatAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatInput) || !IsConnected) return;
        await _chat.SendChatAsync(PlayerName, ChatInput);
        ChatInput = string.Empty;
    }
}
