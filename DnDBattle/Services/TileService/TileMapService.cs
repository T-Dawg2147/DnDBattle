using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services.TileService;

namespace DnDBattle.Services.TileService
{
    public class TileMapService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public TileMapService()
        {
            _jsonOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<bool> SaveMapAsync(Models.Tiles.TileMap map, string filePath)
        {
            try
            {
                var dto = MapToDto(map);
                var json = JsonSerializer.Serialize(dto, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                Debug.WriteLine($"[TileMapService] Saved map to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMapService] Save failed: {ex.Message}");
                return false;
            }
        }

        public async Task<TileMap> LoadMapAsync(string filePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Loading map from: {filePath}");

                // Check file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Tile map file not found: {filePath}");
                }

                // Read JSON
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Reading file...");
                string json = await File.ReadAllTextAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"[TileMapService] File size: {json.Length} characters");

                // Try to deserialize
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Deserializing JSON...");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // ← Ignore case differences
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                TileMapDto dto;
                try
                {
                    dto = JsonSerializer.Deserialize<TileMapDto>(json, options);
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[TileMapService] JSON Deserialization failed!");
                    System.Diagnostics.Debug.WriteLine($"[TileMapService] Error: {jsonEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[TileMapService] Line: {jsonEx.LineNumber}, Position: {jsonEx.BytePositionInLine}");

                    // Show first 500 chars of JSON for debugging
                    System.Diagnostics.Debug.WriteLine($"[TileMapService] JSON preview: {json.Substring(0, Math.Min(500, json.Length))}");

                    throw new Exception($"Invalid tile map file format.\n\nJSON Error at line {jsonEx.LineNumber}: {jsonEx.Message}", jsonEx);
                }

                if (dto == null)
                {
                    throw new Exception("Deserialized tile map is null");
                }

                System.Diagnostics.Debug.WriteLine($"[TileMapService] Deserialization successful!");
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Map: {dto.Name}");
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Size: {dto.Width}×{dto.Height}");
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Tiles: {dto.Tiles?.Count ?? 0}");

                // Convert DTO to TileMap
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Converting DTO to TileMap...");
                var tileMap = DtoToMap(dto);

                System.Diagnostics.Debug.WriteLine($"[TileMapService] Load complete!");
                return tileMap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TileMapService] LOAD FAILED: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private TileMapDto MapToDto(Models.Tiles.TileMap map)
        {
            return new TileMapDto
            {
                Id = map.Id,
                Name = map.Name,
                Width = map.Width,
                Height = map.Height,
                CellSize = map.CellSize,
                BackgroundColor = map.BackgroundColor,
                ShowGrid = map.ShowGrid,
                CreatedDate = map.CreatedDate,
                ModifiedDate = map.ModifiedDate,
                Tiles = map.PlacedTiles.Select(t => new TileDto
                {
                    InstanceId = t.InstanceId,
                    TileDefinitionId = t.TileDefinitionId,
                    GridX = t.GridX,
                    GridY = t.GridY,
                    Rotation = t.Rotation,
                    FlipHorizontal = t.FlipHorizontal,
                    FlipVertical = t.FlipVertical,
                    ZIndex = t.ZIndex,
                    Notes = t.Notes
                }).ToList()
            };
        }

        private Models.Tiles.TileMap DtoToMap(TileMapDto dto)
        {
            var map = new Models.Tiles.TileMap
            {
                Id = dto.Id,
                Name = dto.Name,
                Width = dto.Width,
                Height = dto.Height,
                CellSize = dto.CellSize,
                BackgroundColor = dto.BackgroundColor,
                ShowGrid = dto.ShowGrid,
                CreatedDate = dto.CreatedDate,
                ModifiedDate = dto.ModifiedDate,
                PlacedTiles = new ObservableCollection<Tile>(
                    dto.Tiles.Select(t => new Tile
                    {
                        InstanceId = t.InstanceId,
                        TileDefinitionId = t.TileDefinitionId,
                        GridX = t.GridX,
                        GridY = t.GridY,
                        Rotation = t.Rotation,
                        FlipHorizontal = t.FlipHorizontal,
                        FlipVertical = t.FlipVertical,
                        ZIndex = t.ZIndex,
                        Notes = t.Notes
                    })
                )
            };

            return map;
        }

