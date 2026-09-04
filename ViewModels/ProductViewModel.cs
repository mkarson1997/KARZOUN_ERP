using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KarzounERP.ViewModels;

public partial class ProductViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly IProductService _productService;
    private readonly AppSession _session;
    private readonly IExcelService _excelService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private List<Product> _products = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private bool? _areAllProductsSelected;
    private bool _updatingSelection;

    public ProductViewModel(IProductService productService, AppSession session, 
        IExcelService excelService, INotificationService notificationService)
    {
        _productService = productService;
        _session = session;
        _excelService = excelService;
        _notificationService = notificationService;
    }

    public async Task LoadAsync()
    {
        if (!_session.HasActiveCompany) return;
        SetBusy(true);
        try
        {
            Products = await _productService.GetProductsAsync(_session.ActiveCompanyId, SearchText);
            WireProductSelectionNotifications();
            TotalCount = Products.Count;
            UpdateSelectionState();
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    partial void OnProductsChanged(List<Product> value) => WireProductSelectionNotifications();

    partial void OnAreAllProductsSelectedChanged(bool? value)
    {
        if (_updatingSelection || !value.HasValue) return;
        foreach (var product in Products)
            product.IsSelected = value.Value;
        UpdateSelectionState();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var product in Products)
            product.IsSelected = true;
        UpdateSelectionState();
    }

    [RelayCommand]
    private void SelectionChanged() => UpdateSelectionState();

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var product in Products)
            product.IsSelected = false;
        UpdateSelectionState();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selected = Products.Where(p => p.IsSelected).ToList();
        if (selected.Count == 0) return;
        var result = MessageBox.Show(
            string.Format(LocalizationManager.Get("Msg_ConfirmDeleteSelected"), selected.Count),
            LocalizationManager.Get("Msg_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        foreach (var product in selected)
            await _productService.DeleteProductAsync(product.Id);
        _notificationService.Success(string.Format(LocalizationManager.Get("Msg_SelectedDeleted"), selected.Count));
        await LoadAsync();
    }

    private void UpdateSelectionState()
    {
        SelectedCount = Products.Count(p => p.IsSelected);
        _updatingSelection = true;
        AreAllProductsSelected = SelectedCount == 0 ? false : SelectedCount == Products.Count ? true : null;
        _updatingSelection = false;
    }

    private void WireProductSelectionNotifications()
    {
        foreach (var product in Products)
        {
            product.PropertyChanged -= ProductSelectionChanged;
            product.PropertyChanged += ProductSelectionChanged;
        }
    }

    private void ProductSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_updatingSelection && e.PropertyName == nameof(Product.IsSelected))
            UpdateSelectionState();
    }

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
            _notificationService.Success(string.Format(LocalizationManager.Get("Msg_ProductDeleted") ?? "Product '{0}' deleted successfully.", product.Name));
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task CheckDuplicatesAsync()
    {
        if (!_session.HasActiveCompany) return;

        var allProducts = await _productService.GetProductsAsync(_session.ActiveCompanyId);
        var pairs = ProductDuplicateHelper.ScanDuplicatePairs(allProducts);

        foreach (var product in Products)
        {
            product.IsDuplicateCandidate = false;
            product.IsSelected = false;
        }

        if (pairs.Count == 0)
        {
            UpdateSelectionState();
            _notificationService.Success(LocalizationManager.Get("Msg_NoSeriousDuplicatesFound") 
                ?? "No serious duplicate or highly similar products were found.");
            return;
        }

        var duplicateIds = pairs
            .SelectMany(p => new[] { p.ProductA.Id, p.ProductB.Id })
            .ToHashSet();

        foreach (var product in Products.Where(p => duplicateIds.Contains(p.Id)))
        {
            product.IsDuplicateCandidate = true;
            product.IsSelected = true;
        }
        UpdateSelectionState();

        var dialog = new Views.Products.DuplicateReportWindow(pairs, _productService, _notificationService);
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
        
        _notificationService.Success(LocalizationManager.Get("Msg_DuplicateCheckCompleted") ?? "Duplicate check completed.");
    }

    [RelayCommand]
    private async Task ImportProductsAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files|*.xlsx;*.xls",
            Title = LocalizationManager.Get("Prod_ImportExcel") ?? "Import Excel"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            SetBusy(true, LocalizationManager.Get("Msg_Importing") ?? "Importing...");
            try
            {
                var existingProducts = await _productService.GetProductsAsync(_session.ActiveCompanyId);
                var result = _excelService.ImportProducts(openFileDialog.FileName, _session.ActiveCompanyId, existingProducts);
                
                int actuallySaved = 0;
                foreach (var p in result.ProductsToSave)
                {
                    await _productService.AddProductAsync(p);
                    actuallySaved++;
                }

                var msgTemplate = LocalizationManager.Get("Msg_ImportSummary") ?? "Imported: {0}, Skipped: {1}, Duplicates: {2}, Errors: {3}";
                var msg = string.Format(msgTemplate, 
                    actuallySaved, result.Summary.SkippedCount, result.Summary.DuplicateCount, result.Summary.ErrorCount);
                
                if (!string.IsNullOrWhiteSpace(result.Summary.Message)) msg += "\n\nError: " + result.Summary.Message;
                
                _notificationService.Success(msg);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocalizationManager.Get("Msg_Error") ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }
    }

    [RelayCommand]
    private void ExportProducts()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            DefaultExt = ".xlsx",
            Title = LocalizationManager.Get("Prod_ExportExcel") ?? "Export Excel"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                _excelService.ExportProducts(Products, saveFileDialog.FileName);
                _notificationService.Success(LocalizationManager.Get("Msg_ExportSuccess") ?? "Export Successful!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ShowForm(ProductFormViewModel vm)
    {
        var dialog = new Views.Products.ProductFormDialog { DataContext = vm };
        if (dialog.ShowDialog() == true) await LoadAsync();
    }
}
