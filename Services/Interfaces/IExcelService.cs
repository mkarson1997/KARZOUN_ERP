using FornixxCRM.Models;

namespace FornixxCRM.Services.Interfaces;

public interface IExcelService
{
    void ExportCustomers(List<Customer> customers, string filePath);
    List<Customer> ImportCustomers(string filePath, int companyId);
    void ExportDocuments(List<SalesDocument> documents, string filePath);
    void ExportSalesReport(List<SalesDocument> documents, string filePath);
}