        /// <summary>
        /// Convert Tile to DTO with metadata
        /// </summary>
        private TileDto TileToDto(Tile tile)
        {
            var dto = new TileDto
            {
                InstanceId = tile.InstanceId,
                TileDefinitionId = tile.TileDefinitionId,
                GridX = tile.GridX,
                GridY = tile.GridY,
                Rotation = tile.Rotation,
                FlipHorizontal = tile.FlipHorizontal,
                FlipVertical = tile.FlipVertical,
                ZIndex = tile.ZIndex,
                Notes = tile.Notes,
                Metadata = tile.Metadata.Select(m => MetadataToDto(m)).ToList()
            };

            return dto;
        }

        /// <summary>
        /// Convert DTO to Tile with metadata
        /// </summary>
        private Tile DtoToTile(TileDto dto)
        {
            var tile = new Tile
            {
                InstanceId = dto.InstanceId,
                TileDefinitionId = dto.TileDefinitionId,
                GridX = dto.GridX,
                GridY = dto.GridY,
                Rotation = dto.Rotation,
                FlipHorizontal = dto.FlipHorizontal,
                FlipVertical = dto.FlipVertical,
                ZIndex = dto.ZIndex,
                Notes = dto.Notes,
                Metadata = new ObservableCollection<TileMetadata>(
                    dto.Metadata.Select(m => DtoToMetadata(m)).Where(m => m != null)
                )
            };

            return tile;
        }

        /// <summary>
        /// Convert metadata to DTO
        /// </summary>
        private TileMetadataDto MetadataToDto(TileMetadata metadata)
        {
            var dto = new TileMetadataDto
            {
                Id = metadata.Id,
                Type = metadata.Type.ToString(),
                Name = metadata.Name,
                IsVisibleToPlayers = metadata.IsVisibleToPlayers,
                IsTriggered = metadata.IsTriggered,
                DMNotes = metadata.DMNotes,
                IsEnabled = metadata.IsEnabled
            };

            // Serialize type-specific data
            switch (metadata)
            {
                case TrapMetadata trap:
                    dto.Data = JsonSerializer.Serialize(TrapToDto(trap), _jsonOptions);
                    break;
                case SecretMetadata secret:
                    dto.Data = JsonSerializer.Serialize(SecretToDto(secret), _jsonOptions);
                    break;
                case InteractiveMetadata interactive:
                    dto.Data = JsonSerializer.Serialize(InteractiveToDto(interactive), _jsonOptions);
                    break;
                case HazardMetadata hazard:
                    dto.Data = JsonSerializer.Serialize(HazardToDto(hazard), _jsonOptions);
                    break;
                case TeleporterMetadata teleporter:
                    dto.Data = JsonSerializer.Serialize(TeleporterToDto(teleporter), _jsonOptions);
                    break;
                case HealingZoneMetadata healing:
                    dto.Data = JsonSerializer.Serialize(HealingToDto(healing), _jsonOptions);
                    break;
                case SpawnMetadata spawn:
                    dto.Data = JsonSerializer.Serialize(SpawnToDto(spawn), _jsonOptions);
                    break;
                case TriggerMetadata trigger:
                    dto.Data = JsonSerializer.Serialize(TriggerToDto(trigger), _jsonOptions);
                    break;
            }

            return dto;
        }

        /// <summary>
        /// Convert DTO to metadata
        /// </summary>
        private TileMetadata DtoToMetadata(TileMetadataDto dto)
        {
            TileMetadata metadata = null;

            try
            {
                switch (dto.Type)
                {
                    case "Trap":
                        var trapData = JsonSerializer.Deserialize<TrapMetadataDto>(dto.Data, _jsonOptions);
                        metadata = DtoToTrap(trapData);
                        break;
                    case "Secret":
                        var secretData = JsonSerializer.Deserialize<SecretMetadataDto>(dto.Data, _jsonOptions);
                        metadata = DtoToSecret(secretData);
                        break;
                    case "Interactive":
                        var interactiveData = JsonSerializer.Deserialize<InteractiveMetadataDto>(dto.Data, _jsonOptions);
                        metadata = DtoToInteractive(interactiveData);
                        break;
                    case "Hazard":
                        var hazardData = JsonSerializer.Deserialize<HazardMetadataDto>(dto.Data, _jsonOptions);
                        metadata = DtoToHazard(hazardData);
                        break;
                    case "Teleporter":
                        var teleporterData = JsonSerializer.Deserialize<TeleporterMetadataDto>(dto.Data, _jsonOptions);
                        metadata = DtoToTeleporter(teleporterData);
                        break;
                    case "Healing":
                        var healingData = JsonSerializer.Deserialize<HealingZoneMetadataDto>(dto.Data, _jsonOptions);
                        metadata = DtoToHealing(healingData);
                        break;
                    case "Spawn":
                        var spawnData = JsonSerializer.Deserialize<SpawnMetadataDto>(dto.Data, _jsonOptions);
                        metadata = DtoToSpawn(spawnData);
                        break;
                    case "Trigger":
                        var triggerData = JsonSerializer.Deserialize<TriggerMetadataDto>(dto.Data, _jsonOptions);
                        metadata = DtoToTrigger(triggerData);
                        break;
                }

                if (metadata != null)
                {
                    metadata.Id = dto.Id;
                    metadata.Name = dto.Name;
                    metadata.IsVisibleToPlayers = dto.IsVisibleToPlayers;
                    metadata.IsTriggered = dto.IsTriggered;
                    metadata.DMNotes = dto.DMNotes;
                    metadata.IsEnabled = dto.IsEnabled;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMapService] Failed to deserialize {dto.Type} metadata: {ex.Message}");
            }

            return metadata;
        }

