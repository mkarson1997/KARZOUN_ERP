using System.Globalization;
using System.Windows.Data;

namespace KarzounERP.Helpers.Converters;

public class EnumToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        return ArabicEnumHelper.GetLabel(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
