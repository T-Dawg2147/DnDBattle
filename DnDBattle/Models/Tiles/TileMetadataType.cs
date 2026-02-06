using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Models.Tiles
{ 
    public enum TileMetadataType
    {
        None = 0,
        Trap = 1 << 0,
        Hazard = 1 << 1,
        Secret = 1 << 2,
        Interactive = 1 << 3,
        Trigger = 1 << 4,
        Spawn = 1 << 5,
        Teleporter = 1 << 6,
        Healing = 1 << 7,
        Aura = 1 << 8,
        Lore = 1 << 9,
        Custom = 1 << 10
    }

    public static class TileMetadataTypeExtension
    {
        public static string GetDisplayName(this TileMetadataType type)
        {
            return type switch
            {
                TileMetadataType.Trap => "Trap",
                TileMetadataType.Hazard => "Environmental Hazard",
                TileMetadataType.Secret => "Secret",
                TileMetadataType.Interactive => "Interactive Object",
                TileMetadataType.Trigger => "Event Trigger",
                TileMetadataType.Spawn => "Spawn Point",
                TileMetadataType.Teleporter => "Teleporter",
                TileMetadataType.Healing => "Healing Zone",
                TileMetadataType.Aura => "Aura Effect",
                TileMetadataType.Lore => "Lore Point",
                TileMetadataType.Custom => "Custom",
                _ => "Unknown"
            };
        }

        public static string GetIcon(this TileMetadataType type)
        {
            return type switch
            {
                TileMetadataType.Trap => "⚠️",
                TileMetadataType.Hazard => "☠️",
                TileMetadataType.Secret => "🔍",
                TileMetadataType.Interactive => "⚙️",
                TileMetadataType.Trigger => "⚡",
                TileMetadataType.Spawn => "👹",
                TileMetadataType.Teleporter => "🌀",
                TileMetadataType.Healing => "💚",
                TileMetadataType.Aura => "✨",
                TileMetadataType.Lore => "📜",
                TileMetadataType.Custom => "🔧",
                _ => "❓"
            };
        }
    }
}
