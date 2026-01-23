using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DnDBattle.Services
{
    public class QuickActionsService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DnDBattle",
            "quickactions.json");

        private static List<QuickAction> _cachedActions;

        public static List<QuickAction> GetQuickActions()
        {
            if (_cachedActions != null)
                return _cachedActions;

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var saved = JsonSerializer.Deserialize<List<QuickAction>>(json);

                    if (saved != null && saved.Count > 0)
                    {
                        var defaults = QuickActionPresets.GetDefaultActions();
                        var merged = MergeActions(saved, defaults);
                        _cachedActions = merged;
                        return _cachedActions;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading quick actions: {ex.Message}");
            }

            _cachedActions = QuickActionPresets.GetDefaultActions();
            return _cachedActions;
        }

        public static List<QuickAction> GetEnabledQuickActions() =>
            GetQuickActions()
            .Where(a => a.IsEnabled)
            .OrderBy(a => a.SortOrder)
            .ToList();

        public static void SaveQuickActions(List<QuickAction> actions)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(actions, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
                File.WriteAllText(ConfigPath, json);

                _cachedActions = actions;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving quick actions: {ex.Message}");
            }
        }

        private static List<QuickAction> MergeActions(List<QuickAction> saved, List<QuickAction> defaults)
        {
            var result = new List<QuickAction>();
            var savedDict = saved.ToDictionary(a => a.Id);

            foreach (var defaultAction in defaults)
            {
                if (savedDict.TryGetValue(defaultAction.Id, out var savedAction))
                    result.Add(savedAction);
                else
                {
                    defaultAction.IsEnabled = false;
                    result.Add(defaultAction);
                }
            }

            foreach (var savedAction in saved)
            {
                if (!defaults.Any(d => d.Id == savedAction.Id))
                    result.Add(savedAction);
            }

            return result.OrderBy(a => a.SortOrder).ToList();
        }

        public static void ResetToDefaults()
        {
            _cachedActions = QuickActionPresets.GetDefaultActions();
            SaveQuickActions(_cachedActions);
        }
    }
}
