using System.ComponentModel.DataAnnotations;

namespace FornixxCRM.Models;

public class Company
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CommercialName { get; set; } = string.Empty;

    public string? LogoPath { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    public string? Address { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    public string? PaymentInfo { get; set; }
    public string? DefaultInvoiceNotes { get; set; }
    public string? DefaultQuotationNotes { get; set; }
    public string? FooterText { get; set; }

    public bool TaxEnabled { get; set; } = false;
    public decimal TaxRate { get; set; } = 0;

    [MaxLength(20)]
    public string InvoicePrefix { get; set; } = "INV";

    [MaxLength(20)]
    public string QuotationPrefix { get; set; } = "QUO";

    public int NextInvoiceNumber { get; set; } = 1;
    public int NextQuotationNumber { get; set; } = 1;
    public int NumberPadding { get; set; } = 4;
    public string? StampPath { get; set; }
    public string? QrCodeTemplate { get; set; }
    [MaxLength(100)]
    public string? AppPassword { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<SalesDocument> Documents { get; set; } = new List<SalesDocument>();
}
