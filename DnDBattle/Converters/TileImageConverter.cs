using DnDBattle.Services.TileService;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DnDBattle.Converters
{
    /// <summary>
    /// Converter to load tile images via cache
    /// </summary>
    public class TileImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath)
            {
                return TileImageCacheService.Instance.GetOrLoadImage(imagePath);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
