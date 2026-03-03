using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Encounters
{
    public class EncounterBuilderService
    {
        private static readonly Dictionary<int, int[]> XPThresholds = new Dictionary<int, int[]>
        {
            { 1, new[] { 25, 50, 75, 100 } },
            { 2, new[] { 50, 100, 150, 200 } },
            { 3, new[] { 75, 150, 225, 400 } },
            { 4, new[] { 125, 250, 375, 500 } },
            { 5, new[] { 250, 500, 750, 1100 } },
            { 6, new[] { 300, 600, 900, 1400 } },
            { 7, new[] { 350, 750, 1100, 1700 } },
            { 8, new[] { 450, 900, 1400, 2100 } },
            { 9, new[] { 550, 1100, 1600, 2400 } },
            { 10, new[] { 600, 1200, 1900, 2800 } },
            { 11, new[] { 800, 1600, 2400, 3600 } },
            { 12, new[] { 1000, 2000, 3000, 4500 } },
            { 13, new[] { 1100, 2200, 3400, 5100 } },
            { 14, new[] { 1250, 2500, 3800, 5700 } },
            { 15, new[] { 1400, 2800, 4300, 6400 } },
            { 16, new[] { 1600, 3200, 4800, 7200 } },
            { 17, new[] { 2000, 3900, 5900, 8800 } },
            { 18, new[] { 2100, 4200, 6300, 9500 } },
            { 19, new[] { 2400, 4900, 7300, 10900 } },
            { 20, new[] { 2800, 5700, 8500, 12700 } }
        };

        // XP by Challenge Rating
        private static readonly Dictionary<string, int> CRtoXP = new Dictionary<string, int>
        {
            { "0", 10 }, { "1/8", 25 }, { "1/4", 50 }, { "1/2", 100 },
            { "1", 200 }, { "2", 450 }, { "3", 700 }, { "4", 1100 },
            { "5", 1800 }, { "6", 2300 }, { "7", 2900 }, { "8", 3900 },
            { "9", 5000 }, { "10", 5900 }, { "11", 7200 }, { "12", 8400 },
            { "13", 10000 }, { "14", 11500 }, { "15", 13000 }, { "16", 15000 },
            { "17", 18000 }, { "18", 20000 }, { "19", 22000 }, { "20", 25000 },
            { "21", 33000 }, { "22", 41000 }, { "23", 50000 }, { "24", 62000 },
            { "25", 75000 }, { "26", 90000 }, { "27", 105000 }, { "28", 120000 },
            { "29", 135000 }, { "30", 155000 }
        };

        public int GetXPForCR(string cr)
        {
            if (string.IsNullOrWhiteSpace(cr))
                return 0;

            cr = cr.Trim().ToLower().Replace(" ", "");

            if (int.TryParse(cr, out int numericCR))
                cr = numericCR.ToString();

            if (CRtoXP.TryGetValue(cr, out int xp))
                return xp;

            if (cr.Contains("/"))
                if (CRtoXP.TryGetValue(cr, out xp))
                    return xp;

            return 0;
        }

        public double GetEncounterMultiplier(int monsterCount, int partySize)
        {
            double multiplier;

            if (monsterCount == 1)
                multiplier = 1.0;
            else if (monsterCount == 2)
                multiplier = 1.5;
            else if (monsterCount <= 6)
                multiplier = 2.0;
            else if (monsterCount <= 10)
                multiplier = 2.5;
            else if (monsterCount <= 14)
                multiplier = 3.0;
            else
                multiplier = 4.0;

            if (partySize < 3)
            {
                if (multiplier == 1.0) multiplier = 1.5;
                else if (multiplier == 1.5) multiplier = 2.0;
                else if (multiplier == 2.0) multiplier = 2.5;
                else if (multiplier == 2.5) multiplier = 3.0;
                else if (multiplier == 3.0) multiplier = 4.0;
                else multiplier = 5.0;
            }
            else if (partySize >= 6)
            {
                if (multiplier == 1.5) multiplier = 1.0;
                else if (multiplier == 2.0) multiplier = 1.5;
                else if (multiplier == 2.5) multiplier = 3.0;
                else if (multiplier == 3.0) multiplier = 4.0;
                else multiplier = 5.0;
            }

            return multiplier;
        }

        public (int easy, int medium, int hard, int deadly) GetPartyThresholds(int partySize, int averageLevel)
        {
            averageLevel = Math.Clamp(averageLevel, 1, 20);

            var thresholds = XPThresholds[averageLevel];

            return (
                thresholds[0] * partySize,
                thresholds[1] * partySize,
                thresholds[2] * partySize,
                thresholds[3] * partySize
            );
        }

        public EncounterDifficulty CalculateDifficulty(int adjustedXP, int partySize, int averageLevel)
        {
            var (easy, medium, hard, deadly) = GetPartyThresholds(partySize, averageLevel);

            if (adjustedXP >= deadly)
                return EncounterDifficulty.Deadly;
            if (adjustedXP >= hard)
                return EncounterDifficulty.Hard;
            if (adjustedXP >= medium)
                return EncounterDifficulty.Medium;
            if (adjustedXP >= easy)
                return EncounterDifficulty.Easy;

            return EncounterDifficulty.Trivial;
        }

        public EncounterCalculation CalculateEncounter(IEnumerable<EncounterCreature> creatures, int partySize, int averageLevel)
        {
            var creatureList = creatures.ToList();

            int totalMonsters = creatureList.Sum(c => c.Quantity);
            int totalXP = creatureList.Sum(c => GetXPForCR(c.Creature.ChallengeRating) * c.Quantity);
            double multipler = GetEncounterMultiplier(totalMonsters, partySize);
            int adjustedXP = (int)(totalXP * multipler);
            var difficulty = CalculateDifficulty(adjustedXP, partySize, averageLevel);
            var thresholds = GetPartyThresholds(partySize, averageLevel);

            return new EncounterCalculation()
            {
                TotalCreatures = totalMonsters,
                TotalXP = totalXP,
                Multiplier = multipler,
                AdjustedXP = adjustedXP,
                Difficulty = difficulty,
                EasyThreshold = thresholds.easy,
                MediumThreshold = thresholds.medium,
                HardThreshold = thresholds.hard,
                DeadlyThreshold = thresholds.deadly
            };
        }
    }

    public class EncounterCreature
    {
        public Token Creature { get; set; }
        public int Quantity { get; set; } = 1;

        public int TotalXP => new EncounterBuilderService().GetXPForCR(Creature?.ChallengeRating) * Quantity;
    }

    public class EncounterCalculation
    {
        public int TotalCreatures { get; set; }
        public int TotalXP { get; set; }
        public double Multiplier { get; set; }
        public int AdjustedXP { get; set; }
        public EncounterDifficulty Difficulty { get; set; }
        public int EasyThreshold { get; set; }
        public int MediumThreshold { get; set; }
        public int HardThreshold { get; set; }
        public int DeadlyThreshold { get; set; }
    }

    public enum EncounterDifficulty
    {
        Trivial,
        Easy,
        Medium,
        Hard,
        Deadly
    }

    public static class EncounterDifficultyExtensions
    {
        public static string GetDisplayName(this EncounterDifficulty difficulty)
        {
            return difficulty switch
            {
                EncounterDifficulty.Trivial => "Trivial",
                EncounterDifficulty.Easy => "Easy",
                EncounterDifficulty.Medium => "Medium",
                EncounterDifficulty.Hard => "Hard",
                EncounterDifficulty.Deadly => "Deadly",
                _ => "Unknown"
            };
        }

        public static string GetIcon(this EncounterDifficulty difficulty)
        {
            return difficulty switch
            {
                EncounterDifficulty.Trivial => "😴",
                EncounterDifficulty.Easy => "🟢",
                EncounterDifficulty.Medium => "🟡",
                EncounterDifficulty.Hard => "🔴",
                EncounterDifficulty.Deadly => "💀",
                _ => "❓"
            };
        }

        public static Color GetColor(this EncounterDifficulty difficulty)
        {
            return difficulty switch
            {
                EncounterDifficulty.Trivial => Color.FromRgb(158, 158, 158),
                EncounterDifficulty.Easy => Color.FromRgb(76, 175, 80),
                EncounterDifficulty.Medium => Color.FromRgb(255, 193, 7),
                EncounterDifficulty.Hard => Color.FromRgb(255, 152, 0),
                EncounterDifficulty.Deadly => Color.FromRgb(244, 67, 54),
                _ => Color.FromRgb(158, 158, 158)
            };
        }

    }
}
