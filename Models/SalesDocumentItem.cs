using System.ComponentModel.DataAnnotations;

namespace KarzounERP.Models;

public class SalesDocumentItem
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int? ProductId { get; set; }

    [Required, MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public ProductType ProductType { get; set; } = ProductType.Physical;
    public string? Description { get; set; }
    public decimal? Weight { get; set; }
    public string WeightUnit { get; set; } = "kg"; // "g", "kg", "ton"
    public decimal UnitPrice { get; set; } = 0;
    public int Quantity { get; set; } = 1;
    public string? ImagePath { get; set; }
    public decimal LineTotal { get; set; } = 0;
    public int SortOrder { get; set; } = 0;

    public SalesDocument Document { get; set; } = null!;
}
