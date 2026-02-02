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

        public async Task<Models.Tiles.TileMap> LoadMapAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var dto = JsonSerializer.Deserialize<TileMapDto>(json, _jsonOptions);

                var map = DtoToMap(dto);
                Debug.WriteLine($"[TileMapService] Loaded map from {filePath}");
                return map;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMapService] Load failed: {ex.Message}");
                return null;
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
    }
}
