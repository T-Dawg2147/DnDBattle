using System.Globalization;
using System.Windows.Data;

namespace DnDBattle.UI.Shared.Converters;

[ValueConversion(typeof(int), typeof(double))]
public sealed class HpBarWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3) return 0.0;
        if (values[0] is not int current || values[1] is not int max || values[2] is not double totalWidth)
            return 0.0;
        if (max <= 0) return 0.0;
        return totalWidth * ((double)current / max);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
