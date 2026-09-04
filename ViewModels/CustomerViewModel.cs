using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;
using System.ComponentModel;
using System.Windows;

namespace KarzounERP.ViewModels;

public partial class CustomerViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly IExcelService _excelService;
    private readonly ICustomerService _customerService;
    private readonly ICompanyService _companyService;
    private readonly AppSession _session;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<Customer> _customers = new();
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ImportanceLevel? _filterImportance;
    [ObservableProperty] private FollowUpStage? _filterStage;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private bool? _areAllCustomersSelected;
    private bool _updatingSelection;

    public string CompanyColorMarker => ResolveThemeColor(_session.ActiveCompanyId);

    public List<ImportanceLevel?> ImportanceLevels { get; } =
        new List<ImportanceLevel?> { null }.Concat(Enum.GetValues<ImportanceLevel>().Cast<ImportanceLevel?>()).ToList();
    public List<FollowUpStage?> FollowUpStages { get; } =
        new List<FollowUpStage?> { null }.Concat(Enum.GetValues<FollowUpStage>().Cast<FollowUpStage?>()).ToList();

    private readonly INotificationService _notificationService;

    public CustomerViewModel(ICustomerService customerService, ICompanyService companyService, AppSession session,
        NavigationService navigationService, IExcelService excelService, INotificationService notificationService)
    {
        _customerService = customerService;
        _companyService = companyService;
        _session = session;
        _navigationService = navigationService;
        _excelService = excelService;
        _notificationService = notificationService;
    }

    public async Task LoadAsync()
    {
        if (!_session.HasActiveCompany) return;
        SetBusy(true, LocalizationManager.Get("Msg_LoadingCustomers"));
        try
        {
            Customers = await _customerService.GetCustomersAsync(
                _session.ActiveCompanyId, SearchText, FilterImportance, FilterStage);
            await ApplyCustomerCompanyColorsAsync();
            OnPropertyChanged(nameof(CompanyColorMarker));
            WireCustomerSelectionNotifications();
            TotalCount = Customers.Count;
            UpdateSelectionState();
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    partial void OnCustomersChanged(List<Customer> value) => WireCustomerSelectionNotifications();

    partial void OnAreAllCustomersSelectedChanged(bool? value)
    {
        if (_updatingSelection || !value.HasValue) return;
        foreach (var customer in Customers)
            customer.IsSelected = value.Value;
        UpdateSelectionState();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var customer in Customers)
            customer.IsSelected = true;
        UpdateSelectionState();
    }

    [RelayCommand]
    private void SelectionChanged() => UpdateSelectionState();

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var customer in Customers)
            customer.IsSelected = false;
        UpdateSelectionState();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selected = Customers.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0) return;
        var result = MessageBox.Show(
            string.Format(LocalizationManager.Get("Msg_ConfirmDeleteSelected"), selected.Count),
            LocalizationManager.Get("Msg_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        foreach (var customer in selected)
            await _customerService.DeleteCustomerAsync(customer.Id);
        _notificationService.Success(string.Format(LocalizationManager.Get("Msg_SelectedDeleted"), selected.Count));
        await LoadAsync();
    }

    private void UpdateSelectionState()
    {
        SelectedCount = Customers.Count(c => c.IsSelected);
        _updatingSelection = true;
        AreAllCustomersSelected = SelectedCount == 0 ? false : SelectedCount == Customers.Count ? true : null;
        _updatingSelection = false;
    }

    private void WireCustomerSelectionNotifications()
    {
        foreach (var customer in Customers)
        {
            customer.PropertyChanged -= CustomerSelectionChanged;
            customer.PropertyChanged += CustomerSelectionChanged;
        }
    }

    private async Task ApplyCustomerCompanyColorsAsync()
    {
        var companies = await _companyService.GetAllCompaniesAsync();
        var byName = companies
            .SelectMany(c => new[] { c.Name, c.CommercialName }
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => new { Name = n!.Trim(), Company = c }))
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Company, StringComparer.OrdinalIgnoreCase);

        foreach (var customer in Customers)
        {
            if (!string.IsNullOrWhiteSpace(customer.ColorMarker))
            {
                customer.DisplayColorMarker = customer.ColorMarker;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(customer.CompanyName) &&
                byName.TryGetValue(customer.CompanyName.Trim(), out var linkedCompany))
            {
                customer.DisplayColorMarker = ResolveThemeColor(linkedCompany.Id);
                continue;
            }

            customer.DisplayColorMarker = CompanyColorMarker;
        }
    }

    private static string ResolveThemeColor(int companyId)
    {
        var global = AppearanceSettingsStore.LoadGlobal();
        var companyTheme = AppearanceSettingsStore.LoadCompanyTheme(companyId);
        return companyTheme.ApplyCompanyTheme && !string.IsNullOrWhiteSpace(companyTheme.ThemePrimaryColor)
            ? companyTheme.ThemePrimaryColor
            : global.PrimaryColor;
    }

    private void CustomerSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_updatingSelection && e.PropertyName == nameof(Customer.IsSelected))
            UpdateSelectionState();
    }

    [RelayCommand]
    private void AddCustomer()
    {
        var vm = App.Services.GetRequiredService<CustomerFormViewModel>();
        vm.PrepareNew(_session.ActiveCompanyId);
        ShowForm(vm);
    }

    [RelayCommand]
    private void EditCustomer(Customer? customer)
    {
        if (customer == null) return;
        var vm = App.Services.GetRequiredService<CustomerFormViewModel>();
        vm.LoadFromCustomer(customer);
        ShowForm(vm);
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(Customer? customer)
    {
        if (customer == null) return;
        var result = MessageBox.Show(
            string.Format(LocalizationManager.Get("Msg_ConfirmDeleteCustomer"), customer.FullName),
            LocalizationManager.Get("Msg_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            await _customerService.DeleteCustomerAsync(customer.Id);
            _notificationService.Success(string.Format(LocalizationManager.Get("Msg_CustomerDeleted") ?? "Customer '{0}' deleted successfully.", customer.FullName));
            await LoadAsync();
        }
    }

    [RelayCommand]
    private void ViewCustomerDetail(Customer? customer)
    {
        if (customer == null) return;
        _navigationService.NavigateTo<CustomerDetailViewModel>(vm => vm.CustomerId = customer.Id);
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = string.Empty;
        FilterImportance = null;
        FilterStage = null;
        await LoadAsync();
    }

    
    [RelayCommand]
    private async Task ImportCustomersAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files|*.xlsx;*.xls",
            Title = LocalizationManager.Get("Cust_ImportExcel") ?? "Import Excel"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            SetBusy(true, LocalizationManager.Get("Msg_Importing") ?? "Importing...");
            try
            {
                var existingCustomers = await _customerService.GetCustomersAsync(_session.ActiveCompanyId);
                var result = _excelService.ImportCustomers(openFileDialog.FileName, _session.ActiveCompanyId, existingCustomers);
                
                int actuallySaved = 0;
                foreach (var c in result.CustomersToSave)
                {
                    await _customerService.AddCustomerAsync(c);
                    actuallySaved++;
                }

                var msgTemplate = LocalizationManager.Get("Msg_ImportSummary") ?? "Imported: {0}, Skipped: {1}, Duplicates: {2}, Errors: {3}";
                var msg = string.Format(msgTemplate, 
                    actuallySaved, result.Summary.SkippedCount, result.Summary.DuplicateCount, result.Summary.ErrorCount);
                
                if(!string.IsNullOrWhiteSpace(result.Summary.Message)) msg += "\n\nError: " + result.Summary.Message;
                
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
    private void ExportSelectedColumns()
    {
        var dialog = new KarzounERP.Views.Customers.ColumnSelectionDialog();
        if (dialog.ShowDialog() == true)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                DefaultExt = ".xlsx",
                Title = LocalizationManager.Get("Cust_ExportSelected") ?? "Export"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _excelService.ExportSelectedColumns(Customers, dialog.SelectedColumns, saveFileDialog.FileName);
                    _notificationService.Success(LocalizationManager.Get("Msg_ExportSuccess") ?? "Export Successful!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }


    private async void ShowForm(CustomerFormViewModel vm)
    {
        var dialog = new Views.Customers.CustomerFormDialog { DataContext = vm };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }
}
