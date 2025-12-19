using System;

namespace DnDBattle.Models
{
    public class ActionLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; }
        public string Message { get; set; }

        public override string ToString() => $"[{Timestamp:HH:mm:ss}] {Source}:{Message}";
    }
}