        #region Trap Serialization

        private TrapMetadataDto TrapToDto(TrapMetadata trap)
        {
            return new TrapMetadataDto
            {
                AutoRollDetection = trap.AutoRollDetection,
                AutoRollDisarm = trap.AutoRollDisarm,
                AutoRollSave = trap.AutoRollSave,
                AutoRollDamage = trap.AutoRollDamage,
                DetectionDC = trap.DetectionDC,
                IsDetected = trap.IsDetected,
                DetectionDescription = trap.DetectionDescription,
                CanBeDisarmed = trap.CanBeDisarmed,
                DisarmSkill = trap.DisarmSkill,
                DisarmDC = trap.DisarmDC,
                IsDisarmed = trap.IsDisarmed,
                FailedDisarmTriggersTrap = trap.FailedDisarmTriggersTrap,
                TriggerType = trap.TriggerType.ToString(),
                SaveAbility = trap.SaveAbility,
                SaveDC = trap.SaveDC,
                AreaOfEffect = trap.AreaOfEffect,
                DamageDice = trap.DamageDice,
                DamageType = trap.DamageType.ToString(),
                HalfDamageOnSave = trap.HalfDamageOnSave,
                AdditionalEffects = trap.AdditionalEffects,
                IsReusable = trap.IsReusable,
                ResetTimeRounds = trap.ResetTimeRounds,
                MaxTriggers = trap.MaxTriggers,
                TimesTriggered = trap.TimesTriggered,
                TriggerDescription = trap.TriggerDescription,
                EffectDescription = trap.EffectDescription
            };
        }

        private TrapMetadata DtoToTrap(TrapMetadataDto dto)
        {
            return new TrapMetadata
            {
                AutoRollDetection = dto.AutoRollDetection,
                AutoRollDisarm = dto.AutoRollDisarm,
                AutoRollSave = dto.AutoRollSave,
                AutoRollDamage = dto.AutoRollDamage,
                DetectionDC = dto.DetectionDC,
                IsDetected = dto.IsDetected,
                DetectionDescription = dto.DetectionDescription,
                CanBeDisarmed = dto.CanBeDisarmed,
                DisarmSkill = dto.DisarmSkill,
                DisarmDC = dto.DisarmDC,
                IsDisarmed = dto.IsDisarmed,
                FailedDisarmTriggersTrap = dto.FailedDisarmTriggersTrap,
                TriggerType = Enum.Parse<TrapTriggerType>(dto.TriggerType),
                SaveAbility = dto.SaveAbility,
                SaveDC = dto.SaveDC,
                AreaOfEffect = dto.AreaOfEffect,
                DamageDice = dto.DamageDice,
                DamageType = Enum.Parse<DamageType>(dto.DamageType),
                HalfDamageOnSave = dto.HalfDamageOnSave,
                AdditionalEffects = dto.AdditionalEffects ?? new System.Collections.Generic.List<string>(),
                IsReusable = dto.IsReusable,
                ResetTimeRounds = dto.ResetTimeRounds,
                MaxTriggers = dto.MaxTriggers,
                TimesTriggered = dto.TimesTriggered,
                TriggerDescription = dto.TriggerDescription,
                EffectDescription = dto.EffectDescription
            };
        }

        #endregion

        #region Secret Serialization

