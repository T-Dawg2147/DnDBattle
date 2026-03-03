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
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Vision overlay rendering and automatic fog reveal.
    /// Implements features 4.3 (Token Vision), 4.4 (Vision Mode Rendering),
    /// and 4.5 (Automatic Fog Reveal).
    /// </summary>
    public partial class BattleGridControl
    {
        private VisionService _visionService;
        private readonly DrawingVisual _visionVisual = new DrawingVisual();
        private bool _showVisionOverlay = false;

        /// <summary>
        /// Initializes the vision service. Call once after wall service is available.
        /// </summary>
        private void InitializeVisionService()
        {
            _visionService = new VisionService(_wallService);
        }

        /// <summary>
        /// Toggles the vision overlay display on the map.
        /// </summary>
        // VISUAL REFRESH - VISION
        public void ToggleVisionOverlay(bool show)
        {
            _showVisionOverlay = show;
            RedrawVisionOverlay();
        }

        /// <summary>
        /// Redraws the vision overlay for the selected token or all player tokens.
        /// </summary>
        // VISUAL REFRESH - VISION
        public void RedrawVisionOverlay()
        {
            if (_visionService == null) InitializeVisionService();

            using (var dc = _visionVisual.RenderOpen())
            {
                if (!_showVisionOverlay || !Options.EnableTokenVision || Tokens == null)
                    return;

                var playerTokens = Tokens.Where(t => t.IsPlayer).ToList();
                if (playerTokens.Count == 0) return;

                var allVisible = new HashSet<(int x, int y)>();

                foreach (var token in playerTokens)
                {
                    var visible = _visionService.CalculateVisibleCells(
                        token, _lights.AsReadOnly(), _gridWidth, _gridHeight);
                    foreach (var cell in visible)
                        allVisible.Add(cell);
                }

                // Draw dim overlay on non-visible cells
                var dimBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 30));
                dimBrush.Freeze();

                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int y = 0; y < _gridHeight; y++)
                    {
                        if (!allVisible.Contains((x, y)))
                        {
                            var rect = new Rect(x * GridCellSize, y * GridCellSize, GridCellSize, GridCellSize);
                            dc.DrawRectangle(dimBrush, null, rect);
                        }
                    }
                }

                // Vision mode rendering: darkvision grayscale hint
                if (Options.EnableVisionModeRendering)
                {
                    foreach (var token in playerTokens)
                    {
                        var vision = token.Vision;
                        if (vision == null) continue;

                        if (vision.DarkvisionRange > 0)
                        {
                            RenderDarkvisionHint(dc, token, allVisible);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Renders a subtle grayscale tint over cells seen only through darkvision.
        /// </summary>
        // VISUAL REFRESH - VISION
        private void RenderDarkvisionHint(DrawingContext dc, Token token, HashSet<(int x, int y)> allVisible)
        {
            var vision = token.Vision;
            if (vision == null || vision.DarkvisionRange <= 0) return;

            var dvTint = new SolidColorBrush(Color.FromArgb(40, 100, 100, 140));
            dvTint.Freeze();

            for (int dx = -vision.DarkvisionRange; dx <= vision.DarkvisionRange; dx++)
            {
                for (int dy = -vision.DarkvisionRange; dy <= vision.DarkvisionRange; dy++)
                {
                    int tx = token.GridX + dx;
                    int ty = token.GridY + dy;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist > vision.DarkvisionRange) continue;
                    if (!allVisible.Contains((tx, ty))) continue;

                    // Only tint cells in darkness (not lit)
                    int lightLevel = VisionService.GetLightLevel(tx, ty, _lights.AsReadOnly());
                    if (lightLevel == 0)
                    {
                        var rect = new Rect(tx * GridCellSize, ty * GridCellSize, GridCellSize, GridCellSize);
                        dc.DrawRectangle(dvTint, null, rect);
                    }
                }
            }
        }

        /// <summary>
        /// Updates fog of war automatically based on player token vision (Feature 4.5).
        /// </summary>
        // VISUAL REFRESH - VISION
        public void UpdateFogFromTokenVision()
        {
            if (!Options.EnableAutoFogReveal || _fogService == null || !_fogService.IsEnabled)
                return;
            if (_visionService == null) InitializeVisionService();
            if (Tokens == null) return;

            var playerTokens = Tokens.Where(t => t.IsPlayer).ToList();
            if (playerTokens.Count == 0) return;

            var allVisible = new HashSet<(int x, int y)>();

            foreach (var token in playerTokens)
            {
                var visible = _visionService.CalculateVisibleCells(
                    token, _lights.AsReadOnly(), _fogService.GridWidth, _fogService.GridHeight);
                foreach (var cell in visible)
                    allVisible.Add(cell);
            }

            bool changed = false;

            if (Options.FogRevealMode == 1)
            {
                // Dynamic mode: hide cells that are no longer visible
                for (int x = 0; x < _fogService.GridWidth; x++)
                {
                    for (int y = 0; y < _fogService.GridHeight; y++)
                    {
                        if (_fogService.IsCellRevealed(x, y) && !allVisible.Contains((x, y)))
                        {
                            _fogService.HideCell(x, y);
                            changed = true;
                        }
                    }
                }
            }

            foreach (var (x, y) in allVisible)
            {
                if (_fogService.IsValidCell(x, y) && !_fogService.IsCellRevealed(x, y))
                {
                    _fogService.RevealCell(x, y);
                    changed = true;
                }
            }

            if (changed)
            {
                RedrawFog();
            }
        }
    }
}
