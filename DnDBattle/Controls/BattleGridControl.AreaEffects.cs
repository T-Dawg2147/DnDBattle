using DnDBattle.Models;
using DnDBattle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DnDBattle.Controls
{
    /// <summary>
    /// Partial class containing Area Effect functionality
    /// </summary>
    public partial class BattleGridControl
    {
        #region Area Effect Placement

        /// <summary>
        /// Starts placing an area effect of the given shape
        /// </summary>
        public void StartAreaEffectPlacement(AreaEffectShape shape, int sizeInFeet, Color color)
        {
            _isPlacingAreaEffect = true;
            _currentAoeShape = shape;
            _currentAoeSize = sizeInFeet;
            _currentAoeColor = color;

            _previewEffect = new AreaEffect
            {
                Shape = shape,
                SizeInFeet = sizeInFeet,
                Color = color,
                IsPreview = true
            };

            Cursor = Cursors.Cross;
        }

        /// <summary>
        /// Starts placing a preset area effect
        /// </summary>
        public void StartAreaEffectPlacement(AreaEffect preset)
        {
            _isPlacingAreaEffect = true;
            _currentAoeShape = preset.Shape;
            _currentAoeSize = preset.SizeInFeet;
            _currentAoeColor = preset.Color;

            _previewEffect = new AreaEffect
            {
                Name = preset.Name,
                Shape = preset.Shape,
                SizeInFeet = preset.SizeInFeet,
                WidthInFeet = preset.WidthInFeet,
                Color = preset.Color,
                IsPreview = true,
                DurationRounds = preset.DurationRounds,
                RoundsRemaining = preset.DurationRounds,
                RequiresConcentration = preset.RequiresConcentration,
                DamageExpression = preset.DamageExpression,
                DamageType = preset.DamageType,
                DamageTiming = preset.DamageTiming
            };

            Cursor = Cursors.Cross;
        }

        /// <summary>
        /// Cancels the current area effect placement
        /// </summary>
        public void CancelAreaEffectPlacement()
        {
            _isPlacingAreaEffect = false;
            _currentAoeShape = null;
            _previewEffect = null;
            Cursor = Cursors.Arrow;
            RedrawAreaEffects();
        }

        /// <summary>
        /// Updates the area effect size during placement
        /// </summary>
        public void UpdateAreaEffectSize(int sizeInFeet)
        {
            _currentAoeSize = sizeInFeet;
            if (_previewEffect != null)
            {
                _previewEffect.SizeInFeet = sizeInFeet;
                RedrawAreaEffects();
            }
        }

        /// <summary>
        /// Updates the area effect color during placement
        /// </summary>
        public void UpdateAreaEffectColor(Color color)
        {
            _currentAoeColor = color;
            if (_previewEffect != null)
            {
                _previewEffect.Color = color;
                RedrawAreaEffects();
            }
        }

        #endregion

        #region Area Effect Rendering

        /// <summary>
        /// Redraws all area effects
        /// </summary>
        private void RedrawAreaEffects()
        {
            using var dc = _areaEffectVisual.RenderOpen();

            // Draw active effects
            foreach (var effect in _areaEffectService.ActiveEffects)
            {
                DrawAreaEffect(dc, effect);
            }

            // Draw preview effect
            if (_previewEffect != null)
            {
                DrawAreaEffect(dc, _previewEffect);
            }
        }

        private void DrawAreaEffect(DrawingContext dc, AreaEffect effect)
        {
            var geometry = AreaEffectService.GetEffectGeometry(effect, GridCellSize);

            // Animation: pulsing border
            double pulseValue = 0;
            double borderThickness = effect.IsPreview ? 2 : 3;
            byte borderAlpha = 200;

            if (!effect.IsPreview && Options.EnableEffectAnimations && effect.AnimationType == EffectAnimationType.Pulse)
            {
                pulseValue = (Math.Sin(effect.AnimationPhase) + 1.0) / 2.0;
                borderThickness = 2 + pulseValue * 4;
                borderAlpha = (byte)(100 + pulseValue * 155);
            }

            // Create fill brush with transparency
            var fillBrush = new SolidColorBrush(effect.Color);
            fillBrush.Freeze();

            // Create outline pen
            var outlineColor = Color.FromArgb(borderAlpha, effect.Color.R, effect.Color.G, effect.Color.B);
            var pen = new Pen(new SolidColorBrush(outlineColor), borderThickness);
            if (effect.IsPreview)
            {
                pen.DashStyle = DashStyles.Dash;
            }
            pen.Freeze();

            // Animation: rotation
            if (!effect.IsPreview && Options.EnableEffectAnimations && effect.AnimationType == EffectAnimationType.Rotate)
            {
                double cx = effect.Origin.X * GridCellSize;
                double cy = effect.Origin.Y * GridCellSize;
                dc.PushTransform(new RotateTransform(effect.AnimationPhase, cx, cy));
            }

            dc.DrawGeometry(fillBrush, pen, geometry);

            if (!effect.IsPreview && Options.EnableEffectAnimations && effect.AnimationType == EffectAnimationType.Rotate)
            {
                dc.Pop();
            }

            // Draw origin point for cones and lines
            if (effect.Shape == AreaEffectShape.Cone || effect.Shape == AreaEffectShape.Line)
            {
                double originX = effect.Origin.X * GridCellSize;
                double originY = effect.Origin.Y * GridCellSize;

                dc.DrawEllipse(
                    Brushes.White,
                    new Pen(Brushes.Black, 1),
                    new Point(originX, originY),
                    5, 5);
            }

            // Draw label for placed effects
            if (!effect.IsPreview && !string.IsNullOrEmpty(effect.Name))
            {
                var bounds = geometry.Bounds;
                var labelParts = effect.Name;

                // Append concentration indicator
                if (effect.RequiresConcentration)
                    labelParts += " (C)";

                var text = new FormattedText(
                    labelParts,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    12,
                    Brushes.White,
                    1.0);

                // Draw text background
                var textPos = new Point(bounds.X + (bounds.Width - text.Width) / 2, bounds.Y + (bounds.Height - text.Height) / 2);
                dc.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
                    null,
                    new Rect(textPos.X - 4, textPos.Y - 2, text.Width + 8, text.Height + 4));
                dc.DrawText(text, textPos);
            }

            // Phase 6.2: Duration timer badge
            if (!effect.IsPreview && Options.EnableDurationTracking && effect.DurationRounds > 0)
            {
                DrawEffectTimerBadge(dc, geometry.Bounds, effect);
            }
        }

        /// <summary>
        /// Draws a round timer badge showing remaining rounds on an area effect
        /// </summary>
        private void DrawEffectTimerBadge(DrawingContext dc, Rect bounds, AreaEffect effect)
        {
            var badgeCenter = new Point(bounds.Right - 12, bounds.Top + 12);

            var badgeBrush = effect.RoundsRemaining <= 1
                ? Brushes.Red
                : new SolidColorBrush(Color.FromRgb(100, 181, 246));

            dc.DrawEllipse(badgeBrush, new Pen(Brushes.White, 2), badgeCenter, 12, 12);

            var roundsText = new FormattedText(
                effect.RoundsRemaining.ToString(),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                12,
                Brushes.White,
                1.0);

            dc.DrawText(roundsText, new Point(
                badgeCenter.X - roundsText.Width / 2,
                badgeCenter.Y - roundsText.Height / 2));
        }

        #endregion

        #region Area Effect Helpers

        /// <summary>
        /// Calculates the angle from origin to target point
        /// </summary>
        private double CalculateAngle(Point origin, Point target)
        {
            double dx = target.X - origin.X;
            double dy = target.Y - origin.Y;
            return Math.Atan2(dy, dx) * 180.0 / Math.PI;
        }

        private void PlaceAreaEffect()
        {
            if (_previewEffect == null) return;

            // Create a copy of the preview as a placed effect
            var placedEffect = new AreaEffect
            {
                Name = _previewEffect.Name,
                Shape = _previewEffect.Shape,
                SizeInFeet = _previewEffect.SizeInFeet,
                WidthInFeet = _previewEffect.WidthInFeet,
                Origin = _previewEffect.Origin,
                DirectionAngle = _previewEffect.DirectionAngle,
                Color = _previewEffect.Color,
                IsPreview = false,
                // Phase 6 fields
                DurationRounds = _previewEffect.DurationRounds,
                RoundsRemaining = _previewEffect.RoundsRemaining > 0 ? _previewEffect.RoundsRemaining : _previewEffect.DurationRounds,
                RequiresConcentration = _previewEffect.RequiresConcentration,
                DamageExpression = _previewEffect.DamageExpression,
                DamageType = _previewEffect.DamageType,
                DamageTiming = _previewEffect.DamageTiming,
                AnimationType = (EffectAnimationType)Options.DefaultAnimationType
            };

            _areaEffectService.AddEffect(placedEffect);

            // Log the action
            string durationInfo = placedEffect.DurationRounds > 0 ? $", {placedEffect.DurationRounds} rounds" : "";
            string damageInfo = !string.IsNullOrEmpty(placedEffect.DamageExpression) ? $", {placedEffect.DamageExpression} {placedEffect.DamageType.GetDisplayName()}" : "";
            AddToActionLog("AoE", $"Placed {placedEffect.Name ?? placedEffect.Shape.ToString()} ({placedEffect.SizeInFeet} ft{durationInfo}{damageInfo})");

            // Reset for next placement (keep the same settings)
            _previewEffect = new AreaEffect
            {
                Name = placedEffect.Name,
                Shape = placedEffect.Shape,
                SizeInFeet = placedEffect.SizeInFeet,
                WidthInFeet = placedEffect.WidthInFeet,
                Color = placedEffect.Color,
                IsPreview = true,
                DurationRounds = placedEffect.DurationRounds,
                DamageExpression = placedEffect.DamageExpression,
                DamageType = placedEffect.DamageType,
                DamageTiming = placedEffect.DamageTiming,
                RequiresConcentration = placedEffect.RequiresConcentration
            };

            RedrawAreaEffects();
        }

        #endregion
    }
}
