using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;
using System.Windows;

namespace FornixxCRM.ViewModels;

public partial class CustomerViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly ICustomerService _customerService;
    private readonly AppSession _session;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<Customer> _customers = new();
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ImportanceLevel? _filterImportance;
    [ObservableProperty] private FollowUpStage? _filterStage;
    [ObservableProperty] private int _totalCount;

    public List<ImportanceLevel?> ImportanceLevels { get; } =
        new List<ImportanceLevel?> { null }.Concat(Enum.GetValues<ImportanceLevel>().Cast<ImportanceLevel?>()).ToList();
    public List<FollowUpStage?> FollowUpStages { get; } =
        new List<FollowUpStage?> { null }.Concat(Enum.GetValues<FollowUpStage>().Cast<FollowUpStage?>()).ToList();

    public CustomerViewModel(ICustomerService customerService, AppSession session,
        NavigationService navigationService)
    {
        _customerService = customerService;
        _session = session;
        _navigationService = navigationService;
    }

    public async Task LoadAsync()
    {
        if (!_session.HasActiveCompany) return;
        SetBusy(true, LocalizationManager.Get("Msg_LoadingCustomers"));
        try
        {
            Customers = await _customerService.GetCustomersAsync(
                _session.ActiveCompanyId, SearchText, FilterImportance, FilterStage);
            TotalCount = Customers.Count;
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

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

    private async void ShowForm(CustomerFormViewModel vm)
    {
        var dialog = new Views.Customers.CustomerFormDialog { DataContext = vm };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }
}
