using System.Globalization;

namespace KarzounERP.Helpers;

public static class MoneyFormatter
{
    public const string DefaultCurrency = "USD";

    public static string NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return DefaultCurrency;
        var trimmed = currency.Trim().ToUpperInvariant();
        var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && parts.Distinct().Count() == 1)
        {
            return parts[0];
        }
        return trimmed;
    }

    public static string FormatMoney(decimal amount, string? currency = null)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            try
            {
                var session = App.Services?.GetService(typeof(AppSession)) as AppSession;
                currency = session?.ActiveCompanyCurrency;
            }
            catch { }
        }
        var code = NormalizeCurrency(currency);
        return amount.ToString("N2", CultureInfo.InvariantCulture) + " " + code;
    }

    public static string FormatHeaderWithCurrency(string header, string? currency = null)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            try
            {
                var session = App.Services?.GetService(typeof(AppSession)) as AppSession;
                currency = session?.ActiveCompanyCurrency;
            }
            catch { }
        }
        var code = NormalizeCurrency(currency);
        if (header.Contains($"({code})", StringComparison.OrdinalIgnoreCase))
            return header;
        return $"{header} ({code})";
    }
}