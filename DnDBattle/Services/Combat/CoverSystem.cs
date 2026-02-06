using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
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
using DnDBattle.Services.Combat;

namespace DnDBattle.Services.Combat
{
    /// <summary>
    /// Detects cover between an attacker and defender by tracing a line
    /// on the grid and checking for blocking tiles/tokens.
    /// </summary>
    public class CoverSystem
    {
        /// <summary>
        /// Calculates the cover level between attacker and defender.
        /// Uses Bresenham line to find obstacles along the path.
        /// </summary>
        /// <param name="attacker">Attacking token.</param>
        /// <param name="defender">Defending token.</param>
        /// <param name="isBlocked">
        /// Callback that returns true if the cell at (x,y) blocks line-of-sight
        /// (e.g., a wall tile). Null means no obstructions.
        /// </param>
        /// <param name="providesCover">
        /// Callback that returns true if the cell at (x,y) provides partial cover
        /// (e.g., a half-wall or furniture). Null means no cover tiles.
        /// </param>
        public CoverLevel CalculateCover(
            Token attacker,
            Token defender,
            Func<int, int, bool>? isBlocked = null,
            Func<int, int, bool>? providesCover = null)
        {
            if (!Options.EnableCoverSystem)
                return CoverLevel.None;

            if (isBlocked == null && providesCover == null)
                return CoverLevel.None;

            var cells = GetCellsAlongLine(attacker.GridX, attacker.GridY, defender.GridX, defender.GridY);

            bool hasFullBlock = false;
            int coverCount = 0;

            foreach (var (x, y) in cells)
            {
                // Skip start/end cells
                if ((x == attacker.GridX && y == attacker.GridY) ||
                    (x == defender.GridX && y == defender.GridY))
                    continue;

                if (isBlocked != null && isBlocked(x, y))
                {
                    hasFullBlock = true;
                    break;
                }

                if (providesCover != null && providesCover(x, y))
                {
                    coverCount++;
                }
            }

            if (hasFullBlock) return CoverLevel.Full;
            if (coverCount >= 2) return CoverLevel.ThreeQuarters;
            if (coverCount >= 1) return CoverLevel.Half;
            return CoverLevel.None;
        }

        /// <summary>
        /// Returns the effective AC of the defender, including any cover bonus.
        /// </summary>
        public static int GetEffectiveAC(Token defender, CoverLevel cover)
        {
            return defender.ArmorClass + CombatConditionHelper.GetCoverACBonus(cover);
        }

        /// <summary>
        /// Bresenham line algorithm to get cells along a line between two grid points.
        /// </summary>
        public static List<(int x, int y)> GetCellsAlongLine(int x0, int y0, int x1, int y1)
        {
            var cells = new List<(int, int)>();
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                cells.Add((x0, y0));
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }

            return cells;
        }
    }
}