        private SecretMetadataDto SecretToDto(SecretMetadata secret)
        {
            return new SecretMetadataDto
            {
                InvestigationDC = secret.InvestigationDC,
                IsDiscovered = secret.IsDiscovered,
                DiscoveryDescription = secret.DiscoveryDescription,
                SecretKind = secret.SecretKind.ToString(),
                Direction = secret.Direction,
                TreasureDescription = secret.TreasureDescription,
                RequiresActivation = secret.RequiresActivation,
                ActivationDescription = secret.ActivationDescription
            };
        }

        private SecretMetadata DtoToSecret(SecretMetadataDto dto)
        {
            return new SecretMetadata
            {
                InvestigationDC = dto.InvestigationDC,
                IsDiscovered = dto.IsDiscovered,
                DiscoveryDescription = dto.DiscoveryDescription,
                SecretKind = Enum.Parse<SecretType>(dto.SecretKind),
                Direction = dto.Direction,
                TreasureDescription = dto.TreasureDescription,
                RequiresActivation = dto.RequiresActivation,
                ActivationDescription = dto.ActivationDescription
            };
        }

        #endregion

        #region Interactive Serialization

        private InteractiveMetadataDto InteractiveToDto(InteractiveMetadata interactive)
        {
            return new InteractiveMetadataDto
            {
                ObjectType = interactive.ObjectType.ToString(),
                State = interactive.State,
                ExamineDescription = interactive.ExamineDescription,
                ActivationEffect = interactive.ActivationEffect,
                SingleUse = interactive.SingleUse,
                TimesActivated = interactive.TimesActivated,
                RequiresCheck = interactive.RequiresCheck,
                RequiredSkill = interactive.RequiredSkill,
                CheckDC = interactive.CheckDC,
                IsLocked = interactive.IsLocked,
                LockedDescription = interactive.LockedDescription,
                GoldPieces = interactive.GoldPieces,
                ContainedItems = interactive.ContainedItems,
                HasBeenLooted = interactive.HasBeenLooted
            };
        }

        private InteractiveMetadata DtoToInteractive(InteractiveMetadataDto dto)
        {
            return new InteractiveMetadata
            {
                ObjectType = Enum.Parse<InteractiveType>(dto.ObjectType),
                State = dto.State,
                ExamineDescription = dto.ExamineDescription,
                ActivationEffect = dto.ActivationEffect,
                SingleUse = dto.SingleUse,
                TimesActivated = dto.TimesActivated,
                RequiresCheck = dto.RequiresCheck,
                RequiredSkill = dto.RequiredSkill,
                CheckDC = dto.CheckDC,
                IsLocked = dto.IsLocked,
                LockedDescription = dto.LockedDescription,
                GoldPieces = dto.GoldPieces,
                ContainedItems = dto.ContainedItems,
                HasBeenLooted = dto.HasBeenLooted
            };
        }

        #endregion

        #region Hazard Serialization

        private HazardMetadataDto HazardToDto(HazardMetadata hazard)
        {
            return new HazardMetadataDto
            {
                HazardKind = hazard.HazardKind.ToString(),
                Description = hazard.Description,
                DamageDice = hazard.DamageDice,
                DamageType = hazard.DamageType.ToString(),
                DamageTrigger = hazard.DamageTrigger.ToString(),
                AllowsSave = hazard.AllowsSave,
                SaveAbility = hazard.SaveAbility,
                SaveDC = hazard.SaveDC,
                SaveNegatesDamage = hazard.SaveNegatesDamage,
                SaveHalvesDamage = hazard.SaveHalvesDamage,
                DamagesEachTurn = hazard.DamagesEachTurn,
                PerTurnDamage = hazard.PerTurnDamage
            };
        }

        private HazardMetadata DtoToHazard(HazardMetadataDto dto)
        {
            return new HazardMetadata
            {
                HazardKind = Enum.Parse<HazardType>(dto.HazardKind),
                Description = dto.Description,
                DamageDice = dto.DamageDice,
                DamageType = Enum.Parse<DamageType>(dto.DamageType),
                DamageTrigger = Enum.Parse<HazardTrigger>(dto.DamageTrigger),
                AllowsSave = dto.AllowsSave,
                SaveAbility = dto.SaveAbility,
                SaveDC = dto.SaveDC,
                SaveNegatesDamage = dto.SaveNegatesDamage,
                SaveHalvesDamage = dto.SaveHalvesDamage,
                DamagesEachTurn = dto.DamagesEachTurn,
                PerTurnDamage = dto.PerTurnDamage
            };
        }

        #endregion

        #region Teleporter Serialization

