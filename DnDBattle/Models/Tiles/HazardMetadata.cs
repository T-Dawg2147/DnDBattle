using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    public class HazardMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Hazard;

        #region Hazard Type

        public HazardType HazardKind { get; set; } = HazardType.Lava;

        public string Description { get; set; } = "A pool of molten lava.";

        #endregion

        #region Damage

        public string DamageDice { get; set; } = "2d10";

        public DamageType DamageType { get; set; } = DamageType.Fire;

        public HazardTrigger DamageTrigger { get; set; } = HazardTrigger.OnEnter;

        #endregion

        #region Saving Throw

        public bool AllowsSave { get; set; } = true;

        public string SaveAbility { get; set; } = "DEX";

        public int SaveDC { get; set; } = 13;

        public bool SaveNegatesDamage { get; set; } = false;

        public bool SaveHalvesDamage { get; set; } = true;

        #endregion

        #region Ongoing Effects

        public bool DamagesEachTurn { get; set; } = false;

        public string PerTurnDamage { get; set; } = "1d10";

        #endregion
    }

    public enum HazardType
    {
        Fire,
        Lava,
        Acid,
        PoisonGas,
        Ice,
        Lightning,
        Thorns,
        Quicksand,
        Spikes,
        MagicZone
    }

    public enum HazardTrigger
    {
        OnEnter,
        StartOfTurn,
        EndOfTurn,
        Always
    }
}
