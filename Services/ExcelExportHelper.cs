using ClosedXML.Excel;
using FornixxCRM.Helpers;
using FornixxCRM.Pdf;

namespace FornixxCRM.Services;

internal static class ExcelExportHelper
{
    public static string Lang => LocalizationManager.Language;

    public static void WriteHeaderRow(IXLWorksheet ws, string[] headers, int columnCount)
    {
        for (var i = 0; i < columnCount; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FF6B00");
            cell.Style.Font.FontColor = XLColor.White;
        }
        ws.SheetView.FreezeRows(1);
        ws.RightToLeft = LocalizationManager.IsRtl;
    }

    public static void FinishSheet(IXLWorksheet ws)
    {
        ws.Columns().AdjustToContents();
    }

    public static string FormatDate(DateTime date) => PdfFormatters.FormatDate(date, Lang);

    public static string L(string key) => PdfLabels.Get(key, Lang);
}
