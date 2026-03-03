using System;
using System.Collections.Generic;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Models.Creatures
{
    public class TokenNote
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Text { get; set; }
        public NoteType Type { get; set; } = NoteType.General;
        public bool IsPersistent { get; set; } = true; // Persists between combats
        public int? ExpiresOnRound { get; set; } // Auto-remove after this round
    }

    public enum NoteType
    {
        General,
        Combat,      // e.g., "Used Shield spell"
        Condition,   // e.g., "Frightened until end of next turn"
        Reminder,    // e.g., "Has advantage on next attack"
        Tracking     // e.g., "3/3 Superiority Dice remaining"
    }

    public static class NoteTypeExtensions
    {
        public static string GetIcon(this NoteType type)
        {
            return type switch
            {
                NoteType.General => "📝",
                NoteType.Combat => "⚔️",
                NoteType.Condition => "🎯",
                NoteType.Reminder => "⏰",
                NoteType.Tracking => "📊",
                _ => "📝"
            };
        }

        public static System.Windows.Media.Color GetColor(this NoteType type)
        {
            return type switch
            {
                NoteType.General => System.Windows.Media.Color.FromRgb(158, 158, 158),
                NoteType.Combat => System.Windows.Media.Color.FromRgb(244, 67, 54),
                NoteType.Condition => System.Windows.Media.Color.FromRgb(255, 152, 0),
                NoteType.Reminder => System.Windows.Media.Color.FromRgb(33, 150, 243),
                NoteType.Tracking => System.Windows.Media.Color.FromRgb(156, 39, 176),
                _ => System.Windows.Media.Color.FromRgb(158, 158, 158)
            };
        }
    }
}