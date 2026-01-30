using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DnDBattle.Services.Mapping_Services
{
    public class TileMapPersistenceService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly string _mapsFolder;

        public TileMapPersistenceService()
        {
            _mapsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DnDBattle",
                "TileMaps");

            EnsureFolderExists();
        }

        public string MapsFolder => _mapsFolder;

        public async Task<string> SaveMapAsync(TileMap map, string filePath = null)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            map.ModifiedAt = DateTime.UtcNow;

            var path = filePath ?? GetDefaultPath(map);
            var dto = ToDto(map);
            var json = JsonSerializer.Serialize(dto, _jsonOptions);

            await File.WriteAllTextAsync(path, json);
            Debug.WriteLine($"[TileMapPersistence] Saved map to: {path}");

            return path;
        }

        public async Task<TileMap> LoadMapAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Map file not found.", filePath);

            var json = await File.ReadAllTextAsync(filePath);
            var dto = JsonSerializer.Deserialize<TileMapDto>(json, _jsonOptions);

            Debug.WriteLine($"[TIleMapPersistence] Loaded map from: {filePath}");
            return FromDto(dto);
        }

        public IEnumerable<string> GetSavedMaps()
        {
            if (!Directory.Exists(_mapsFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(_mapsFolder, "*.json");
        }

        public bool DeleteMap(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.Write($"[TileMapPersistence] Error deleting map: {ex.Message}");
            }
            return false;
        }

        private string GetDefaultPath(TileMap map)
        {
            var safeName = string.Join("_", map.Name.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_mapsFolder, $"{safeName}_{map.Id:N}.json");
        }

        private void EnsureFolderExists()
        {
            if (!Directory.Exists(_mapsFolder))
                Directory.CreateDirectory(_mapsFolder);
        }

        #region DTO Conversion

        private TileMapDto ToDto(TileMap map)
        {
            return new TileMapDto()
            {
                Id = map.Id,
                Name = map.Name,
                WidthInSquares = map.WidthInSquares,
                HeightInSqaures = map.HeightInSquares,
                BackgroundColor = map.BackgroundColor,
                CreatedAt = map.CreatedAt,
                ModifiedAt = map.ModifiedAt,
                Tiles = map.Tiles.ConvertAll(t => new TileDto()
                {
                    TileDefinitionId = t.TileDefinitionId,
                    GridX = t.GridX,
                    GridY = t.GridY,
                    Rotation = t.Rotation,
                    Layer = t.Layer
                })
            };
        }

        private TileMap FromDto(TileMapDto dto)
        {
            var map = new TileMap()
            {
                Id = dto.Id,
                Name = dto.Name,
                WidthInSquares = dto.WidthInSquares,
                HeightInSquares = dto.HeightInSqaures,
                GridCellSize = dto.GridCellSize,
                BackgroundColor = dto.BackgroundColor,
                CreatedAt = dto.CreatedAt,
                ModifiedAt = dto.ModifiedAt
            };

            foreach (var tileDto in dto.Tiles)
            {
                map.Tiles.Add(new Tile()
                {
                    TileDefinitionId = tileDto.TileDefinitionId,
                    GridX = tileDto.GridX,
                    GridY = tileDto.GridY,
                    Rotation = tileDto.Rotation,
                    Layer = tileDto.Layer
                });
            }

            return map;
        }

        #endregion
    }

    #region DTOs for JSON Serialization

    public class TileMapDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int WidthInSquares { get; set; }
        public int HeightInSqaures { get; set; }
        public double GridCellSize { get; set; }
        public string BackgroundColor { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public List<TileDto> Tiles { get; set; } = new List<TileDto>();
    }

    public class TileDto
    {
        public string TileDefinitionId { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int Rotation { get; set; }
        public int Layer { get; set; }
    }

    #endregion
}
