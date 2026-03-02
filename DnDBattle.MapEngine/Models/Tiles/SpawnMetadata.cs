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

namespace DnDBattle.Models.Tiles
{
    public class SpawnMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Spawn;

        #region Spawn Configuration

        public string CreatureTemplateId { get; set; }

        public string CreatureName { get; set; } = "Goblin";

        public bool SpawnOnMapLoad { get; set; } = false;

        public int SpawnCount { get; set; } = 1;

        public int SpawnRadius { get; set; } = 0;

        #endregion

        #region Trigger Conditions

        public SpawnTrigger TriggerCondition { get; set; } = SpawnTrigger.Manual;

        public int SpawnOnRound { get; set; } = 1;

        public int TriggerDistance { get; set; } = 3;

        #endregion

        #region State

        public bool HasSpawned { get; set; } = false;

        public bool IsReusable { get; set; } = false;

        public int SpawnDelay { get; set; } = 0;

        #endregion
    }

    public enum SpawnTrigger
    {
        Manual,
        CombatStart,
        RoundNumber,
        Proximity,
        Event
    }
}
