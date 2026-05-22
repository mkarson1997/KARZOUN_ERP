using CommunityToolkit.Mvvm.ComponentModel;
using FornixxCRM.Models;

namespace FornixxCRM.ViewModels;

public partial class LineItemViewModel : ObservableObject
{
    public int Id { get; set; }
    public int? ProductId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private string _productName = string.Empty;

    [ObservableProperty]
    private ProductType _productType = ProductType.Physical;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private decimal _unitPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal? _weight;

    public decimal LineTotal => UnitPrice * Quantity;

    public event EventHandler? Changed;

    partial void OnUnitPriceChanged(decimal value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnQuantityChanged(int value) => Changed?.Invoke(this, EventArgs.Empty);

    public SalesDocumentItem ToModel() => new()
    {
        Id = Id,
        ProductId = ProductId,
        ProductName = ProductName,
        ProductType = ProductType,
        Description = Description,
        Weight = Weight,
        UnitPrice = UnitPrice,
        Quantity = Quantity,
        LineTotal = LineTotal
    };

    public static LineItemViewModel FromModel(SalesDocumentItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        ProductType = item.ProductType,
        Description = item.Description ?? string.Empty,
        Weight = item.Weight,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity
    };
}
