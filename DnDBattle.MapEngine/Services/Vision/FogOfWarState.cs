using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;

namespace DnDBattle.Services.Vision
{
    public class FogOfWarState
    {
        private readonly HashSet<(int X, int Y)> _revealedTiles = new HashSet<(int X, int Y)>();

        private readonly HashSet<(int X, int Y)> _visibleTiles = new HashSet<(int X, int Y)>();

        public bool IsEnabled { get; set; } = false;

        public FogMode Mode { get; set; } = FogMode.Exploration;

        public int VisionRange { get; set; } = 12;

        public bool IsTileRevealed(int x, int y) =>
            _revealedTiles.Contains((x, y));

        public bool IsTileVisible(int x, int y) =>
            _revealedTiles.Contains((x, y));

        public void RevealTile(int x, int y) =>
            _revealedTiles.Add((x, y));

        public void AddVisibleTile(int x, int y) =>
            _visibleTiles.Add((x, y));

        public void ClearVisibility() =>
            _revealedTiles.Clear();

        public void RevealArea(int centerX, int centerY, int radius)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    int dist = Math.Abs(x - centerX) + Math.Abs(y - centerY);
                    if (dist <= radius)
                    {
                        RevealTile(x, y);
                        AddVisibleTile(x, y);
                    }
                }
            }
        }

        public void Reset()
        {
            _revealedTiles.Clear();
            _visibleTiles.Clear();
        }

        public IEnumerable<(int x, int y)> GetRevealedTiles() =>
            _revealedTiles;

        public IEnumerable<(int x, int y)> GetVisibleTiles() =>
            _visibleTiles;
    }

    public enum FogMode
    {
        Exploration,
        Dynamic,
        Disabled
    }
}
