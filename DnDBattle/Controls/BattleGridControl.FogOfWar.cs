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
using DnDBattle.ViewModels;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileService;
using DnDBattle.Views.Editors;
using DnDBattle.Views.TileMap;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Fog of War functionality
    /// </summary>
    public partial class BattleGridControl
    {
        #region Fog of War Initialization

        public void InitializeFogOfWar()
        {
            _fogService = new FogOfWarService();
            _fogService.FogChanged += OnFogChanged;
            _fogService.LogMessage += (msg) => System.Diagnostics.Debug.WriteLine(msg);

            int gridWidth = (int)(ActualWidth / GridCellSize) + 10;
            int gridHeight = (int)(ActualHeight / GridCellSize) + 10;
            _fogService.Initialize(Math.Max(50, gridWidth), Math.Max(50, gridHeight), GridCellSize);
        }

        #endregion

        #region Fog of War Toggle and Control

        public void ToggleFogOfWar(bool enabled)
        {
            AddToActionLog("Fog", enabled ? "🌫️ Fog of War enabled" : "☀️ Fog of War disabled");
        }

        private void OnFogChanged()
        {
            RedrawFog();
        }

        /// <summary>
        /// Redraws the fog overlay
        /// </summary>
        public void RedrawFog()
        {
            if (_fogService == null) return;

            if (!_fogService.IsEnabled)
            {
                // Remove fog layer if it exists
                RemoveFogLayer();
                return;
            }

            // Create new fog visual
            RenderFogLayer();
        }

        #endregion

        #region Fog Layer Rendering

        private void RenderFogLayer()
        {
            // Find or create the fog canvas
            if (_fogCanvas == null)
            {
                _fogCanvas = RenderCanvas.Children.OfType<Canvas>()
                    .FirstOrDefault(c => c.Tag as string == "FogLayer");
            }

            if (_fogCanvas == null)
            {
                _fogCanvas = new Canvas
                {
                    Tag = "FogLayer",
                    IsHitTestVisible = false
                };
                RenderCanvas.Children.Add(_fogCanvas);
            }

            // Clear existing fog rectangles
            _fogCanvas.Children.Clear();

            // Set Z-index high so fog is on top
            Canvas.SetZIndex(_fogCanvas, 1000);

            bool isPlayerView = _fogService.ShowPlayerView;
            double opacity = isPlayerView ? _fogService.PlayerFogOpacity : _fogService.DmFogOpacity;
            var fogBrush = new SolidColorBrush(_fogService.FogColor);

            // Draw fog for each hidden cell
            for (int x = 0; x < _fogService.GridWidth; x++)
            {
                for (int y = 0; y < _fogService.GridHeight; y++)
                {
                    if (!_fogService.IsCellRevealed(x, y))
                    {
                        var rect = new System.Windows.Shapes.Rectangle
                        {
                            Width = GridCellSize,
                            Height = GridCellSize,
                            Fill = fogBrush,
                            Opacity = opacity,
                            IsHitTestVisible = false
                        };

                        Canvas.SetLeft(rect, x * GridCellSize);
                        Canvas.SetTop(rect, y * GridCellSize);
                        _fogCanvas.Children.Add(rect);
                    }
                }
            }
        }

        private void RenderFogOfWar()
        {
            using (var dc = _fogVisual.RenderOpen())
            {
                if (!_fogOfWar.IsEnabled || _loadedTileMap == null)
                {
                    // Clear fog
                    return;
                }

                double cellSize = GridCellSize;

                for (int x = 0; x < _loadedTileMap.Width; x++)
                {
                    for (int y = 0; y < _loadedTileMap.Height; y++)
                    {
                        bool isRevealed = _fogOfWar.IsTileRevealed(x, y);
                        bool isVisible = _fogOfWar.IsTileVisible(x, y);

                        Brush fogBrush = null;

                        if (!isRevealed)
                        {
                            // Completely hidden - black fog
                            fogBrush = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0));
                        }
                        else if (_fogOfWar.Mode == FogMode.Dynamic && !isVisible)
                        {
                            // Revealed but not currently visible - gray fog
                            fogBrush = new SolidColorBrush(Color.FromArgb(120, 30, 30, 30));
                        }

                        if (fogBrush != null)
                        {
                            fogBrush.Freeze();
                            var rect = new Rect(x * cellSize, y * cellSize, cellSize, cellSize);
                            dc.DrawRectangle(fogBrush, null, rect);
                        }
                    }
                }
            }
        }

        private void UpdateFogVisibility()
        {
            if (!_fogOfWar.IsEnabled || _loadedTileMap == null) return;

            _fogOfWar.ClearVisibility();

            if (Application.Current?.MainWindow?.DataContext is MainViewModel vm)
            {
                // Reveal areas around player tokens
                foreach (var token in vm.Tokens.Where(t => t.IsPlayer))
                {
                    _fogOfWar.RevealArea(token.GridX, token.GridY, _fogOfWar.VisionRange);
                }
            }

            RenderFogOfWar();
        }

        #endregion

        #region Fog Control Methods

        // Add method to toggle fog
        public void SetFogOfWar(bool enabled, FogMode mode = FogMode.Exploration)
        {
            _fogOfWar.IsEnabled = enabled;
            _fogOfWar.Mode = mode;

            if (enabled)
            {
                UpdateFogVisibility();
            }
            else
            {
                RenderFogOfWar(); // Clear
            }

            AddToActionLog("Fog", enabled ? "🌫️ Fog of War enabled" : "☀️ Fog of War disabled");
        }

        public void RevealAllFog()
        {
            if (_loadedTileMap == null) return;

            for (int x = 0; x < _loadedTileMap.Width; x++)
            {
                for (int y = 0; y < _loadedTileMap.Height; y++)
                {
                    _fogOfWar.RevealTile(x, y);
                    _fogOfWar.AddVisibleTile(x, y);
                }
            }

            RenderFogOfWar();
            AddToActionLog("Fog", "☀️ All fog revealed");
        }

        // Add method to reset fog
        public void ResetFog()
        {
            _fogOfWar.Reset();
            RenderFogOfWar();
            AddToActionLog("Fog", "🌫️ Fog reset");
        }

        private void RemoveFogLayer()
        {
            if (_fogCanvas != null)
            {
                _fogCanvas.Children.Clear();

                if (RenderCanvas.Children.Contains(_fogCanvas))
                {
                    RenderCanvas.Children.Remove(_fogCanvas);
                }

                _fogCanvas = null;
            }

            // Also remove any stray fog canvases
            var fogCanvases = RenderCanvas.Children.OfType<Canvas>()
                .Where(c => c.Tag as string == "FogLayer")
                .ToList();

            foreach (var canvas in fogCanvases)
            {
                RenderCanvas.Children.Remove(canvas);
            }
        }

        /// <summary>
        /// Sets fog enabled state
        /// </summary>
        public void SetFogEnabled(bool enabled)
        {
            _fogService.IsEnabled = enabled;
            RedrawFog();
        }

        /// <summary>
        /// Sets player view mode
        /// </summary>
        public void SetPlayerView(bool isPlayerView)
        {
            _fogService.ShowPlayerView = isPlayerView;
            RedrawFog();

            // Also update token visibility in player view
            if (isPlayerView)
            {
                UpdateTokenVisibilityForPlayerView();
            }
            else
            {
                ShowAllTokens();
            }
        }

        private void UpdateTokenVisibilityForPlayerView()
        {
            // Hide tokens that are in fog
            foreach (var child in RenderCanvas.Children.OfType<FrameworkElement>())
            {
                if (child.Tag is Models.Creatures.Token token && !token.IsPlayer)
                {
                    bool visible = _fogService.IsTokenVisible(token);
                    child.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        private void ShowAllTokens()
        {
            foreach (var child in RenderCanvas.Children.OfType<FrameworkElement>())
            {
                if (child.Tag is Models.Creatures.Token)
                {
                    child.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Sets the brush mode for fog editing
        /// </summary>
        public void SetFogBrushMode(FogBrushMode mode)
        {
            _fogService.BrushMode = mode;
        }

        /// <summary>
        /// Sets the brush size for fog editing
        /// </summary>
        public void SetFogBrushSize(int size)
        {
            _fogService.BrushSize = size;
        }

        /// <summary>
        /// Starts a shape-based fog reveal/hide
        /// </summary>
        public void StartFogShapeTool(FogShapeTool tool)
        {
            _currentFogShapeTool = tool;
            _fogShapeStartPoint = null;

            if (tool != FogShapeTool.None)
            {
                RenderCanvas.Cursor = Cursors.Cross;
            }
        }

        /// <summary>
        /// Reveals area around all player tokens
        /// </summary>
        public void RevealAroundPlayers()
        {
            if (Tokens != null)
            {
                _fogService.RevealAroundTokens(Tokens, playersOnly: true);
            }
        }

        #endregion

        #region Fog Mouse Handling

        private void HandleFogMouseDown(Point position)
        {
            if (!_fogService.IsEnabled) return;

            int gridX = (int)(position.X / GridCellSize);
            int gridY = (int)(position.Y / GridCellSize);

            if (_currentFogShapeTool != FogShapeTool.None)
            {
                // Start shape drawing
                _fogShapeStartPoint = new Point(gridX, gridY);
            }
            else
            {
                // Brush mode - start painting
                _isFogBrushActive = true;
                _fogService.ApplyBrush(gridX, gridY);
            }
        }

        // Add to RenderCanvas_MouseMove
        private void HandleFogMouseMove(Point position)
        {
            if (!_fogService.IsEnabled) return;

            int gridX = (int)(position.X / GridCellSize);
            int gridY = (int)(position.Y / GridCellSize);

            if (_isFogBrushActive)
            {
                _fogService.ApplyBrush(gridX, gridY);
            }
        }

        // Add to RenderCanvas_MouseLeftButtonUp
        private void HandleFogMouseUp(Point position)
        {
            if (!_fogService.IsEnabled) return;

            int gridX = (int)(position.X / GridCellSize);
            int gridY = (int)(position.Y / GridCellSize);

            if (_currentFogShapeTool != FogShapeTool.None && _fogShapeStartPoint.HasValue)
            {
                int startX = (int)_fogShapeStartPoint.Value.X;
                int startY = (int)_fogShapeStartPoint.Value.Y;

                if (_currentFogShapeTool == FogShapeTool.Rectangle)
                {
                    if (_fogService.BrushMode == FogBrushMode.Reveal)
                        _fogService.RevealRectangle(startX, startY, gridX, gridY);
                    else
                        _fogService.HideRectangle(startX, startY, gridX, gridY);
                }
                else if (_currentFogShapeTool == FogShapeTool.Circle)
                {
                    int radius = (int)Math.Max(Math.Abs(gridX - startX), Math.Abs(gridY - startY));
                    if (_fogService.BrushMode == FogBrushMode.Reveal)
                        _fogService.RevealCircle(startX, startY, radius);
                    else
                        _fogService.HideCircle(startX, startY, radius);
                }

                _fogShapeStartPoint = null;
                _currentFogShapeTool = FogShapeTool.None;
                RenderCanvas.Cursor = Cursors.Arrow;
            }

            _isFogBrushActive = false;
        }

        #endregion
    }
}
