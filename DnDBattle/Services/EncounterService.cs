using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DnDBattle.Services
{
    public static class EncounterService
    {
        public static void SaveEncounterToFile(EncounterDto dto, string filePath)
        {
            var folder = Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory;

            if (!string.IsNullOrEmpty(dto.MapImagePath) && Path.IsPathRooted(dto.MapImagePath) && File.Exists(dto.MapImagePath))
            {
                try
                {
                    var destName = Path.GetFileName(dto.MapImagePath);
                    var destPath = Path.Combine(folder, destName);
                    File.Copy(dto.MapImagePath, destPath, true);

                    dto.MapImagePath = destName;
                }
                catch { }
            }

            foreach (var t in dto.Tokens)
            {
                if (!string.IsNullOrEmpty(t.ImagePath) && Path.IsPathRooted(t.ImagePath) && File.Exists(t.ImagePath))
                {
                    try
                    {
                        var destName = Path.GetFileName(t.ImagePath);
                        var destPath = Path.Combine(folder, destName);
                        File.Copy(t.ImagePath, destPath, overwrite: true);
                        t.ImagePath = destName;
                    }
                    catch { }
                }
            }

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public static EncounterDto LoadEncounterFromFile(string filePath)
        {
            var folder = Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory;
            var json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<EncounterDto>(json, new JsonSerializerOptions { WriteIndented = true });
            if (dto == null) return new EncounterDto();

            if (!string.IsNullOrEmpty(dto.MapImagePath))
            {
                if (!Path.IsPathRooted(dto.MapImagePath))
                {
                    var canidate = Path.Combine(folder, dto.MapImagePath);
                    if (File.Exists(canidate)) dto.MapImagePath = canidate;
                }
                else
                    if (!File.Exists(dto.MapImagePath)) dto.MapImagePath = null;
            }

            foreach (var t in dto.Tokens)
            {
                if (!string.IsNullOrEmpty(t.ImagePath))
                {
                    if (!Path.IsPathRooted(t.ImagePath))
                    {
                        var cand = Path.Combine(folder, t.ImagePath);
                        if (File.Exists(cand)) t.ImagePath = cand;
                    }
                    else
                        if (!File.Exists(t.ImagePath)) t.ImagePath = null;
                }
            }
            return dto;
        }

        public static TokenDto TokenToDto(Token t)
        {
            if (t == null) return null;
            return new TokenDto()
            {
                Id = t.Id.ToString(),
                Name = t.Name,
                Size = t.Size,
                Type = t.Type,
                Alignment = t.Alignment,
                ChallengeRating = t.ChallengeRating,
                AC = t.ArmorClass,
                MaxHP = t.MaxHP,
                HitDice = t.HitDice,
                InitiativeMod = t.InitiativeModifier,
                Speed = t.Speed,
                Str = t.Str,
                Dex = t.Dex,
                Con = t.Con,
                Int = t.Int,
                Wis = t.Wis,
                Cha = t.Cha,
                Skills = t.Skills?.ToList() ?? new List<string>(),
                Senses = t.Senses,
                Languages = t.Languages,
                Immunities = t.Immunities,
                Resistances = t.Resistances,
                Vulnerabilities = t.Vulnerabilities,
                Traits = t.Traits,
                Actions = MapActionsToObjects(t.Actions),
                BonusActions = MapActionsToObjects(t.BonusActions),
                Reactions = MapActionsToObjects(t.Reactions),
                LegendaryActions = MapActionsToObjects(t.LegendaryActions),
                Notes = t.Notes,
                IconPath = t.IconPath,
                GridX = t.GridX,
                GridY = t.GridY,
                SizeInSquares = t.SizeInSquares,
                ImagePath = t.IconPath ?? t.IconPath
            };
        }

        public static Token DtoToToken(TokenDto dto)
        {
            if (dto == null) return null;
            var t = new Token();
            if (!string.IsNullOrEmpty(dto.Id) && Guid.TryParse(dto.Id, out Guid g)) t.Id = g;
            t.Name = dto.Name;
            t.Size = dto.Size;
            t.Type = dto.Type;
            t.Alignment = dto.Alignment;
            t.ChallengeRating = dto.ChallengeRating;
            t.ArmorClass = dto.AC;
            t.MaxHP = dto.MaxHP;
            t.HP = dto.MaxHP;
            t.HitDice = dto.HitDice;
            t.InitiativeModifier = dto.InitiativeMod;
            t.Speed = dto.Speed;
            t.Str = dto.Str;
            t.Dex = dto.Dex;
            t.Con = dto.Con;
            t.Int = dto.Int;
            t.Wis = dto.Wis;
            t.Cha = dto.Cha;
            t.Skills = dto.Skills ?? new List<string>();
            t.Senses = dto.Senses;
            t.Languages = dto.Languages;
            t.Immunities = dto.Immunities;
            t.Resistances = dto.Resistances;
            t.Vulnerabilities = dto.Vulnerabilities;
            t.Traits = dto.Traits;
            t.Actions = MapObjectsToActions(dto.Actions);
            t.BonusActions = MapObjectsToActions(dto.BonusActions);
            t.Reactions = MapObjectsToActions(dto.Reactions);
            t.LegendaryActions = MapObjectsToActions(dto.LegendaryActions);
            t.Notes = dto.Notes;
            t.IconPath = dto.IconPath;
            t.GridX = dto.GridX;
            t.GridY = dto.GridY;
            t.SizeInSquares = dto.SizeInSquares;

            if (!string.IsNullOrEmpty(dto.ImagePath))
            {
                try
                {
                    if (File.Exists(dto.ImagePath))
                    {
                        t.IconPath = dto.ImagePath;
                    }
                    else
                    {
                        // leave as-is; may be relative to encounter folder and resolved earlier by LoadEncounterFromFile
                        t.IconPath = dto.ImagePath;
                    }
                }
                catch { t.IconPath = dto.ImagePath; }
            }
            return t;
        }

        private static List<object> MapActionsToObjects(List<Models.Action> lst)
        {
            var outList = new List<object>();
            if (lst == null) return outList;
            foreach (var a in lst)
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

        private static List<Models.Action> MapObjectsToActions(List<object> objs)
        {
            var outList = new List<Models.Action>();
            if (objs == null) return outList;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                foreach (var o in objs)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(o);
                        var a = JsonSerializer.Deserialize<Models.Action>(json, opts);
                        if (a != null) outList.Add(a);
                    }
                    catch { }
                }
            }
            catch { }
            return outList;
        }
    }
}
