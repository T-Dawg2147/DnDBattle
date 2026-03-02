using System.Windows;
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
using DnDBattle.Services.TileService;

namespace DnDBattle.Views.Multiplayer
{
    public partial class VoiceChatWindow : Window
    {
        private readonly VoiceChatService _voiceChat = new();

        public VoiceChatWindow()
        {
            InitializeComponent();
            UpdateStatus();
        }

        private void SetLink_Click(object sender, RoutedEventArgs e)
        {
            var link = InviteLinkBox.Text?.Trim();
            if (string.IsNullOrEmpty(link)) return;

            _voiceChat.SetDiscordInvite(link);
            UpdateStatus();
        }

        private void OpenDiscord_Click(object sender, RoutedEventArgs e)
        {
            if (!_voiceChat.OpenDiscordLink())
            {
                MessageBox.Show("No Discord invite link set. Please enter a link first.",
                    "Voice Chat", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CopyInvite_Click(object sender, RoutedEventArgs e)
        {
            var sessionName = SessionNameBox.Text?.Trim() ?? "D&D Session";
            var message = _voiceChat.GetInviteMessage(sessionName);
            Clipboard.SetText(message);
            MessageBox.Show("Invite message copied to clipboard!", "Voice Chat",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateStatus()
        {
            StatusText.Text = _voiceChat.IsActive ? "Active" : "Not configured";
        }
    }
}
