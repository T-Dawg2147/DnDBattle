using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    public class InteractiveMetadata : TileMetadata
    {
        public override TileMetadataType Type => TileMetadataType.Interactive;

        #region Object Type

        public InteractiveType ObjectType { get; set; } = InteractiveType.Lever;

        public string State { get; set; } = "Off";

        #endregion

        #region Interaction

        public string ExamineDesctiption { get; set; } = "You see a level on the wall.";

        public string ActivationEffect { get; set; } = "You pull the lever. Your hear a distant rumbling.";

        public bool SingleUse { get; set; } = false;

        public int TimesActivated { get; set; } = 0;

        #endregion

        #region Requirements

        public bool RequiresCheck { get; set; } = false;

        public string RequiredSkill { get; set; } = "Strength";

        public int CheckDC { get; set; } = 15;

        public bool IsLocked { get; set; } = false;

        public string LockedDescription { get; set; } = "The chest is locked";

        #endregion

        #region Loot (for chest/containers)

        public int GoldPieces { get; set; } = 0;

        public string ContainedItems { get; set; }

        public bool HasBeenLooted { get; set; } = false;

        #endregion
    }

    public enum InteractiveType
    {
        Lever,
        Button,
        Chest,
        Door,
        Statue,
        Alter,
        Pedestal,
        Brazier,
        Crystal,
        Mechanism
    }
}
