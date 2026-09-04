using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KarzounERP.Models;

public class Product : ObservableObject
{
    private bool _isSelected;
    private bool _isDuplicateCandidate;

    public int Id { get; set; }
    public int CompanyId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public ProductType Type { get; set; } = ProductType.Physical;

    public string? Description { get; set; }
    public decimal? Weight { get; set; }
    public string WeightUnit { get; set; } = "kg"; // "g", "kg", "ton"
    public decimal UnitPrice { get; set; } = 0;
    public int DefaultQuantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string? ImagePath { get; set; }
    [NotMapped]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    [NotMapped]
    public bool IsDuplicateCandidate
    {
        get => _isDuplicateCandidate;
        set => SetProperty(ref _isDuplicateCandidate, value);
    }

    public Company Company { get; set; } = null!;
    public List<ProductLocalizedText> LocalizedTexts { get; set; } = new();

    public string DisplayName
    {
        get
        {
            if (Type == ProductType.Physical && Weight.HasValue)
            {
                var weightStr = Weight.Value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
                return $"{Name} - {weightStr} {WeightUnit}";
            }
            return Name;
        }
    }
}
