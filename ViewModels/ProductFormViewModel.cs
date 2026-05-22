using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;

namespace FornixxCRM.ViewModels;

public partial class ProductFormViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private int _editingId = 0;
    private int _companyId;

    [ObservableProperty] private string _windowTitle = LocalizationManager.Get("ProdForm_TitleNew");
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private ProductType _type = ProductType.Physical;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private decimal _weight;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private int _defaultQuantity = 1;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _validationError = string.Empty;

    public IEnumerable<ProductType> AllProductTypes => ArabicEnumHelper.AllProductTypes;

    public bool? DialogResult { get; private set; }
    public event EventHandler? RequestClose;

    public ProductFormViewModel(IProductService productService) => _productService = productService;

    public void PrepareNew(int companyId)
    {
        _editingId = 0; _companyId = companyId;
        WindowTitle = LocalizationManager.Get("ProdForm_TitleNewFull");
        Name = Description = string.Empty; Type = ProductType.Physical;
        Weight = 0; UnitPrice = 0; DefaultQuantity = 1; IsActive = true;
    }

    public void LoadFromProduct(Product p)
    {
        _editingId = p.Id; _companyId = p.CompanyId;
        WindowTitle = LocalizationManager.Get("ProdForm_TitleEdit");
        Name = p.Name; Type = p.Type; Description = p.Description ?? "";
        Weight = p.Weight ?? 0; UnitPrice = p.UnitPrice;
        DefaultQuantity = p.DefaultQuantity; IsActive = p.IsActive;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ValidationError = LocalizationManager.Get("Msg_ValidationProductName"); return; }
        if (UnitPrice < 0) { ValidationError = LocalizationManager.Get("Msg_ValidationProductPrice"); return; }
        ValidationError = string.Empty;

        var product = _editingId > 0
            ? await _productService.GetProductAsync(_editingId) ?? new Product()
            : new Product();

        product.CompanyId = _companyId; product.Name = Name.Trim();
        product.Type = Type; product.Description = Description.Trim();
        product.Weight = Weight > 0 ? Weight : null;
        product.UnitPrice = UnitPrice; product.DefaultQuantity = DefaultQuantity;
        product.IsActive = IsActive;

        if (_editingId > 0) await _productService.UpdateProductAsync(product);
        else await _productService.AddProductAsync(product);

        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() { DialogResult = false; RequestClose?.Invoke(this, EventArgs.Empty); }
}
