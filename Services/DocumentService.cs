using FornixxCRM.Data;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Reports;
using FornixxCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FornixxCRM.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _context;

    public DocumentService(AppDbContext context) => _context = context;

    public async Task<List<SalesDocument>> GetDocumentsAsync(int companyId, DocumentType? type = null,
        DocumentStatus? status = null, int? customerId = null,
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Documents.Include(d => d.Customer)
            .Where(d => d.CompanyId == companyId);
        if (type.HasValue) query = query.Where(d => d.Type == type.Value);
        if (status.HasValue) query = query.Where(d => d.Status == status.Value);
        if (customerId.HasValue) query = query.Where(d => d.CustomerId == customerId.Value);
        if (fromDate.HasValue) query = query.Where(d => d.Date >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(d => d.Date <= toDate.Value);
        return await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
    }

    public async Task<SalesDocument?> GetDocumentAsync(int id)
        => await _context.Documents
            .Include(d => d.Customer)
            .Include(d => d.Company)
            .Include(d => d.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<SalesDocument> CreateDocumentAsync(SalesDocument document, List<SalesDocumentItem> items)
    {
        document.CreatedAt = DateTime.UtcNow;
        for (int i = 0; i < items.Count; i++) items[i].SortOrder = i;
        document.Items = items;
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task UpdateDocumentAsync(SalesDocument document, List<SalesDocumentItem> items)
    {
        var existing = await _context.DocumentItems.Where(i => i.DocumentId == document.Id).ToListAsync();
        _context.DocumentItems.RemoveRange(existing);

        // Detach any already-tracked SalesDocument with the same key to avoid
        // "instance with same key value is already being tracked" conflict.
        var tracked = _context.ChangeTracker.Entries<SalesDocument>()
            .FirstOrDefault(e => e.Entity.Id == document.Id);
        if (tracked != null) tracked.State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        for (int i = 0; i < items.Count; i++)
        {
            items[i].SortOrder = i;
            items[i].DocumentId = document.Id;
            items[i].Id = 0;
        }
        document.Items = items;
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDocumentAsync(int id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc != null) { _context.Documents.Remove(doc); await _context.SaveChangesAsync(); }
    }

    public async Task<SalesDocument> ConvertToInvoiceAsync(int quotationId)
    {
        var q = await GetDocumentAsync(quotationId)
            ?? throw new InvalidOperationException(LocalizationManager.Get("Msg_QuotationNotFound"));
        var num = await GetNextDocumentNumberAsync(q.CompanyId, DocumentType.Invoice);
        var invoice = new SalesDocument
        {
            CompanyId = q.CompanyId, CustomerId = q.CustomerId, DocumentNumber = num,
            Type = DocumentType.Invoice, Status = DocumentStatus.Draft,
            Date = DateTime.Today, DueDate = q.DueDate, Notes = q.Notes,
            PaymentAddress = q.PaymentAddress, FooterText = q.FooterText,
            ShippingNote = q.ShippingNote, DiscountAmount = q.DiscountAmount,
            DiscountPercent = q.DiscountPercent, TaxRate = q.TaxRate,
            Subtotal = q.Subtotal, TaxAmount = q.TaxAmount, GrandTotal = q.GrandTotal,
            ConvertedFromId = quotationId, CreatedAt = DateTime.UtcNow,
            LanguageCode = q.LanguageCode
        };
        // Mark the quotation as converted
        q.Status = DocumentStatus.Converted;
        _context.Documents.Update(q);
        var items = q.Items.Select(i => new SalesDocumentItem
        {
            ProductName = i.ProductName, ProductType = i.ProductType, Description = i.Description,
            Weight = i.Weight, UnitPrice = i.UnitPrice, Quantity = i.Quantity, LineTotal = i.LineTotal
        }).ToList();
        return await CreateDocumentAsync(invoice, items);
    }

    public async Task<SalesDocument> DuplicateDocumentAsync(int id)
    {
        var orig = await GetDocumentAsync(id)
            ?? throw new InvalidOperationException(LocalizationManager.Get("Msg_DocumentNotFound"));
        var num = await GetNextDocumentNumberAsync(orig.CompanyId, orig.Type);
        var dup = new SalesDocument
        {
            CompanyId = orig.CompanyId, CustomerId = orig.CustomerId, DocumentNumber = num,
            Type = orig.Type, Status = DocumentStatus.Draft,
            Date = DateTime.Today, DueDate = orig.DueDate, Notes = orig.Notes,
            PaymentAddress = orig.PaymentAddress, FooterText = orig.FooterText,
            ShippingNote = orig.ShippingNote, DiscountAmount = orig.DiscountAmount,
            DiscountPercent = orig.DiscountPercent, TaxRate = orig.TaxRate,
            Subtotal = orig.Subtotal, TaxAmount = orig.TaxAmount, GrandTotal = orig.GrandTotal,
            CreatedAt = DateTime.UtcNow
        };
        var items = orig.Items.Select(i => new SalesDocumentItem
        {
            ProductName = i.ProductName, ProductType = i.ProductType, Description = i.Description,
            Weight = i.Weight, UnitPrice = i.UnitPrice, Quantity = i.Quantity, LineTotal = i.LineTotal
        }).ToList();
        return await CreateDocumentAsync(dup, items);
    }

    public async Task<string> GetNextDocumentNumberAsync(int companyId, DocumentType type)
    {
        var company = await _context.Companies.FindAsync(companyId)
            ?? throw new InvalidOperationException(LocalizationManager.Get("Msg_CompanyNotFound"));
        string number;
        var padding = company.NumberPadding > 0 ? company.NumberPadding : 4;
        if (type == DocumentType.Invoice)
        {
            number = $"{company.InvoicePrefix}-{company.NextInvoiceNumber.ToString("D" + padding)}";
            company.NextInvoiceNumber++;
        }
        else
        {
            number = $"{company.QuotationPrefix}-{company.NextQuotationNumber.ToString("D" + padding)}";
            company.NextQuotationNumber++;
        }
        await _context.SaveChangesAsync();
        return number;
    }

    public async Task<string> GetNextDocumentNumberPreviewAsync(int companyId, DocumentType type)
    {
        var company = await _context.Companies.FindAsync(companyId)
            ?? throw new InvalidOperationException(LocalizationManager.Get("Msg_CompanyNotFound"));
        var padding = company.NumberPadding > 0 ? company.NumberPadding : 4;
        if (type == DocumentType.Invoice)
        {
            return $"{company.InvoicePrefix}-{company.NextInvoiceNumber.ToString("D" + padding)}";
        }
        else
        {
            return $"{company.QuotationPrefix}-{company.NextQuotationNumber.ToString("D" + padding)}";
        }
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(int companyId)
    {
        var customers = await _context.Customers.CountAsync(c => c.CompanyId == companyId);
        var quotations = await _context.Documents.CountAsync(d => d.CompanyId == companyId && d.Type == DocumentType.Quotation);
        var invoicesList = await _context.Documents.Include(d => d.Customer)
            .Where(d => d.CompanyId == companyId && d.Type == DocumentType.Invoice).ToListAsync();
        var paidTotal = invoicesList.Sum(d => d.PaidAmount);
        var unpaidTotal = invoicesList.Where(d => d.Status != DocumentStatus.Cancelled).Sum(d => d.GrandTotal - d.PaidAmount);

        // Fetch raw data into memory first — SQLite cannot SUM decimal columns via SQL
        var recentRaw = await _context.Documents.Include(d => d.Customer)
            .Where(d => d.CompanyId == companyId)
            .OrderByDescending(d => d.CreatedAt).Take(5)
            .ToListAsync();
        var recentDocs = recentRaw.Select(d => new RecentDocument
        {
            Id = d.Id, DocumentNumber = d.DocumentNumber,
            CustomerName = d.Customer?.FullName ?? "",
            TypeLabel = ArabicEnumHelper.GetDocumentTypeLabel(d.Type),
            StatusLabel = ArabicEnumHelper.GetStatusLabel(d.Status),
            GrandTotal = d.GrandTotal, Date = d.Date
        }).ToList();

        var topRaw = await _context.Documents.Include(d => d.Customer)
            .Where(d => d.CompanyId == companyId && d.Type == DocumentType.Invoice)
            .Select(d => new { d.CustomerId, Name = d.Customer.FullName, d.GrandTotal })
            .ToListAsync();
        var topCustomers = topRaw
            .GroupBy(d => new { d.CustomerId, d.Name })
            .Select(g => new TopCustomer
            {
                Id = g.Key.CustomerId, Name = g.Key.Name,
                TotalAmount = g.Sum(d => d.GrandTotal), DocumentCount = g.Count()
            })
            .OrderByDescending(tc => tc.TotalAmount).Take(5).ToList();

        var monthlyRaw = await _context.Documents
            .Where(d => d.CompanyId == companyId && d.Type == DocumentType.Invoice
                && d.Date.Year >= DateTime.Today.Year - 1)
            .Select(d => new { d.Date.Year, d.Date.Month, d.GrandTotal, d.PaidAmount, d.Status })
            .ToListAsync();
        var monthly = monthlyRaw
            .GroupBy(d => new { d.Year, d.Month })
            .Select(g => new MonthlySummary
            {
                Year = g.Key.Year, Month = g.Key.Month, InvoiceCount = g.Count(),
                TotalAmount = g.Sum(d => d.GrandTotal),
                PaidAmount = g.Sum(d => d.PaidAmount),
                UnpaidAmount = g.Where(d => d.Status != DocumentStatus.Cancelled).Sum(d => d.GrandTotal - d.PaidAmount)
            })
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).Take(12).ToList();

        foreach (var m in monthly)
            m.MonthLabel = LocalizationManager.FormatMonthYear(m.Month, m.Year);

        return new DashboardStats
        {
            TotalCustomers = customers, TotalQuotations = quotations,
            TotalInvoices = invoicesList.Count, TotalSalesAmount = invoicesList.Sum(d => d.GrandTotal),
            PaidTotal = paidTotal, UnpaidTotal = unpaidTotal,
            RecentDocuments = recentDocs, TopCustomers = topCustomers, MonthlySummary = monthly
        };
    }

    private static string GetArabicMonth(int month) => LocalizationManager.GetMonthName(month);
}
