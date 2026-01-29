using System.Windows;
using System.Windows.Media;

namespace DnDBattle.Utils
{
    /// <summary>
    /// Provides cached Typeface instances to avoid repeated allocations.
    /// Typeface objects are immutable and safe to share across the application.
    /// </summary>
    public static class CachedTypefaces
    {
        // Primary UI font - Segoe UI
        public static readonly Typeface SegoeUI;
        public static readonly Typeface SegoeUIBold;
        public static readonly Typeface SegoeUISemiBold;
        public static readonly Typeface SegoeUIItalic;

        // Emoji font for icons
        public static readonly Typeface SegoeUIEmoji;

        // Monospace for code/numbers
        public static readonly Typeface Consolas;

        static CachedTypefaces()
        {
            var segoeFamily = new FontFamily("Segoe UI");
            var emojiFamily = new FontFamily("Segoe UI Emoji");
            var consolasFamily = new FontFamily("Consolas");

            SegoeUI = new Typeface(segoeFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            SegoeUIBold = new Typeface(segoeFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            SegoeUISemiBold = new Typeface(segoeFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
            SegoeUIItalic = new Typeface(segoeFamily, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);

            SegoeUIEmoji = new Typeface(emojiFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            Consolas = new Typeface(consolasFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        }
    }
}