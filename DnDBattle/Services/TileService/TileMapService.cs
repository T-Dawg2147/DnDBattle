using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DnDBattle.Services.TileService
{
    public class TileMapService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public TileMapService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        #region Public Methods

        /// <summary>
        /// Saves a tile map to JSON file
        /// </summary>
        public async Task<string> SaveMapAsync(TileMap tileMap, string filePath)
        {
            try
            {
                Debug.WriteLine($"[TileMapService] Saving map: {tileMap.Name}");

                // Convert TileMap model to DTO
                var dto = new TileMapDto
                {
                    Id = tileMap.Id.ToString(),
                    Name = tileMap.Name,
                    Width = tileMap.Width,
                    Height = tileMap.Height,
                    CellSize = tileMap.CellSize,
                    BackgroundColor = ColorToHex(tileMap.BackgroundColor),
                    ShowGrid = tileMap.ShowGrid,
                    CreatedDate = tileMap.CreatedDate,
                    ModifiedDate = DateTime.UtcNow,

                    // Remove duplicates and convert tiles
                    Tiles = tileMap.PlacedTiles
                        .Where(t => t != null)
                        .GroupBy(t => t.Id)
                        .Select(g => g.First())
                        .Select(t => PlacedTileToDto(t))
                        .ToList()
                };

                Debug.WriteLine($"[TileMapService] Saving {dto.Tiles.Count} unique tiles");

                // Serialize to JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(dto, options);
                Debug.WriteLine($"[TileMapService] JSON size: {json.Length:N0} characters");

                await File.WriteAllTextAsync(filePath, json);
                Debug.WriteLine($"[TileMapService] Saved to: {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMapService] Save error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads a tile map from JSON file
        /// </summary>
        public async Task<TileMap> LoadMapAsync(string filePath)
        {
            try
            {
                Debug.WriteLine($"[TileMapService] Loading map from: {filePath}");

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Tile map file not found: {filePath}");
                }

                // Read JSON
                string json = await File.ReadAllTextAsync(filePath);
                Debug.WriteLine($"[TileMapService] Read {json.Length:N0} characters");

                // Deserialize
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                TileMapDto dto = await Task.Run(() =>
                    JsonSerializer.Deserialize<TileMapDto>(json, options)
                );

                if (dto == null)
                {
                    throw new Exception("Failed to deserialize tile map");
                }

                Debug.WriteLine($"[TileMapService] Deserialized: {dto.Name}");
                Debug.WriteLine($"[TileMapService] Size: {dto.Width}×{dto.Height}");
                Debug.WriteLine($"[TileMapService] Tiles: {dto.Tiles?.Count ?? 0}");

                // Convert DTO to model
                var tileMap = await DtoToMapAsync(dto);

                Debug.WriteLine($"[TileMapService] Load complete!");
                return tileMap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMapService] Load failed: {ex.Message}");
                Debug.WriteLine($"[TileMapService] Stack: {ex.StackTrace}");
                throw;
            }
        }

        #endregion

        #region Color Conversion

        private string ColorToHex(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Colors.DarkSlateGray;
            
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Colors.DarkSlateGray;
            }
        }

        #endregion

        #region TileMap Conversion

        /// <summary>
        /// Converts TileMapDto to TileMap model
        /// </summary>
        private async Task<TileMap> DtoToMapAsync(TileMapDto dto)
        {
            return await Task.Run(() =>
            {
                var tileMap = new TileMap
                {
                    Id = Guid.TryParse(dto.Id, out var id) ? id : Guid.NewGuid(),
                    Name = dto.Name ?? "Untitled Map",
                    Width = dto.Width,
                    Height = dto.Height,
                    CellSize = dto.CellSize,
                    BackgroundColor = HexToColor(dto.BackgroundColor),
                    ShowGrid = dto.ShowGrid,
                    CreatedDate = dto.CreatedDate,
                    ModifiedDate = dto.ModifiedDate,
                    PlacedTiles = new List<PlacedTile>()
                };

                // Convert tiles
                if (dto.Tiles != null)
                {
                    foreach (var tileDto in dto.Tiles)
                    {
                        var tile = DtoToPlacedTile(tileDto);
                        tileMap.PlacedTiles.Add(tile);
                    }
                }

                Debug.WriteLine($"[TileMapService] Converted {tileMap.PlacedTiles.Count} tiles");
                return tileMap;
            });
        }

        #endregion

        #region Tile Conversion

        /// <summary>
        /// Converts PlacedTile model to TileDto
        /// </summary>
        private TileDto PlacedTileToDto(PlacedTile tile)
        {
            return new TileDto
            {
                InstanceId = tile.Id.ToString(),
                TileDefinitionId = tile.TileDefinitionId.ToString(),
                GridX = tile.GridX,
                GridY = tile.GridY,
                Rotation = tile.Rotation,
                FlipHorizontal = tile.FlipHorizontal,
                FlipVertical = tile.FlipVertical,
                ZIndex = tile.ZIndex,
                Notes = tile.Notes,
                Metadata = tile.Metadata?
                    .Select(m => MetadataToDto(m))
                    .Where(dto => dto != null)
                    .ToList() ?? new List<MetadataDto>()
            };
        }

        /// <summary>
        /// Converts TileDto to PlacedTile model
        /// </summary>
        private PlacedTile DtoToPlacedTile(TileDto dto)
        {
            var tile = new PlacedTile
            {
                Id = Guid.TryParse(dto.InstanceId, out var id) ? id : Guid.NewGuid(),
                TileDefinitionId = Guid.TryParse(dto.TileDefinitionId, out var defId) ? defId : Guid.Empty,
                GridX = dto.GridX,
                GridY = dto.GridY,
                Rotation = dto.Rotation,
                FlipHorizontal = dto.FlipHorizontal,
                FlipVertical = dto.FlipVertical,
                ZIndex = dto.ZIndex,
                Notes = dto.Notes,
                Metadata = new ObservableCollection<TileMetadata>()
            };

            // Deserialize metadata
            if (dto.Metadata != null)
            {
                foreach (var metaDto in dto.Metadata)
                {
                    var metadata = DtoToMetadata(metaDto);
                    if (metadata != null)
                    {
                        tile.Metadata.Add(metadata);
                    }
                }
            }

            return tile;
        }

        #endregion

        #region Metadata Conversion

        /// <summary>
        /// Converts TileMetadata to MetadataDto (simple wrapper)
        /// </summary>
        private MetadataDto MetadataToDto(TileMetadata metadata)
        {
            try
            {
                return new MetadataDto
                {
                    Type = metadata.Type.ToString(),
                    Data = JsonSerializer.Serialize(metadata, metadata.GetType(), _jsonOptions)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMapService] Error serializing metadata: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts MetadataDto to TileMetadata (deserializes based on type)
        /// </summary>
        private TileMetadata DtoToMetadata(MetadataDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Type) || string.IsNullOrEmpty(dto.Data))
                {
                    return null;
                }

                // Parse type
                if (!Enum.TryParse<TileMetadataType>(dto.Type, out var metadataType))
                {
                    Debug.WriteLine($"[TileMapService] Unknown metadata type: {dto.Type}");
                    return null;
                }

                // Deserialize based on type
                TileMetadata metadata = metadataType switch
                {
                    TileMetadataType.Trap => JsonSerializer.Deserialize<TrapMetadata>(dto.Data, _jsonOptions),
                    TileMetadataType.Secret => JsonSerializer.Deserialize<SecretMetadata>(dto.Data, _jsonOptions),
                    TileMetadataType.Interactive => JsonSerializer.Deserialize<InteractiveMetadata>(dto.Data, _jsonOptions),
                    TileMetadataType.Trigger => JsonSerializer.Deserialize<TriggerMetadata>(dto.Data, _jsonOptions),
                    TileMetadataType.Hazard => JsonSerializer.Deserialize<HazardMetadata>(dto.Data, _jsonOptions),
                    TileMetadataType.Teleporter => JsonSerializer.Deserialize<TeleporterMetadata>(dto.Data, _jsonOptions),
                    TileMetadataType.Healing => JsonSerializer.Deserialize<HealingZoneMetadata>(dto.Data, _jsonOptions),
                    TileMetadataType.Spawn => JsonSerializer.Deserialize<SpawnMetadata>(dto.Data, _jsonOptions),
                    _ => null
                };

                return metadata;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMapService] Error deserializing metadata: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}