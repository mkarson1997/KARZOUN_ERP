using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Reports;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace KarzounERP.ViewModels;

public partial class DashboardViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly IDocumentService _documentService;
    private readonly ICustomerService _customerService;
    private readonly NavigationService _navigationService;
    private readonly AppSession _session;

    [ObservableProperty] private int _totalCustomers;
    [ObservableProperty] private int _totalQuotations;
    [ObservableProperty] private int _totalInvoices;
    [ObservableProperty] private decimal _totalSalesAmount;
    [ObservableProperty] private decimal _paidTotal;
    [ObservableProperty] private decimal _unpaidTotal;
    [ObservableProperty] private List<RecentDocument> _recentDocuments = new();
    [ObservableProperty] private List<TopCustomer> _topCustomers = new();
    [ObservableProperty] private List<MonthlySummary> _monthlySummary = new();
    [ObservableProperty] private string _currency = "USD";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFollowUpReminders))]
    private List<Customer> _followUpReminders = new();

    public bool HasFollowUpReminders => FollowUpReminders.Count > 0;

    public DashboardViewModel(
        IDocumentService documentService,
        ICustomerService customerService,
        NavigationService navigationService,
        AppSession session)
    {
        _documentService = documentService;
        _customerService = customerService;
        _navigationService = navigationService;
        _session = session;
        _session.ActiveCompanyChanged += async (_, _) => await LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (!_session.HasActiveCompany) return;
        SetBusy(true, LocalizationManager.Get("Msg_LoadingDashboard"));
        try
        {
            Currency = _session.ActiveCompanyCurrency;
            var stats = await _documentService.GetDashboardStatsAsync(_session.ActiveCompanyId);
            TotalCustomers = stats.TotalCustomers;
            TotalQuotations = stats.TotalQuotations;
            TotalInvoices = stats.TotalInvoices;
            TotalSalesAmount = stats.TotalSalesAmount;
            PaidTotal = stats.PaidTotal;
            UnpaidTotal = stats.UnpaidTotal;
            RecentDocuments = stats.RecentDocuments;
            TopCustomers = stats.TopCustomers;
            MonthlySummary = stats.MonthlySummary;

            FollowUpReminders = await _customerService.GetFollowUpRemindersAsync(_session.ActiveCompanyId);
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void GoToCustomer(Customer? customer)
    {
        if (customer == null) return;
        var vm = App.Services.GetRequiredService<CustomerDetailViewModel>();
        vm.CustomerId = customer.Id;
        _navigationService.NavigateTo(vm);
    }
}
