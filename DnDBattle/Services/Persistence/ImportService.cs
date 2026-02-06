using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Persistence
{
    public static class ImportService
    {
        public static List<Token> ImportTokensFromJsonFile(string filePath)
        {
            var tokens = new List<Token>();
            var json = File.ReadAllText(filePath);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse import JSON", ex);
            }

            if (doc.RootElement.ValueKind != JsonValueKind.Array) return tokens;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                try
                {
                    var t = new Token();

                    // Id (Guid)
                    if (el.TryGetProperty("Id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                    {
                        if (Guid.TryParse(idProp.GetString(), out Guid g)) t.Id = g;
                    }

                    // Simple fields
                    if (el.TryGetProperty("Name", out var name)) t.Name = name.GetString();
                    if (el.TryGetProperty("Size", out var size)) t.Size = size.GetString();
                    if (el.TryGetProperty("Type", out var type)) t.Type = type.GetString();
                    if (el.TryGetProperty("Alignment", out var align)) t.Alignment = align.GetString();
                    if (el.TryGetProperty("ChallengeRating", out var cr)) t.ChallengeRating = cr.GetString();

                    if (el.TryGetProperty("ArmorClass", out var ac) && ac.TryGetInt32(out int acv)) t.ArmorClass = acv;
                    if (el.TryGetProperty("MaxHP", out var hp) && hp.TryGetInt32(out int hpv)) { t.MaxHP = hpv; t.HP = hpv; }
                    if (el.TryGetProperty("HitDice", out var hd)) t.HitDice = hd.GetString();
                    if (el.TryGetProperty("InitiativeMod", out var im) && im.TryGetInt32(out int imv)) t.InitiativeModifier = imv;
                    if (el.TryGetProperty("Speed", out var sp)) t.Speed = sp.GetString();

                    // ability scores
                    if (el.TryGetProperty("Str", out var s) && s.TryGetInt32(out int sv)) t.Str = sv;
                    if (el.TryGetProperty("Dex", out var d) && d.TryGetInt32(out int dv)) t.Dex = dv;
                    if (el.TryGetProperty("Con", out var c) && c.TryGetInt32(out int cv)) t.Con = cv;
                    if (el.TryGetProperty("Int", out var ii) && ii.TryGetInt32(out int iv)) t.Int = iv;
                    if (el.TryGetProperty("Wis", out var w) && w.TryGetInt32(out int wv)) t.Wis = wv;
                    if (el.TryGetProperty("Cha", out var ch) && ch.TryGetInt32(out int chv)) t.Cha = chv;

                    // lists/strings
                    if (el.TryGetProperty("Skills", out var skills) && skills.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var si in skills.EnumerateArray()) if (si.ValueKind == JsonValueKind.String) t.Skills.Add(si.GetString());
                    }
                    if (el.TryGetProperty("Senses", out var senses)) t.Senses = senses.GetString();
                    if (el.TryGetProperty("Languages", out var langs)) t.Languages = langs.GetString();
                    if (el.TryGetProperty("Immunities", out var imu)) t.Immunities = imu.GetString();
                    if (el.TryGetProperty("Resistances", out var res)) t.Resistances = res.GetString();
                    if (el.TryGetProperty("Vulnerabilities", out var vuln)) t.Vulnerabilities = vuln.GetString();
                    if (el.TryGetProperty("Traits", out var traits)) t.Traits = traits.GetString();
                    if (el.TryGetProperty("Notes", out var notes)) t.Notes = notes.GetString();

                    // IconPath (optional) - try to resolve relative to import file
                    if (el.TryGetProperty("IconPath", out var icon) && icon.ValueKind == JsonValueKind.String)
                    {
                        var path = icon.GetString();
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            try
                            {
                                var candidate = path;
                                if (!Path.IsPathRooted(candidate))
                                    candidate = Path.Combine(Path.GetDirectoryName(filePath) ?? "", path);
                                if (File.Exists(candidate))
                                {
                                    t.IconPath = candidate;
                                    t.Image = new BitmapImage(new Uri(candidate));
                                }
                                else
                                {
                                    t.IconPath = path; // still store it
                                }
                            }
                            catch { t.IconPath = path; }
                        }
                    }

                    // Actions lists
                    MapActionArrayToList(el, "Actions", t.Actions);
                    MapActionArrayToList(el, "BonusActions", t.BonusActions);
                    MapActionArrayToList(el, "Reactions", t.Reactions);
                    MapActionArrayToList(el, "LegendaryActions", t.LegendaryActions);

                    // Extras: capture any fields we don't map explicitly
                    foreach (var prop in el.EnumerateObject())
                    {
                        var nameProp = prop.Name;
                        if (!KnownPropertyNames.Contains(nameProp))
                        {
                            t.Extras[nameProp] = prop.Value.ToString();
                        }
                    }

                    tokens.Add(t);
                }
                catch
                {
                    // ignore per-item errors
                }
            }

            return tokens;
        }

        private static readonly HashSet<string> KnownPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id","Name","Size","Type","Alignment","ChallengeRating","ArmorClass","MaxHP","HitDice","InitiativeMod",
            "Speed","Str","Dex","Con","Int","Wis","Cha","Skills","Senses","Languages","Immunities","Resistances",
            "Vulnerabilities","Traits","Actions","BonusActions","Reactions","LegendaryActions","Notes","IconPath"
        };

        private static void MapActionArrayToList(JsonElement el, string propName, List<Models.Combat.Action> target)
        {
            if (!el.TryGetProperty(propName, out var arr) || arr.ValueKind != JsonValueKind.Array) return;
            foreach (var aEl in arr.EnumerateArray())
            {
                var aa = MapJsonElementToAction(aEl);
                if (aa != null) target.Add(aa);
            }
        }

        private static Models.Combat.Action MapJsonElementToAction(JsonElement el)
        {
            try
            {
                var action = new Models.Combat.Action();

                if (el.ValueKind == JsonValueKind.String)
                {
                    action.Name = el.GetString();
                    return action;
                }

                if (el.TryGetProperty("Name", out var n)) action.Name = n.GetString();
                if (el.TryGetProperty("AttackBonus", out var ab) && ab.TryGetInt32(out var abi)) action.AttackBonus = abi;
                if (el.TryGetProperty("Damage", out var dmg)) action.DamageExpression = dmg.GetString();
                if (string.IsNullOrWhiteSpace(action.DamageExpression))
                {
                    if (el.TryGetProperty("DamageExpression", out var de)) action.DamageExpression = de.GetString();
                }
                if (el.TryGetProperty("Range", out var r)) action.Range = r.GetString();
                if (el.TryGetProperty("Description", out var d)) action.Description = d.GetString();

                if (string.IsNullOrWhiteSpace(action.Name)) action.Name = "Action";
                if (string.IsNullOrWhiteSpace(action.DamageExpression)) action.DamageExpression = string.Empty;

                return action;
            }
            catch
            {
                return null;
            }
        }

        /*private static DnDBattle.Models.Combat.Action MapJsonElementToAttackAction(JsonElement el)
        {
            string error = "Error";
            try
            {
                var action = new DnDBattle.Models.Combat.Action();

                if (el.ValueKind == JsonValueKind.String)
                {
                    action.Name = el.GetString();
                    action.DamageExpression = null;
                    return action;
                }

                if (el.TryGetProperty("Name", out var n)) 
                    action.Name = n.GetString() ?? error;
                if (el.TryGetProperty("AttackBonus", out var ab) && ab.TryGetInt32(out var abi)) 
                    action.AttackBonus = abi;
                if (el.TryGetProperty("Damage", out var dmg)) 
                    action.DamageExpression = dmg.GetString() ?? error;
                if (string.IsNullOrWhiteSpace(action.DamageExpression))
                {
                    if (el.TryGetProperty("DamageExpression", out var de)) action.DamageExpression = de.GetString() ?? error;
                    else if (el.TryGetProperty("Damage_dice", out var dd)) action.DamageExpression = dd.GetString() ?? error;
                }
                if (el.TryGetProperty("Range", out var r)) 
                    action.Range = r.GetString() ?? error;
                if (el.TryGetProperty("Description", out var d)) action.Description = d.GetString();

                if (string.IsNullOrWhiteSpace(action.Name)) action.Name = "Attack";
                if (string.IsNullOrWhiteSpace(action.DamageExpression)) action.DamageExpression = "1d4";

                return action;
            }
            catch
            {
                return null;
            }
        }*/
    }
}
