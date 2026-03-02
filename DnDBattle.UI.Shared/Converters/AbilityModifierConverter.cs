using System.Globalization;
using System.Windows.Data;

namespace DnDBattle.UI.Shared.Converters;

[ValueConversion(typeof(int), typeof(string))]
public sealed class AbilityModifierConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int score) return "+0";
        int mod = (score - 10) / 2;
        return mod >= 0 ? $"+{mod}" : mod.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
