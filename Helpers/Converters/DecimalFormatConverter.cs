using System.Globalization;
using System.Windows.Data;

namespace KarzounERP.Helpers.Converters;

public class DecimalFormatConverter : IValueConverter
{
    public string Format { get; set; } = "N2";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string fmt = parameter as string ?? Format;
        if (value is decimal d) return d.ToString(fmt, CultureInfo.InvariantCulture);
        if (value is double dbl) return dbl.ToString(fmt, CultureInfo.InvariantCulture);
        if (value is float f) return f.ToString(fmt, CultureInfo.InvariantCulture);
        return value?.ToString() ?? "0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = DigitNormalizer.ToEnglishDigits(value?.ToString());
        if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0m;
    }
}

public class NullableDateConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt && dt != default)
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return null;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt) return (DateTime?)dt;
        var str = DigitNormalizer.ToEnglishDigits(value?.ToString());
        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return (DateTime?)parsed;
        return null;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
        if (parameter as string == "Inverse")
            return isNull ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        return isNull ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
