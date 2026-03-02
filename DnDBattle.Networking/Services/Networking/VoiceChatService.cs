using System;
using System.Diagnostics;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;

namespace DnDBattle.Services.Networking
{
    /// <summary>
    /// Voice chat integration for Phase 9 multiplayer.
    /// Uses external voice chat apps (Discord, etc.) rather than
    /// implementing a custom voice solution.
    /// Provides helper methods to open Discord links and manage voice state.
    /// </summary>
    public sealed class VoiceChatService
    {
        /// <summary>Raised when a message should be logged.</summary>
        public event System.Action<string, string>? MessageLogged;

        /// <summary>Whether voice chat integration is active.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Current Discord invite link for the session.</summary>
        public string? DiscordInviteLink { get; private set; }

        /// <summary>
        /// Set the Discord invite link for the current campaign/session.
        /// </summary>
        public void SetDiscordInvite(string inviteLink)
        {
            if (!Options.EnableVoiceChat) return;

            DiscordInviteLink = inviteLink;
            IsActive = true;
            Log("VoiceChat", $"Discord invite set: {inviteLink}");
        }

        /// <summary>
        /// Open the Discord invite link in the user's default browser.
        /// </summary>
        public bool OpenDiscordLink()
        {
            if (string.IsNullOrEmpty(DiscordInviteLink))
            {
                Log("VoiceChat", "No Discord invite link set");
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = DiscordInviteLink,
                    UseShellExecute = true
                };
                Process.Start(psi);
                Log("VoiceChat", "Opened Discord invite in browser");
                return true;
            }
            catch (Exception ex)
            {
                Log("VoiceChat", $"Failed to open Discord link: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate a placeholder invite message for sharing with players.
        /// </summary>
        public string GetInviteMessage(string sessionName)
        {
            if (string.IsNullOrEmpty(DiscordInviteLink))
                return $"Join the D&D session: {sessionName}";

            return $"🎲 Join the D&D session: {sessionName}\n" +
                   $"🎤 Voice Chat: {DiscordInviteLink}\n" +
                   $"Connect in DnDBattle to join the battle map!";
        }

        /// <summary>
        /// Clear voice chat state.
        /// </summary>
        public void Clear()
        {
            DiscordInviteLink = null;
            IsActive = false;
        }

        private void Log(string source, string message) =>
            MessageLogged?.Invoke(source, message);
    }
}
