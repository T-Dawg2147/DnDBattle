using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Utils;
using DnDBattle.Services;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Combat
{
    /// <summary>
    /// Manages concentration checks when a concentrating token takes damage.
    /// DC = max(10, damage / 2). Auto-breaks concentration on failure.
    /// </summary>
    public class ConcentrationService
    {
        /// <summary>
        /// Performs a concentration check after damage. Returns the result including success/failure.
        /// Automatically breaks concentration on failure when enabled.
        /// </summary>
        public ConcentrationCheckResult CheckConcentration(Token token, int damageTaken)
        {
            if (!Options.EnableConcentrationTracking || !token.IsConcentrating)
                return new ConcentrationCheckResult { Skipped = true };

            int DC = Token.CalculateConcentrationDC(damageTaken);
            var roll = DiceRoller.RollExpression("1d20");
            int modifier = token.ConcentrationSaveModifier;
            int total = roll.Total + modifier;
            bool success = total >= DC;

            var result = new ConcentrationCheckResult
            {
                Token = token,
                SpellName = token.ConcentrationSpell,
                DamageTaken = damageTaken,
                DC = DC,
                D20Roll = roll.Total,
                Modifier = modifier,
                Total = total,
                Success = success
            };

            if (!success)
            {
                token.BreakConcentration();
            }

            return result;
        }

        /// <summary>
        /// Starts concentration on a spell, breaking any previous concentration.
        /// </summary>
        public void StartConcentration(Token token, string spellName)
        {
            if (!Options.EnableConcentrationTracking) return;

            // Drop previous concentration first
            if (token.IsConcentrating)
            {
                token.BreakConcentration();
            }

            token.SetConcentration(spellName);
        }
    }

    /// <summary>
    /// Result of a concentration saving throw check.
    /// </summary>
    public class ConcentrationCheckResult
    {
        public Token? Token { get; set; }
        public string? SpellName { get; set; }
        public int DamageTaken { get; set; }
        public int DC { get; set; }
        public int D20Roll { get; set; }
        public int Modifier { get; set; }
        public int Total { get; set; }
        public bool Success { get; set; }
        public bool Skipped { get; set; }

        public override string ToString()
        {
            if (Skipped) return "";
            string tokenName = Token?.Name ?? "Token";
            string spell = SpellName ?? "Unknown";
            return $"{tokenName} concentration ({spell}): {D20Roll}+{Modifier}={Total} vs DC {DC} - " +
                   (Success ? "MAINTAINED" : "BROKEN!");
        }
    }
}
