using DnDBattle.Models;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
﻿using System;

namespace DnDBattle.Models.Combat
{
    public class ActionLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; }
        public string Message { get; set; }

        public override string ToString() => $"[{Timestamp:HH:mm:ss}] {Source}:{Message}";
    }
}
