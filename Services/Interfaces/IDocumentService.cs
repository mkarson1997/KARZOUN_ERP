using KarzounERP.Models;
using KarzounERP.Reports;

namespace KarzounERP.Services.Interfaces;

public interface IDocumentService
{
    Task<List<SalesDocument>> GetDocumentsAsync(int companyId, DocumentType? type = null,
        DocumentStatus? status = null, int? customerId = null,
        DateTime? fromDate = null, DateTime? toDate = null);
    Task<SalesDocument?> GetDocumentAsync(int id);
    Task<SalesDocument> CreateDocumentAsync(SalesDocument document, List<SalesDocumentItem> items);
    Task UpdateDocumentAsync(SalesDocument document, List<SalesDocumentItem> items);
    Task DeleteDocumentAsync(int id);
    Task<SalesDocument> ConvertToInvoiceAsync(int quotationId);
    Task<SalesDocument> DuplicateDocumentAsync(int id);
    Task<string> GetNextDocumentNumberAsync(int companyId, DocumentType type);
    Task<string> GetNextDocumentNumberPreviewAsync(int companyId, DocumentType type);
    Task<DashboardStats> GetDashboardStatsAsync(int companyId);
}
