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
    public class TriggerMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Trigger;

        public string EventDescription { get; set; }

        public string ScriptCommand { get; set; }

        public bool TriggerOnce { get; set; } = true;

        public int DelayRounds { get; set; } = 0;
    }
}
