using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DnDBattle.Utils
{
    /// <summary>
    /// Centralized cache of commonly-used UI brushes.
    /// All brushes are frozen and safe to share across the application.
    /// </summary>
    public static class UIThemeBrushes
    {
        #region Backgrounds
        public static readonly SolidColorBrush TooltipBackground;
        public static readonly SolidColorBrush DarkBackground;
        public static readonly SolidColorBrush PanelBackground;
        public static readonly SolidColorBrush PanelBackgroundHover;
        public static readonly SolidColorBrush BarBackground;
        public static readonly SolidColorBrush ConditionPanelBackground;
        #endregion

        #region Text Colors
        public static readonly SolidColorBrush PrimaryText;       // White
        public static readonly SolidColorBrush SecondaryText;     // Light gray (150, 150, 150)
        public static readonly SolidColorBrush TertiaryText;      // Darker gray (130, 130, 130)
        public static readonly SolidColorBrush MutedText;         // (100, 100, 100)
        public static readonly SolidColorBrush DisabledText;      // (136, 136, 136)
        #endregion

        #region Borders
        public static readonly SolidColorBrush TooltipBorder;
        public static readonly SolidColorBrush PanelBorder;
        #endregion

        #region Status Colors (HP)
        public static readonly SolidColorBrush HPHealthy;         // Green (76, 175, 80)
        public static readonly SolidColorBrush HPWarning;         // Yellow (255, 193, 7)
        public static readonly SolidColorBrush HPCritical;        // Red (244, 67, 54)
        #endregion

        #region Stat Colors
        public static readonly SolidColorBrush ACColor;           // Blue (100, 181, 246)
        public static readonly SolidColorBrush CRColor;           // Orange (255, 152, 0)
        public static readonly SolidColorBrush InitiativeColor;   // Purple (186, 104, 200)
        public static readonly SolidColorBrush AttackBonusColor;  // Green (76, 175, 80)
        public static readonly SolidColorBrush DamageColor;       // Red (244, 67, 54)
        public static readonly SolidColorBrush RangeColor;        // Light Blue (100, 181, 246)
        #endregion

        #region Tag Colors
        public static readonly SolidColorBrush TagBackground;
        #endregion

        #region HP Bar 
        public static readonly SolidColorBrush HPBarBackground;
        #endregion

        #region Condition Badge Colors
        // Cache for condition-specific brushes
        private static readonly Dictionary<Models.Condition, (SolidColorBrush background, SolidColorBrush border)> ConditionBrushCache;

        // Overflow indicator
        public static readonly SolidColorBrush ConditionOverflowBackground;
        #endregion

        static UIThemeBrushes()
        {
            // Backgrounds
            TooltipBackground = Freeze(new SolidColorBrush(Color.FromRgb(30, 30, 30)));
            DarkBackground = Freeze(new SolidColorBrush(Color.FromRgb(37, 37, 38)));
            PanelBackground = Freeze(new SolidColorBrush(Color.FromRgb(45, 45, 48)));
            PanelBackgroundHover = Freeze(new SolidColorBrush(Color.FromRgb(60, 60, 64)));
            BarBackground = Freeze(new SolidColorBrush(Color.FromRgb(50, 50, 50)));
            ConditionPanelBackground = Freeze(new SolidColorBrush(Color.FromRgb(50, 40, 30)));

            // Text Colors
            PrimaryText = Freeze(new SolidColorBrush(Colors.White));
            SecondaryText = Freeze(new SolidColorBrush(Color.FromRgb(150, 150, 150)));
            TertiaryText = Freeze(new SolidColorBrush(Color.FromRgb(130, 130, 130)));
            MutedText = Freeze(new SolidColorBrush(Color.FromRgb(100, 100, 100)));
            DisabledText = Freeze(new SolidColorBrush(Color.FromRgb(136, 136, 136)));

            // Borders
            TooltipBorder = Freeze(new SolidColorBrush(Color.FromRgb(60, 60, 60)));
            PanelBorder = Freeze(new SolidColorBrush(Color.FromRgb(80, 80, 80)));

            // Status Colors
            HPHealthy = Freeze(new SolidColorBrush(Color.FromRgb(76, 175, 80)));
            HPWarning = Freeze(new SolidColorBrush(Color.FromRgb(255, 193, 7)));
            HPCritical = Freeze(new SolidColorBrush(Color.FromRgb(244, 67, 54)));

            // Stat Colors
            ACColor = Freeze(new SolidColorBrush(Color.FromRgb(100, 181, 246)));
            CRColor = Freeze(new SolidColorBrush(Color.FromRgb(255, 152, 0)));
            InitiativeColor = Freeze(new SolidColorBrush(Color.FromRgb(186, 104, 200)));
            AttackBonusColor = HPHealthy;  // Same green as HP healthy
            DamageColor = HPCritical;       // Same red as HP critical
            RangeColor = ACColor;           // Same blue as AC

            // Tags
            TagBackground = Freeze(new SolidColorBrush(Color.FromRgb(70, 70, 75)));

            // HP Bar background
            HPBarBackground = Freeze(new SolidColorBrush(Color.FromRgb(40, 40, 40)));

            // Condition overflow
            ConditionOverflowBackground = Freeze(new SolidColorBrush(Color.FromArgb(180, 50, 50, 50)));

            // Pre-cache all condition colors
            ConditionBrushCache = new Dictionary<Models.Condition, (SolidColorBrush, SolidColorBrush)>();

            foreach (Models.Condition condition in Enum.GetValues(typeof(Models.Condition)))
            {
                if (condition == Models.Condition.None) continue;

                var color = ConditionExtensions.GetConditionColor(condition);
                var bgBrush = Freeze(new SolidColorBrush(color));
                var borderBrush = Freeze(new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B)));

                ConditionBrushCache[condition] = (bgBrush, borderBrush);
            }
        }

        private static SolidColorBrush Freeze(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Gets the appropriate HP brush based on health percentage.
        /// </summary>
        public static SolidColorBrush GetHPBrush(double hpPercent)
        {
            if (hpPercent > 0.5) return HPHealthy;
            if (hpPercent > 0.25) return HPWarning;
            return HPCritical;
        }

        /// <summary>
        /// Gets cached brushes for a condition badge.
        /// </summary>
        public static (SolidColorBrush background, SolidColorBrush border) GetConditionBrushes(Models.Condition condition)
        {
            if (ConditionBrushCache.TryGetValue(condition, out var brushes))
            {
                return brushes;
            }

            // Fallback for unknown conditions (shouldn't happen)
            var color = ConditionExtensions.GetConditionColor(condition);
            return (new SolidColorBrush(color), new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B)));
        }
    }
}
