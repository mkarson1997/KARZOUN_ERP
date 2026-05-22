using FornixxCRM.Models;

namespace FornixxCRM.Services.Interfaces;

public interface IPdfService
{
    byte[] GeneratePdf(SalesDocument document, Company company, Customer customer, string language = "ar");
    void SaveAndOpenPdf(SalesDocument document, Company company, Customer customer, string language = "ar");
}
