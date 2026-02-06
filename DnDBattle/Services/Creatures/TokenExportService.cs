using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Services.Creatures;

namespace DnDBattle.Services.Creatures
{
    public static class TokenExportService
    {
        public static void ExportTokensToJson(string filePath, IEnumerable<Models.Creatures.Token> tokens)
        {
            var list = new List<object>();
            foreach (var t in tokens)
            {
                var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Id"] = t.Id.ToString(),
                    ["Name"] = t.Name,
                    ["Size"] = t.Size,
                    ["Type"] = t.Type,
                    ["Alignment"] = t.Alignment,
                    ["ChallengeRating"] = t.ChallengeRating,
                    ["ArmorClass"] = t.ArmorClass,
                    ["MaxHP"] = t.MaxHP,
                    ["HitDice"] = t.HitDice,
                    ["InitiativeMod"] = t.InitiativeModifier,
                    ["Speed"] = t.Speed,
                    ["Str"] = t.Str,
                    ["Dex"] = t.Dex,
                    ["Con"] = t.Con,
                    ["Int"] = t.Int,
                    ["Wis"] = t.Wis,
                    ["Cha"] = t.Cha,
                    ["Skills"] = t.Skills,
                    ["Senses"] = t.Senses,
                    ["Languages"] = t.Languages,
                    ["Immunities"] = t.Immunities,
                    ["Resistances"] = t.Resistances,
                    ["Vulnerabilities"] = t.Vulnerabilities,
                    ["Traits"] = t.Traits,
                    ["Actions"] = MapAttackActionList(t.Actions),
                    ["BonusActions"] = MapAttackActionList(t.BonusActions),
                    ["Reactions"] = MapAttackActionList(t.Reactions),
                    ["LegendaryActions"] = MapAttackActionList(t.LegendaryActions),
                    ["Notes"] = t.Notes,
                    ["IconPath"] = t.IconPath
                };
                list.Add(obj);
            }

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(list, opts);
            File.WriteAllText(filePath, json);
        }

        private static List<object> MapAttackActionList(List<Models.Combat.Action> actions)
        {
            var outList = new List<object>();
            if (actions == null) return outList;
            foreach (var a in actions)
            {
                outList.Add(new
                {
                    Name = a.Name,
                    AttackBonus = a.AttackBonus,
                    Damage = a.DamageExpression,
                    Range = a.Range,
                    Description = a.Description
                });
            }
            return outList;
        }
    }
}
