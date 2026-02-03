using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Data Transfer Object for serializing TileMap to JSON
    /// </summary>
    public class TileMapDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double CellSize { get; set; }
        public string BackgroundColor { get; set; }
        public bool ShowGrid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Serialized tile placements
        /// </summary>
        public List<TileDto> Tiles { get; set; } = new List<TileDto>();
    }

    /// <summary>
    /// DTO for individual tile instances
    /// </summary>
    public class TileDto
    {
        public Guid InstanceId { get; set; }
        public string TileDefinitionId { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int Rotation { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }
        public int? ZIndex { get; set; }
        public string Notes { get; set; }

        // ===== METADATA SERIALIZATION =====
        public List<TileMetadataDto> Metadata { get; set; } = new List<TileMetadataDto>();
    }

    /// <summary>
    /// Base DTO for metadata serialization
    /// </summary>
    public class TileMetadataDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } // "Trap", "Secret", etc.
        public string Name { get; set; }
        public bool IsVisibleToPlayers { get; set; }
        public bool IsTriggered { get; set; }
        public string DMNotes { get; set; }
        public bool IsEnabled { get; set; }

        // Polymorphic data stored as JSON string
        public string Data { get; set; }
    }

    // ===== SPECIALIZED DTOs FOR EACH METADATA TYPE =====

    public class TrapMetadataDto
    {
        public bool AutoRollDetection { get; set; }
        public bool AutoRollDisarm { get; set; }
        public bool AutoRollSave { get; set; }
        public bool AutoRollDamage { get; set; }
        public int DetectionDC { get; set; }
        public bool IsDetected { get; set; }
        public string DetectionDescription { get; set; }
        public bool CanBeDisarmed { get; set; }
        public string DisarmSkill { get; set; }
        public int DisarmDC { get; set; }
        public bool IsDisarmed { get; set; }
        public bool FailedDisarmTriggersTrap { get; set; }
        public string TriggerType { get; set; }
        public string SaveAbility { get; set; }
        public int SaveDC { get; set; }
        public int AreaOfEffect { get; set; }
        public string DamageDice { get; set; }
        public string DamageType { get; set; }
        public bool HalfDamageOnSave { get; set; }
        public List<string> AdditionalEffects { get; set; }
        public bool IsReusable { get; set; }
        public int ResetTimeRounds { get; set; }
        public int MaxTriggers { get; set; }
        public int TimesTriggered { get; set; }
        public string TriggerDescription { get; set; }
        public string EffectDescription { get; set; }
    }

    public class SecretMetadataDto
    {
        public int InvestigationDC { get; set; }
        public bool IsDiscovered { get; set; }
        public string DiscoveryDescription { get; set; }
        public string SecretKind { get; set; }
        public string Direction { get; set; }
        public string TreasureDescription { get; set; }
        public bool RequiresActivation { get; set; }
        public string ActivationDescription { get; set; }
    }

    public class InteractiveMetadataDto
    {
        public string ObjectType { get; set; }
        public string State { get; set; }
        public string ExamineDescription { get; set; }
        public string ActivationEffect { get; set; }
        public bool SingleUse { get; set; }
        public int TimesActivated { get; set; }
        public bool RequiresCheck { get; set; }
        public string RequiredSkill { get; set; }
        public int CheckDC { get; set; }
        public bool IsLocked { get; set; }
        public string LockedDescription { get; set; }
        public int GoldPieces { get; set; }
        public string ContainedItems { get; set; }
        public bool HasBeenLooted { get; set; }
    }

    public class HazardMetadataDto
    {
        public string HazardKind { get; set; }
        public string Description { get; set; }
        public string DamageDice { get; set; }
        public string DamageType { get; set; }
        public string DamageTrigger { get; set; }
        public bool AllowsSave { get; set; }
        public string SaveAbility { get; set; }
        public int SaveDC { get; set; }
        public bool SaveNegatesDamage { get; set; }
        public bool SaveHalvesDamage { get; set; }
        public bool DamagesEachTurn { get; set; }
        public string PerTurnDamage { get; set; }
    }

    public class TeleporterMetadataDto
    {
        public int DestinationX { get; set; }
        public int DestinationY { get; set; }
        public string TeleportDescription { get; set; }
        public bool IsVisible { get; set; }
        public bool IsActive { get; set; }
        public bool RequiresConsent { get; set; }
        public bool IsOneWay { get; set; }
    }

    public class HealingZoneMetadataDto
    {
        public string HealingDice { get; set; }
        public string Description { get; set; }
        public string HealingTrigger { get; set; }
        public bool OncePerCreature { get; set; }
        public List<Guid> HealedTokens { get; set; }
        public bool HasCharges { get; set; }
        public int ChargesRemaining { get; set; }
        public bool RemovesConditions { get; set; }
        public string ConditionsRemoved { get; set; }
    }

    public class SpawnMetadataDto
    {
        public string CreatureTemplateId { get; set; }
        public string CreatureName { get; set; }
        public int SpawnCount { get; set; }
        public int SpawnRadius { get; set; }
        public string TriggerCondition { get; set; }
        public int SpawnOnRound { get; set; }
        public int TriggerDistance { get; set; }
        public bool HasSpawned { get; set; }
        public bool IsReusable { get; set; }
        public int SpawnDelay { get; set; }
    }

    public class TriggerMetadataDto
    {
        public string EventDescription { get; set; }
        public string ScriptCommand { get; set; }
        public bool TriggerOnce { get; set; }
        public int DelayRounds { get; set; }
    }
}
