using DnDBattle.Controls;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
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
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;

namespace DnDBattle.Services.Persistence
{
    public static class AutosaveService
    {
        private static readonly string AppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DnDBattle");
        private static readonly string EncounterFolder = Path.Combine(AppFolder, "encounters");
        private static readonly string AutoFile = Path.Combine(EncounterFolder, "autosave.json");

        public static void SaveEncounter(MainViewModel vm, Controls.BattleGridControl grid)
        {
            try
            {
                if (!Directory.Exists(EncounterFolder)) Directory.CreateDirectory(EncounterFolder);
                var dto = grid?.GetEncounterDto() ?? new BattleGridControl().GetEncounterDto();

                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AutoFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Autosave failed: {ex.Message}");
            }
        }

        public static string GetAutosavePath() => AutoFile;
    }
}
