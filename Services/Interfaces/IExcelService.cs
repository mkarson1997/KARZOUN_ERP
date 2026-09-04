using KarzounERP.Models;

namespace KarzounERP.Services.Interfaces;

public interface IExcelService
{
    void ExportCustomers(List<Customer> customers, string filePath);
    void ExportSelectedColumns(List<Customer> customers, List<string> selectedColumns, string filePath);
    CustomerImportResult ImportCustomers(string filePath, int companyId, List<Customer> existingCustomers);
    void ExportDocuments(List<SalesDocument> documents, string filePath);
    void ExportSalesReport(List<SalesDocument> documents, string filePath);
    ProductImportResult ImportProducts(string filePath, int companyId, List<Product> existingProducts);
    void ExportProducts(List<Product> products, string filePath);
}
