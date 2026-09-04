using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KarzounERP.Helpers;
using KarzounERP.Models;

namespace KarzounERP.ViewModels;

public partial class LineItemViewModel : ObservableObject
{
    [ObservableProperty] private bool _isProductNameInvalid;
    [ObservableProperty] private bool _isQuantityInvalid;
    [ObservableProperty] private bool _isUnitPriceInvalid;

    public int Id { get; set; }
    public int? ProductId { get; set; }
    [ObservableProperty]
    private string? _imagePath;
    [ObservableProperty]
    private string _searchText = string.Empty;
    [ObservableProperty]
    private ObservableCollection<ProductPickerItem> _filteredPickerItems = new();

    private Product? _selectedProduct;
    private ProductPickerItem? _selectedPickerItem;
    private List<Product> _availableProducts = new();
    private string _pickerCurrency = "USD";
    private bool _isApplyingProductSelection;

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                if (value != null)
                    ApplySelectedProduct(value);

                OnPropertyChanged(nameof(CanUseCustomProduct));
            }
        }
    }

    public ProductPickerItem? SelectedPickerItem
    {
        get => _selectedPickerItem;
        set
        {
            if (!SetProperty(ref _selectedPickerItem, value) || value == null)
                return;

            if (value.IsCustomOption)
            {
                UseCustomProduct();
                return;
            }

            if (value.Product != null)
            {
                SelectedProduct = value.Product;
            }
        }
    }

    public bool CanUseCustomProduct =>
        !string.IsNullOrWhiteSpace(SearchText) &&
        !ProductSearchHelper.HasExactNameMatch(SearchText, _availableProducts);

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

    [ObservableProperty]
    private string _weightUnit = "kg";

    public decimal LineTotal => UnitPrice * Quantity;

    public event EventHandler? Changed;

    partial void OnUnitPriceChanged(decimal value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnQuantityChanged(int value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnWeightChanged(decimal? value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnWeightUnitChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnSearchTextChanged(string value)
    {
        if (_isApplyingProductSelection)
            return;

        if (SelectedProduct != null && string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(ProductName))
        {
            SearchText = ProductName;
            return;
        }

        if (SelectedProduct != null && !string.Equals(value, ProductName, StringComparison.Ordinal))
            SelectedProduct = null;

        RefreshPickerItems();
    }

    public void SetAvailableProducts(IEnumerable<Product> products, string? currency)
    {
        _availableProducts = products?.ToList() ?? new List<Product>();
        if (!string.IsNullOrWhiteSpace(currency))
            _pickerCurrency = currency;

        RefreshPickerItems();
    }

    private void RefreshPickerItems()
    {
        if (FilteredPickerItems == null)
            FilteredPickerItems = new ObservableCollection<ProductPickerItem>();

        FilteredPickerItems.Clear();

        foreach (var product in ProductSearchHelper.SearchProducts(_availableProducts, SearchText))
        {
            FilteredPickerItems.Add(new ProductPickerItem
            {
                Product = product,
                DisplayLine = ProductSearchHelper.FormatPickerLine(product, _pickerCurrency)
            });
        }

        if (!string.IsNullOrWhiteSpace(SearchText) &&
            !ProductSearchHelper.HasExactNameMatch(SearchText, _availableProducts))
        {
            FilteredPickerItems.Add(new ProductPickerItem
            {
                IsCustomOption = true,
                CustomName = SearchText.Trim(),
                DisplayLine = $"{LocalizationManager.Get("DocForm_AddCustomProduct")} — {SearchText.Trim()}"
            });
        }

        OnPropertyChanged(nameof(CanUseCustomProduct));
    }

    [RelayCommand]
    private void UseCustomProduct()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return;

        SelectedProduct = null;
        SelectedPickerItem = null;
        ProductId = null;
        ProductType = ProductType.Physical;
        Weight = null;
        WeightUnit = "kg";
        UnitPrice = 0;
        ImagePath = null;
        ProductName = SearchText.Trim();
        OnPropertyChanged(nameof(CanUseCustomProduct));
    }

    private void ApplySelectedProduct(Product product)
    {
        var displayName = ResolveProductName(product);
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = product.Name?.Trim() ?? string.Empty;

        _isApplyingProductSelection = true;
        try
        {
            ProductId = product.Id;
            ProductName = displayName;
            ProductType = product.Type;
            Weight = product.Weight;
            WeightUnit = string.IsNullOrWhiteSpace(product.WeightUnit) ? "kg" : product.WeightUnit;
            UnitPrice = product.UnitPrice;
            ImagePath = product.ImagePath;
            SearchText = displayName;
        }
        finally
        {
            _isApplyingProductSelection = false;
        }

        RefreshPickerItems();
    }

    public SalesDocumentItem ToModel() => new SalesDocumentItem
    {
        Id = Id,
        ProductId = ProductId,
        ProductName = ProductName,
        ProductType = ProductType,
        Description = Description,
        Weight = Weight,
        WeightUnit = WeightUnit,
        UnitPrice = UnitPrice,
        Quantity = Quantity,
        LineTotal = LineTotal,
        ImagePath = ImagePath
    };

    public static LineItemViewModel FromModel(SalesDocumentItem item)
    {
        var vm = new LineItemViewModel
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ProductType = item.ProductType,
            Description = item.Description ?? string.Empty,
            Weight = item.Weight,
            WeightUnit = item.WeightUnit ?? "kg",
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            ImagePath = item.ImagePath
        };
        vm.SearchText = item.ProductName;
        return vm;
    }

    private static string ResolveProductName(Product product)
    {
        var preferred = ProductSearchHelper.GetPreferredName(product);
        if (!string.IsNullOrWhiteSpace(preferred))
            return preferred.Trim();

        if (!string.IsNullOrWhiteSpace(product.Name))
            return product.Name.Trim();

        return product.DisplayName?.Trim() ?? string.Empty;
    }
}
