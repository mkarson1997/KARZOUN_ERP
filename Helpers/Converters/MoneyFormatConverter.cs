using System.Globalization;
using System.Windows.Data;

namespace KarzounERP.Helpers.Converters;

public class MoneyFormatConverter : IMultiValueConverter, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (!TryGetAmount(value, out var amount))
            return string.Empty;
        return MoneyFormatter.FormatMoney(amount, parameter as string);
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0 || !TryGetAmount(values[0], out var amount))
            return string.Empty;

        string? currency = null;
        if (values.Length > 1 && values[1] != null && values[1] != System.Windows.DependencyProperty.UnsetValue)
        {
            currency = values[1].ToString();
        }

        if (parameter is string paramCurrency && !string.IsNullOrWhiteSpace(paramCurrency))
            currency = paramCurrency;

        return MoneyFormatter.FormatMoney(amount, currency);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static bool TryGetAmount(object? value, out decimal amount)
    {
        amount = 0;
        if (value == null) return false;
        switch (value)
        {
            case decimal d:
                amount = d;
                return true;
            case double dbl:
                amount = (decimal)dbl;
                return true;
            case float f:
                amount = (decimal)f;
                return true;
            case int i:
                amount = i;
                return true;
            case long l:
                amount = l;
                return true;
            default:
                return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
        }
    }
}