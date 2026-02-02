using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    public class TriggerMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Trigger;

        public string EventDescription { get; set; }

        public string ScriptCommand { get; set; }

        public bool TriggerOnce { get; set; } = true;

        public int DelayRounds { get; set; } = 0;
    }
}