        private TeleporterMetadataDto TeleporterToDto(TeleporterMetadata teleporter)
        {
            return new TeleporterMetadataDto
            {
                DestinationX = teleporter.DestinationX,
                DestinationY = teleporter.DestinationY,
                TeleportDescription = teleporter.TeleportDescription,
                IsVisible = teleporter.IsVisible,
                IsActive = teleporter.IsActive,
                RequiresConsent = teleporter.RequiresConsent,
                IsOneWay = teleporter.IsOneWay
            };
        }

        private TeleporterMetadata DtoToTeleporter(TeleporterMetadataDto dto)
        {
            return new TeleporterMetadata
            {
                DestinationX = dto.DestinationX,
                DestinationY = dto.DestinationY,
                TeleportDescription = dto.TeleportDescription,
                IsVisible = dto.IsVisible,
                IsActive = dto.IsActive,
                RequiresConsent = dto.RequiresConsent,
                IsOneWay = dto.IsOneWay
            };
        }

        #endregion

        #region Healing Serialization

        private HealingZoneMetadataDto HealingToDto(HealingZoneMetadata healing)
        {
            return new HealingZoneMetadataDto
            {
                HealingDice = healing.HealingDice,
                Description = healing.Description,
                HealingTrigger = healing.HealingTrigger.ToString(),
                OncePerCreature = healing.OncePerCreature,
                HealedTokens = healing.HealedTokens,
                HasCharges = healing.HasCharges,
                ChargesRemaining = healing.ChargesRemaining,
                RemovesConditions = healing.RemovesConditions,
                ConditionsRemoved = healing.ConditionsRemoved
            };
        }

        private HealingZoneMetadata DtoToHealing(HealingZoneMetadataDto dto)
        {
            return new HealingZoneMetadata
            {
                HealingDice = dto.HealingDice,
                Description = dto.Description,
                HealingTrigger = Enum.Parse<HealingTrigger>(dto.HealingTrigger),
                OncePerCreature = dto.OncePerCreature,
                HealedTokens = dto.HealedTokens ?? new System.Collections.Generic.List<Guid>(),
                HasCharges = dto.HasCharges,
                ChargesRemaining = dto.ChargesRemaining,
                RemovesConditions = dto.RemovesConditions,
                ConditionsRemoved = dto.ConditionsRemoved
            };
        }

        #endregion

        #region Spawn Serialization

        private SpawnMetadataDto SpawnToDto(SpawnMetadata spawn)
        {
            return new SpawnMetadataDto
            {
                CreatureTemplateId = spawn.CreatureTemplateId,
                CreatureName = spawn.CreatureName,
                SpawnCount = spawn.SpawnCount,
                SpawnRadius = spawn.SpawnRadius,
                TriggerCondition = spawn.TriggerCondition.ToString(),
                SpawnOnRound = spawn.SpawnOnRound,
                TriggerDistance = spawn.TriggerDistance,
                HasSpawned = spawn.HasSpawned,
                IsReusable = spawn.IsReusable,
                SpawnDelay = spawn.SpawnDelay
            };
        }

        private SpawnMetadata DtoToSpawn(SpawnMetadataDto dto)
        {
            return new SpawnMetadata
            {
                CreatureTemplateId = dto.CreatureTemplateId,
                CreatureName = dto.CreatureName,
                SpawnCount = dto.SpawnCount,
                SpawnRadius = dto.SpawnRadius,
                TriggerCondition = Enum.Parse<SpawnTrigger>(dto.TriggerCondition),
                SpawnOnRound = dto.SpawnOnRound,
                TriggerDistance = dto.TriggerDistance,
                HasSpawned = dto.HasSpawned,
                IsReusable = dto.IsReusable,
                SpawnDelay = dto.SpawnDelay
            };
        }

        #endregion

        #region Trigger Serialization

        private TriggerMetadataDto TriggerToDto(TriggerMetadata trigger)
        {
            return new TriggerMetadataDto
            {
                EventDescription = trigger.EventDescription,
                ScriptCommand = trigger.ScriptCommand,
                TriggerOnce = trigger.TriggerOnce,
                DelayRounds = trigger.DelayRounds
            };
        }

        private TriggerMetadata DtoToTrigger(TriggerMetadataDto dto)
        {
            return new TriggerMetadata
            {
                EventDescription = dto.EventDescription,
                ScriptCommand = dto.ScriptCommand,
                TriggerOnce = dto.TriggerOnce,
                DelayRounds = dto.DelayRounds
            };
        }

        #endregion
    }
}
