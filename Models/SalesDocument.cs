using System.ComponentModel.DataAnnotations;

namespace FornixxCRM.Models;

public class SalesDocument
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int CustomerId { get; set; }

    [Required, MaxLength(50)]
    public string DocumentNumber { get; set; } = string.Empty;

    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public DateTime Date { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }

    public string? Notes { get; set; }
    public string? PaymentAddress { get; set; }
    public string? FooterText { get; set; }
    public string? ShippingNote { get; set; }

    public decimal DiscountAmount { get; set; } = 0;
    public decimal? DiscountPercent { get; set; }
    public decimal TaxRate { get; set; } = 0;

    public decimal Subtotal { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal GrandTotal { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ConvertedFromId { get; set; }

    // Payment tracking (invoices)
    public decimal PaidAmount { get; set; } = 0;
    public DateTime? PaymentDate { get; set; }

    // Language for PDF export: "ar", "tr", "en"
    public string LanguageCode { get; set; } = "ar";

    public Company Company { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<SalesDocumentItem> Items { get; set; } = new List<SalesDocumentItem>();

    // Computed helpers — not mapped to DB
    public bool IsQuotation => Type == DocumentType.Quotation;
    public decimal RemainingAmount => GrandTotal - PaidAmount;
}
