using System.Globalization;

namespace FornixxCRM.Pdf;

public static class PdfFormatters
{
    public static string FormatDate(DateTime date, string lang) =>
        PdfLabels.IsArabic(lang)
            ? date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
            : date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string FormatMoney(decimal amount, string currency) =>
        amount.ToString("N2", CultureInfo.InvariantCulture) + " " + (string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim());

    public static string FormatDocumentNumber(string? number) =>
        string.IsNullOrWhiteSpace(number) ? "" : number.Trim();
}
