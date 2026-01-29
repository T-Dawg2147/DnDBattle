using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DnDBattle.Utils
{
    /// <summary>
    /// Provides cached drawing resources (brushes, pens) for DrawingContext operations.
    /// All resources are frozen for thread-safety and performance.
    /// </summary>
    public static class CachedDrawingResources
    {
        // Common label brushes
        public static readonly SolidColorBrush LabelBrush;
        public static readonly SolidColorBrush LabelBackgroundBrush;
        public static readonly SolidColorBrush DarkBackgroundBrush;
        public static readonly SolidColorBrush SemiTransparentBlackBrush;

        // Common text colors
        public static readonly SolidColorBrush WhiteBrush;
        public static readonly SolidColorBrush BlackBrush;
        public static readonly SolidColorBrush GrayTextBrush;

        static CachedDrawingResources()
        {
            // Label brushes (used in coordinate labels, tooltips, etc.)
            LabelBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
            LabelBrush.Freeze();

            LabelBackgroundBrush = new SolidColorBrush(Color.FromArgb(150, 30, 30, 30));
            LabelBackgroundBrush.Freeze();

            DarkBackgroundBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
            DarkBackgroundBrush.Freeze();

            SemiTransparentBlackBrush = new SolidColorBrush(Color.FromArgb(200, 50, 50, 50));
            SemiTransparentBlackBrush.Freeze();

            // Standard text colors
            WhiteBrush = new SolidColorBrush(Colors.White);
            WhiteBrush.Freeze();

            BlackBrush = new SolidColorBrush(Colors.Black);
            BlackBrush.Freeze();

            GrayTextBrush = new SolidColorBrush(Color.FromRgb(130, 130, 130));
            GrayTextBrush.Freeze();
        }
    }
}
