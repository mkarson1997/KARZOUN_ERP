using System.Globalization;

namespace KarzounERP.Pdf;

public static class PdfFormatters
{
    public static string FormatDate(DateTime date, string lang) =>
        PdfLabels.IsArabic(lang)
            ? date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
            : date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string FormatMoney(decimal amount, string currency) =>
        Helpers.MoneyFormatter.FormatMoney(amount, currency);

    public static string FormatDocumentNumber(string? number) =>
        string.IsNullOrWhiteSpace(number) ? "" : number.Trim();
}
