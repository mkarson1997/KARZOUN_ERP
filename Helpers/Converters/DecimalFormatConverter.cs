using System.Globalization;
using System.Windows.Data;

namespace FornixxCRM.Helpers.Converters;

public class DecimalFormatConverter : IValueConverter
{
    public string Format { get; set; } = "N2";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string fmt = parameter as string ?? Format;
        if (value is decimal d) return d.ToString(fmt);
        if (value is double dbl) return dbl.ToString(fmt);
        if (value is float f) return f.ToString(fmt);
        return value?.ToString() ?? "0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (decimal.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0m;
    }
}

public class NullableDateConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt && dt != default)
            return dt.ToString("yyyy-MM-dd");
        return null;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt) return (DateTime?)dt;
        if (DateTime.TryParse(value?.ToString(), out var parsed)) return (DateTime?)parsed;
        return null;
    }
}
