namespace DnDBattle.App.Configuration;

public sealed class NetworkSettings
{
    public int DefaultPort { get; set; } = 7777;
    public string DefaultPlayerName { get; set; } = "Player";
    public bool EnableVoiceChat { get; set; } = false;
    public int MaxPlayers { get; set; } = 8;
}
