using ClosedXML.Excel;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;

namespace FornixxCRM.Services;

public class ExcelService : IExcelService
{
    public void ExportCustomers(List<Customer> customers, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(ExcelExportHelper.L("ExcelCustomers"));

        var headers = new[]
        {
            ExcelExportHelper.L("Name"),
            ExcelExportHelper.L("Company"),
            ExcelExportHelper.L("Country"),
            ExcelExportHelper.L("Phone"),
            ExcelExportHelper.L("Email"),
            ExcelExportHelper.L("CustImportance"),
            ExcelExportHelper.L("CustStage"),
            ExcelExportHelper.L("CustMindset"),
            ExcelExportHelper.L("CustNotes"),
            ExcelExportHelper.L("CustCreatedAt")
        };
        ExcelExportHelper.WriteHeaderRow(ws, headers, headers.Length);

        for (var r = 0; r < customers.Count; r++)
        {
            var c = customers[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = c.FullName;
            ws.Cell(row, 2).Value = c.CompanyName ?? "";
            ws.Cell(row, 3).Value = c.Country ?? "";
            ws.Cell(row, 4).Value = c.Phone ?? "";
            ws.Cell(row, 5).Value = c.Email ?? "";
            ws.Cell(row, 6).Value = ArabicEnumHelper.GetImportanceLevelLabel(c.Importance);
            ws.Cell(row, 7).Value = ArabicEnumHelper.GetFollowUpStageLabel(c.FollowUpStage);
            ws.Cell(row, 8).Value = ArabicEnumHelper.GetCommercialMindsetLabel(c.CommercialMindset);
            ws.Cell(row, 9).Value = c.Notes ?? "";
            ws.Cell(row, 10).Value = ExcelExportHelper.FormatDate(c.CreatedAt);
        }

        ExcelExportHelper.FinishSheet(ws);
        wb.SaveAs(filePath);
    }

    public List<Customer> ImportCustomers(string filePath, int companyId)
    {
        var result = new List<Customer>();
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            var name = ws.Cell(r, 1).GetValue<string>();
            if (string.IsNullOrWhiteSpace(name)) continue;
            result.Add(new Customer
            {
                CompanyId = companyId,
                FullName = name,
                CompanyName = ws.Cell(r, 2).GetValue<string>(),
                Country = ws.Cell(r, 3).GetValue<string>(),
                Phone = ws.Cell(r, 4).GetValue<string>(),
                Email = ws.Cell(r, 5).GetValue<string>(),
                CreatedAt = DateTime.UtcNow
            });
        }
        return result;
    }

    public void ExportDocuments(List<SalesDocument> documents, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(ExcelExportHelper.L("ExcelDocuments"));

        var headers = new[]
        {
            ExcelExportHelper.L("DocNo"),
            ExcelExportHelper.L("DocType"),
            ExcelExportHelper.L("Name"),
            ExcelExportHelper.L("Date"),
            ExcelExportHelper.L("Status"),
            ExcelExportHelper.L("ColTotal")
        };
        ExcelExportHelper.WriteHeaderRow(ws, headers, headers.Length);

        for (var r = 0; r < documents.Count; r++)
        {
            var d = documents[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = d.DocumentNumber;
            ws.Cell(row, 2).Value = ArabicEnumHelper.GetDocumentTypeLabel(d.Type);
            ws.Cell(row, 3).Value = d.Customer?.FullName ?? "";
            ws.Cell(row, 4).Value = ExcelExportHelper.FormatDate(d.Date);
            ws.Cell(row, 5).Value = ArabicEnumHelper.GetStatusLabel(d.Status);
            ws.Cell(row, 6).Value = d.GrandTotal;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
        }

        ExcelExportHelper.FinishSheet(ws);
        wb.SaveAs(filePath);
    }

    public void ExportSalesReport(List<SalesDocument> documents, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(ExcelExportHelper.L("ExcelSalesReport"));

        var headers = new[]
        {
            ExcelExportHelper.L("RepMonth"),
            ExcelExportHelper.L("RepInvoiceCount"),
            ExcelExportHelper.L("RepTotal"),
            ExcelExportHelper.L("RepPaid"),
            ExcelExportHelper.L("RepUnpaid")
        };
        ExcelExportHelper.WriteHeaderRow(ws, headers, headers.Length);

        var monthly = documents
            .Where(d => d.Type == DocumentType.Invoice)
            .GroupBy(d => new { d.Date.Year, d.Date.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .ToList();

        for (var r = 0; r < monthly.Count; r++)
        {
            var g = monthly[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = LocalizationManager.FormatMonthYear(g.Key.Month, g.Key.Year);
            ws.Cell(row, 2).Value = g.Count();
            ws.Cell(row, 3).Value = g.Sum(d => d.GrandTotal);
            ws.Cell(row, 4).Value = g.Sum(d => d.PaidAmount);
            ws.Cell(row, 5).Value = g.Where(d => d.Status != DocumentStatus.Cancelled).Sum(d => d.GrandTotal - d.PaidAmount);
            for (var col = 3; col <= 5; col++)
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
        }

        ExcelExportHelper.FinishSheet(ws);
        wb.SaveAs(filePath);
    }
}
