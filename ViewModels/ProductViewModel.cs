using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;
using System.Windows;

namespace FornixxCRM.ViewModels;

public partial class ProductViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly IProductService _productService;
    private readonly AppSession _session;

    [ObservableProperty] private List<Product> _products = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalCount;

    public ProductViewModel(IProductService productService, AppSession session)
    {
        _productService = productService;
        _session = session;
    }

    public async Task LoadAsync()
    {
        if (!_session.HasActiveCompany) return;
        SetBusy(true);
        try
        {
            Products = await _productService.GetProductsAsync(_session.ActiveCompanyId, SearchText);
            TotalCount = Products.Count;
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    [RelayCommand]
    private void AddProduct()
    {
        var vm = App.Services.GetRequiredService<ProductFormViewModel>();
        vm.PrepareNew(_session.ActiveCompanyId);
        ShowForm(vm);
    }

    [RelayCommand]
    private void EditProduct(Product? product)
    {
        if (product == null) return;
        var vm = App.Services.GetRequiredService<ProductFormViewModel>();
        vm.LoadFromProduct(product);
        ShowForm(vm);
    }

    [RelayCommand]
    private async Task DeleteProductAsync(Product? product)
    {
        if (product == null) return;
        var result = MessageBox.Show(
            string.Format(LocalizationManager.Get("Msg_ConfirmDeleteProduct"), product.Name),
            LocalizationManager.Get("Msg_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            await _productService.DeleteProductAsync(product.Id);
            await LoadAsync();
        }
    }

    private async void ShowForm(ProductFormViewModel vm)
    {
        var dialog = new Views.Products.ProductFormDialog { DataContext = vm };
        if (dialog.ShowDialog() == true) await LoadAsync();
    }
}
