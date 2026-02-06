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
    /// Partial class containing Lighting functionality
    /// </summary>
    public partial class BattleGridControl
    {
        // Cache for shadow polygons keyed by light position hash
        private readonly Dictionary<int, List<Point>> _shadowCache = new Dictionary<int, List<Point>>();

        #region Light Management

        public void AddLight(LightSource light)
        {
            _lights.Add(light);
            _spatialIndex.IndexLight(light);
            InvalidateShadowCache();
            RedrawLighting();
        }

        /// <summary>
        /// Updates an existing light's properties and redraws.
        /// </summary>
        public void UpdateLight(LightSource light)
        {
            InvalidateShadowCache();
            RedrawLighting();
        }

        /// <summary>
        /// Returns a read-only view of all lights for external services.
        /// </summary>
        public IReadOnlyList<LightSource> Lights => _lights.AsReadOnly();

        #endregion

        #region Light Rendering

        private void RedrawLighting()
        {
            using (var dc = _lightingVisual.RenderOpen())
            {
                if (_lights == null || _lights.Count == 0 || !Options.EnableLighting)
                {
                    return;
                }

                foreach (var light in _lights)
                {
                    if (!light.IsEnabled) continue;

                    var centerGrid = light.CenterGrid;
                    var centerPixel = new Point(
                        centerGrid.X * GridCellSize + GridCellSize / 2.0,
                        centerGrid.Y * GridCellSize + GridCellSize / 2.0);
                    double radiusPx = Math.Max(GridCellSize, light.RadiusSquares * GridCellSize);

                    if (light.Type == LightType.Directional && Options.EnableDirectionalLights)
                    {
                        RenderDirectionalLight(dc, light, centerPixel, radiusPx);
                    }
                    else
                    {
                        RenderPointLight(dc, light, centerPixel, radiusPx);
                    }

                    // Draw light source indicator
                    var indicatorColor = Color.FromArgb(200, light.LightColor.R, light.LightColor.G, light.LightColor.B);
                    var centerBrush = new SolidColorBrush(indicatorColor);
                    centerBrush.Freeze();
                    dc.DrawEllipse(centerBrush, new Pen(Brushes.Orange, 2), centerPixel, 8, 8);
                }
            }
        }

        private void RenderPointLight(DrawingContext dc, LightSource light, Point centerPixel, double radiusPx)
        {
            List<Point> litPolygonGrid = null;

            if (Options.EnableShadowCasting)
            {
                int cacheKey = HashCode.Combine(
                    light.CenterGrid.X.GetHashCode(),
                    light.CenterGrid.Y.GetHashCode(),
                    light.RadiusSquares.GetHashCode(),
                    Options.ShadowCastRayCount);

                if (!_shadowCache.TryGetValue(cacheKey, out litPolygonGrid))
                {
                    litPolygonGrid = _wallService.ComputeLitPolygon(
                        light.CenterGrid, light.RadiusSquares, Options.ShadowCastRayCount);
                    _shadowCache[cacheKey] = litPolygonGrid;
                }
            }

            if (litPolygonGrid == null || litPolygonGrid.Count < 3)
            {
                // No walls blocking - draw full circle
                var rg = CreateLightGradient(light);
                dc.DrawEllipse(rg, null, centerPixel, radiusPx, radiusPx);
            }
            else
            {
                // Create polygon geometry from lit area
                var litGeometry = new PathGeometry();
                var figure = new PathFigure
                {
                    StartPoint = new Point(
                        litPolygonGrid[0].X * GridCellSize + GridCellSize / 2,
                        litPolygonGrid[0].Y * GridCellSize + GridCellSize / 2),
                    IsClosed = true,
                    IsFilled = true
                };

                for (int i = 1; i < litPolygonGrid.Count; i++)
                {
                    figure.Segments.Add(new LineSegment(
                        new Point(
                            litPolygonGrid[i].X * GridCellSize + GridCellSize / 2,
                            litPolygonGrid[i].Y * GridCellSize + GridCellSize / 2),
                        true));
                }

                litGeometry.Figures.Add(figure);

                // Draw with radial gradient clipped to lit area
                var rg = CreateLightGradient(light);

                dc.PushClip(litGeometry);
                dc.PushTransform(new TranslateTransform(centerPixel.X - radiusPx, centerPixel.Y - radiusPx));
                dc.DrawRectangle(rg, null, new Rect(0, 0, radiusPx * 2, radiusPx * 2));
                dc.Pop();
                dc.Pop();
            }
        }

        private void RenderDirectionalLight(DrawingContext dc, LightSource light, Point centerPixel, double radiusPx)
        {
            // Build a cone geometry
            double dirRad = light.Direction * Math.PI / 180.0;
            double halfCone = light.ConeWidth * Math.PI / 360.0;
            int segments = 24;

            var figure = new PathFigure { StartPoint = centerPixel, IsClosed = true, IsFilled = true };
            for (int i = 0; i <= segments; i++)
            {
                double angle = dirRad - halfCone + (2.0 * halfCone * i / segments);
                var pt = new Point(
                    centerPixel.X + Math.Cos(angle) * radiusPx,
                    centerPixel.Y + Math.Sin(angle) * radiusPx);
                figure.Segments.Add(new LineSegment(pt, true));
            }

            var coneGeometry = new PathGeometry();
            coneGeometry.Figures.Add(figure);

            var rg = CreateLightGradient(light);

            dc.PushClip(coneGeometry);
            dc.PushTransform(new TranslateTransform(centerPixel.X - radiusPx, centerPixel.Y - radiusPx));
            dc.DrawRectangle(rg, null, new Rect(0, 0, radiusPx * 2, radiusPx * 2));
            dc.Pop();
            dc.Pop();
        }

        private RadialGradientBrush CreateLightGradient(LightSource light)
        {
            var rg = new RadialGradientBrush();
            var c = light.LightColor;

            // Bright center
            byte centerAlpha = (byte)(200 * light.Intensity);
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(centerAlpha, c.R, c.G, c.B), 0.0));

            // Bright/dim boundary
            double brightRatio = light.BrightRadius / Math.Max(0.01, light.RadiusSquares);
            byte midAlpha = (byte)(centerAlpha * 0.5);
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(midAlpha, c.R, c.G, c.B), brightRatio));

            // Dim edge
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1.0));

            rg.RadiusX = 1;
            rg.RadiusY = 1;
            rg.Center = new Point(0.5, 0.5);
            rg.GradientOrigin = new Point(0.5, 0.5);
            rg.Freeze();

            return rg;
        }

        #endregion

        #region Shadow Cache

        /// <summary>
        /// Invalidates cached shadow polygons (call when walls or lights change).
        /// </summary>
        public void InvalidateShadowCache()
        {
            _shadowCache.Clear();
        }

        #endregion

        #region Undo / Redo API for obstacles

        public void RemoveLightPublic(LightSource light)
        {
            if (light == null) return;
            _lights.Remove(light);
            InvalidateShadowCache();
            RedrawLighting();
        }

        #endregion
    }
}
