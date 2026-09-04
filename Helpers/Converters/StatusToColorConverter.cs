using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using KarzounERP.Models;

namespace KarzounERP.Helpers.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Draft => new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                DocumentStatus.Sent => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                DocumentStatus.Accepted => new SolidColorBrush(Color.FromRgb(0, 188, 212)),
                DocumentStatus.Paid => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                DocumentStatus.PartiallyPaid => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                DocumentStatus.Rejected => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                DocumentStatus.Cancelled => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                DocumentStatus.Converted => new SolidColorBrush(Color.FromRgb(103, 58, 183)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        if (value is ImportanceLevel importance)
        {
            return importance switch
            {
                ImportanceLevel.Normal => new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                ImportanceLevel.Important => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                ImportanceLevel.VeryImportant => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        if (value is bool active)
        {
            return active
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                : new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
