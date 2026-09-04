using System;
using System.ComponentModel.DataAnnotations;

namespace KarzounERP.Models;

public class ProductLocalizedText
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    [Required, MaxLength(10)]
    public string LanguageCode { get; set; } = string.Empty; // "ar", "tr", "en"

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}
