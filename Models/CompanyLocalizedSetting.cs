using System.ComponentModel.DataAnnotations;

namespace KarzounERP.Models;

public class CompanyLocalizedSetting
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    [Required, MaxLength(10)]
    public string LanguageCode { get; set; } = "ar"; // "ar", "tr", "en"

    public string? DefaultInvoiceNotes { get; set; }
    public string? DefaultQuotationNotes { get; set; }
    public string? LegalFooterText { get; set; }
    public string? DefaultPaymentDetails { get; set; }
    public string? QrTemplateText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
