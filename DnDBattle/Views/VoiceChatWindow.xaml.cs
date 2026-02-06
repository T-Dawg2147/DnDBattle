using System.Windows;
using DnDBattle.Services;

namespace DnDBattle.Views
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
