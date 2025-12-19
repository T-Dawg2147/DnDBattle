using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace DnDBattle.Services
{
    public static class OptionsService
    {
        private static readonly string AppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DnDBattle");
        private static readonly string ConfigFile = Path.Combine(AppFolder, "config.json");

        private class OptionsDto
        {
            public double ShadowSoftnessPx { get; set; }
            public double PathSpeedSquaresPerSecond { get; set; }
            public int MaxRaycaseAngles { get; set; }
            public int MaxAStarNodes { get; set; }
            public bool DefaultLockToGrid { get; set; } = true;
            public bool EnabledPeriodicAutosave { get; set; }
            public int AutosaveIntervalSeconds { get; set; }
            public double DefaultGridCellSize { get; set; }
            public int GridMaxWidth { get; set; }
            public int GridMaxHeight { get; set; }
            public bool AutoResolveAOOs { get; set; }
            public bool LiveMode { get; set; }
        }

        public static void SaveOptions()
        {
            try
            {
                if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
                var dto = new OptionsDto()
                {
                    ShadowSoftnessPx = Options.ShadowSoftnessPx,
                    PathSpeedSquaresPerSecond = Options.PathSpeedSquaresPerSecond,
                    MaxRaycaseAngles = Options.MaxRaycaseAngles,
                    MaxAStarNodes = Options.MaxAStarNodes,
                    DefaultLockToGrid = Options.DefaultLockToGrid,
                    EnabledPeriodicAutosave = Options.EnabledPeriodicAutosave,
                    AutosaveIntervalSeconds = Options.AutosaveIntervalSeconds,
                    DefaultGridCellSize = Options.DefaultGridCellSize,
                    GridMaxWidth = Options.GridMaxWidth,
                    GridMaxHeight = Options.GridMaxHeight,
                    AutoResolveAOOs = Options.AutoResolveAOOs,
                    LiveMode = Options.LiveMode
                };
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OptionsService.SaveOptions failed: {ex}");
            }
        }

        public static void LoadOptions()
        {
            try
            {
                if (!File.Exists(ConfigFile)) return;
                var json = File.ReadAllText(ConfigFile);
                var dto = JsonSerializer.Deserialize<OptionsDto>(json);
                if (dto == null) return;

                Options.ShadowSoftnessPx = dto.ShadowSoftnessPx;
                Options.PathSpeedSquaresPerSecond = dto.PathSpeedSquaresPerSecond;
                Options.MaxRaycaseAngles = dto.MaxRaycaseAngles;
                Options.MaxAStarNodes = dto.MaxAStarNodes;
                Options.DefaultLockToGrid = dto.DefaultLockToGrid;
                Options.EnabledPeriodicAutosave = dto.EnabledPeriodicAutosave;
                Options.AutosaveIntervalSeconds = dto.AutosaveIntervalSeconds;
                Options.DefaultGridCellSize = dto.DefaultGridCellSize;
                Options.GridMaxWidth = dto.GridMaxWidth;
                Options.GridMaxHeight = dto.GridMaxHeight;
                Options.AutoResolveAOOs = dto.AutoResolveAOOs;
                Options.LiveMode = dto.LiveMode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OptionsService.LoadOptions failed: {ex}");
            }
        }
    }
}
