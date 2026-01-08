using System;
using System.Collections.Generic;

namespace DnDBattle.Models
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
