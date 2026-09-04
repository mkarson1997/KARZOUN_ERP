using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KarzounERP.Models;

public class SalesDocument : ObservableObject
{
    private bool _isSelected;

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
    [NotMapped]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    [NotMapped]
    public int TotalQuantity => Items?.Sum(i => i.Quantity) ?? 0;

    [NotMapped]
    public decimal TotalWeightInGrams => Items?.Sum(i => ToGrams(i.Weight, i.WeightUnit) * i.Quantity) ?? 0m;

    [NotMapped]
    public string TotalWeightDisplay => FormatWeight(TotalWeightInGrams);

    public static decimal ToGrams(decimal? weight, string? unit)
    {
        if (!weight.HasValue) return 0m;
        return (unit ?? "kg").Trim().ToLowerInvariant() switch
        {
            "g" or "gram" or "grams" => weight.Value,
            "ton" or "tons" or "tonne" or "tonnes" => weight.Value * 1000000m,
            _ => weight.Value * 1000m
        };
    }

    public static string FormatWeight(decimal grams)
    {
        if (grams <= 0) return "0 g";
        if (grams < 1000m) return $"{grams.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} g";
        var kg = grams / 1000m;
        if (kg < 1000m) return $"{kg.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} kg";
        return $"{(kg / 1000m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} ton";
    }
}
