using DnDBattle.Models;
using DnDBattle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Lighting functionality
    /// </summary>
    public partial class BattleGridControl
    {
        #region Light Management

        public void AddLight(LightSource light)
        {
            _lights.Add(light);
            _spatialIndex.IndexLight(light);
            RedrawLighting();
        }

        #endregion

        #region Light Rendering

        private void RedrawLighting()
        {
            using (var dc = _lightingVisual.RenderOpen())
            {
                if (_lights == null || _lights.Count == 0)
                {
                    return;
                }

                foreach (var light in _lights)
                {
                    var centerGrid = light.CenterGrid;
                    var centerPixel = new Point(
                        centerGrid.X * GridCellSize + GridCellSize / 2.0,
                        centerGrid.Y * GridCellSize + GridCellSize / 2.0);
                    double radiusPx = Math.Max(GridCellSize, light.RadiusSquares * GridCellSize);

                    // Compute lit polygon using wall service
                    var litPolygonGrid = _wallService.ComputeLitPolygon(centerGrid, light.RadiusSquares, 180);

                    if (litPolygonGrid.Count < 3)
                    {
                        // No walls blocking - draw full circle
                        var rg = CreateLightGradient(light.Intensity);
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
                        var rg = CreateLightGradient(light.Intensity);

                        dc.PushClip(litGeometry);
                        dc.PushTransform(new TranslateTransform(centerPixel.X - radiusPx, centerPixel.Y - radiusPx));
                        dc.DrawRectangle(rg, null, new Rect(0, 0, radiusPx * 2, radiusPx * 2));
                        dc.Pop();
                        dc.Pop();
                    }

                    // Draw light source indicator
                    var centerBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 150));
                    centerBrush.Freeze();
                    dc.DrawEllipse(centerBrush, new Pen(Brushes.Orange, 2), centerPixel, 8, 8);
                }
            }
        }

        private RadialGradientBrush CreateLightGradient(double intensity)
        {
            var rg = new RadialGradientBrush();

            byte centerAlpha = (byte)(200 * intensity);
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(centerAlpha, 255, 255, 200), 0.0));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(centerAlpha * 0.5), 255, 220, 150), 0.5));
            rg.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 150, 50), 1.0));

            rg.RadiusX = 1;
            rg.RadiusY = 1;
            rg.Center = new Point(0.5, 0.5);
            rg.GradientOrigin = new Point(0.5, 0.5);
            rg.Freeze();

            return rg;

        }

        #endregion

        #region Undo / Redo API for obstacles

        public void RemoveLightPublic(LightSource light)
        {
            if (light == null) return;
            _lights.Remove(light);
            RedrawLighting();
        }

        #endregion
    }
}
