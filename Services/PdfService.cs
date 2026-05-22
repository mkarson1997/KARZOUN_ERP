using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Pdf;
using FornixxCRM.Services.Interfaces;
using QuestPDF.Fluent;

namespace FornixxCRM.Services;

public class PdfService : IPdfService
{
    public byte[] GeneratePdf(SalesDocument document, Company company, Customer customer, string language = "ar")
    {
        var finalLanguage = language;
        if (string.IsNullOrWhiteSpace(finalLanguage) || (finalLanguage != "ar" && finalLanguage != "tr" && finalLanguage != "en"))
        {
            finalLanguage = LocalizationManager.Language;
        }
        var doc = new InvoiceDocument(document, company, customer, finalLanguage);
        return doc.GeneratePdf();
    }

    public void SaveAndOpenPdf(SalesDocument document, Company company, Customer customer, string language = "ar")
    {
        var finalLanguage = language;
        if (string.IsNullOrWhiteSpace(finalLanguage) || (finalLanguage != "ar" && finalLanguage != "tr" && finalLanguage != "en"))
        {
            finalLanguage = LocalizationManager.Language;
        }
        var tempPath = Path.Combine(Path.GetTempPath(),
            $"{document.DocumentNumber.Replace("/", "-").Replace("\\", "-")}.pdf");
        var bytes = GeneratePdf(document, company, customer, finalLanguage);
        File.WriteAllBytes(tempPath, bytes);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = tempPath,
            UseShellExecute = true
        });
    }
}
