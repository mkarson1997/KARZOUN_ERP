using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KarzounERP.Helpers.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool bval = value switch
        {
            bool b => b,
            int i => i > 0,
            long l => l > 0,
            decimal d => d != 0,
            string s => !string.IsNullOrWhiteSpace(s),
            _ => false
        };
        if (Invert) bval = !bval;
        return bval ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Collapsed;
}
