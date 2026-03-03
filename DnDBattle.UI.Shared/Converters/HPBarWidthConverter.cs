using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DnDBattle.Converters
{
    public class HPBarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 3 &&
                values[0] is int currentHP &&
                values[1] is int maxHP &&
                values[2] is double containerWidth &&
                maxHP > 0)
            {
                double percentage = Math.Max(0, Math.Min(1, (double)currentHP / maxHP));
                return containerWidth * percentage;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
