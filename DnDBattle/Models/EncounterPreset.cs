using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models
{
    public class EncounterPreset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public int RecommendedPartySize { get; set; } = 3;
        public int RecommendedPartyLevel { get; set; } = 1;

        public List<EncounterCreatureEntry> Creatures { get; set; } = new List<EncounterCreatureEntry>();

        public List<string> Tags { get; set; } = new List<string>();
    }

    public class EncounterCreatureEntry
    {
        public string CreatureId { get; set; }
        public string CreatureName { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
