using System;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Category of a map note, used for icon and filtering.
    /// </summary>
    public enum NoteCategory
    {
        General,
        Trap,
        Treasure,
        NPC,
        Quest,
        Lore,
        Secret
    }

    /// <summary>
    /// A text note pinned to a grid position on the map.
    /// Can be DM-only or visible to players.
    /// </summary>
    public class MapNote
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Grid X position.</summary>
        public int GridX { get; set; }

        /// <summary>Grid Y position.</summary>
        public int GridY { get; set; }

        /// <summary>Note body text.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Whether players can see this note.</summary>
        public bool IsPlayerVisible { get; set; }

        /// <summary>Category for iconography and filtering.</summary>
        public NoteCategory Category { get; set; } = NoteCategory.General;

        /// <summary>Background color as an ARGB hex string (e.g. "#FFFFFF00").</summary>
        public string BackgroundColor { get; set; } = "#FFFFFF00";

        /// <summary>Font size for the label text.</summary>
        public double FontSize { get; set; } = 12;

        /// <summary>Whether the text is rendered bold.</summary>
        public bool IsBold { get; set; }

        /// <summary>Optional emoji or icon character shown next to the note.</summary>
        public string Icon { get; set; }
    }
}
