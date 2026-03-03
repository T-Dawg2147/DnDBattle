using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle
{
    public static class AppTheme
    {
        // Font Sizes
        public const double FontSizeTiny = 9;
        public const double FontSizeSmall = 10;
        public const double FontSizeNormal = 12;
        public const double FontSizeMedium = 14;
        public const double FontSizeLarge = 16;
        public const double FontSizeXLarge = 18;
        public const double FontSizeTitle = 22;
        public const double FontSizeHeader = 28;

        // Stat Display Sizes
        public const double StatValueSize = 16;
        public const double StatLabelSize = 9;
        public const double StatModifierSize = 10;

        // Combat Stats
        public const double CombatStatValueSize = 18;
        public const double HPCurrentSize = 24;

        // Colors (as hex strings for XAML)
        public const string ColorBackground = "#1E1E1E";
        public const string ColorBackgroundLight = "#252526";
        public const string ColorBackgroundLighter = "#2D2D30";
        public const string ColorBackgroundCard = "#3E3E42";

        public const string ColorTextPrimary = "#FFFFFF";
        public const string ColorTextSecondary = "#CCCCCC";
        public const string ColorTextMuted = "#888888";
        public const string ColorTextDisabled = "#666666";

        public const string ColorAccentBlue = "#4FC3F7";
        public const string ColorAccentGreen = "#4CAF50";
        public const string ColorAccentYellow = "#FFB74D";
        public const string ColorAccentRed = "#F44336";
        public const string ColorAccentPurple = "#BA68C8";
        public const string ColorAccentOrange = "#FF9800";

        // HP Colors
        public const string ColorHPHealthy = "#4CAF50";
        public const string ColorHPHurt = "#FFC107";
        public const string ColorHPCritical = "#F44336";

        // Action Type Colors
        public const string ColorActionStandard = "#F44336";
        public const string ColorActionBonus = "#FF9800";
        public const string ColorActionReaction = "#9C27B0";
        public const string ColorActionLegendary = "#FFD700";
    }
}
