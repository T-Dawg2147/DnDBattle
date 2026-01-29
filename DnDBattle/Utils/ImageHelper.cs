using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DnDBattle.Utils
{
    public static class ImageHelper
    {
        /// <summary>
        /// Loads a BitmapImage from a file path, properly frozen for WPF performance.
        /// </summary>
        /// <param name="filePath">The full path to the image file</param>
        /// <returns>A frozen BitmapImage, or null if loading fails</returns>
        public static BitmapImage LoadFrozenBitmap(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a BitmapImage from a Uri, properly frozen for WPF performance.
        /// </summary>
        public static BitmapImage LoadFrozenBitmap(Uri uri)
        {
            if (uri == null)
                return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image from {uri}: {ex.Message}");
                return null;
            }
        }
    }
}
