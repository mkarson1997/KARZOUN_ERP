using System.ComponentModel.DataAnnotations;

namespace FornixxCRM.Models;

public class Product
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public ProductType Type { get; set; } = ProductType.Physical;

    public string? Description { get; set; }
    public decimal? Weight { get; set; }
    public decimal UnitPrice { get; set; } = 0;
    public int DefaultQuantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
}
