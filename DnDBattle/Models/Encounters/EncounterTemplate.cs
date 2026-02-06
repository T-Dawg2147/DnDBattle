using System;
using System.Collections.Generic;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using DnDBattle.Models.Encounters;

namespace DnDBattle.Models.Encounters
{
    public class EncounterTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public string Difficulty { get; set; }
        public List<EncounterSlot> Slots { get; set; } = new List<EncounterSlot>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class EncounterSlot
    {
        public string CreatureName { get; set; }
        public int Count { get; set; } = 1;
        public string Notes { get; set; }

        public List<SlotPosition> Positions { get; set; } = new List<SlotPosition>();
    }

    public class SlotPosition
    {
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
    }
}
