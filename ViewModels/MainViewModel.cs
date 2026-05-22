using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;

namespace FornixxCRM.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly NavigationService _navigationService;
    private readonly AppSession _session;
    private readonly ICompanyService _companyService;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _activeCompanyName = LocalizationManager.Get("Msg_NoCompany");

    [ObservableProperty]
    private List<Company> _companies = new();

    [ObservableProperty]
    private Company? _selectedCompany;

    public MainViewModel(NavigationService navigationService, AppSession session,
        ICompanyService companyService)
    {
        _navigationService = navigationService;
        _session = session;
        _companyService = companyService;

        _navigationService.NavigationRequested += (_, vm) => CurrentViewModel = vm;
        _session.ActiveCompanyChanged += (_, c) => ActiveCompanyName = c?.Name ?? LocalizationManager.Get("Msg_NoCompany");
        // Refresh sidebar dropdown whenever a company is added / edited / deleted
        _session.CompaniesChanged += async (_, _) => await RefreshCompaniesAsync();

        LocalizationManager.LanguageChanged += async (_, _) => await OnLanguageChangedAsync();
    }

    private async Task OnLanguageChangedAsync()
    {
        ActiveCompanyName = _session.ActiveCompany?.Name ?? LocalizationManager.Get("Msg_NoCompany");

        if (CurrentViewModel is ILocalizableViewModel localizable)
            localizable.RefreshLocalization();

        if (CurrentViewModel is ILoadableViewModel loadable && _session.HasActiveCompany)
            await loadable.LoadAsync();
    }

    private Company? _previousCompany;
    private bool _isApplyingCompanySelection = false;

    public async Task InitializeAsync()
    {
        Companies = await _companyService.GetAllCompaniesAsync();
        if (Companies.Any())
        {
            var target = Companies.First();
            if (!string.IsNullOrWhiteSpace(target.AppPassword))
            {
                var prompt = new FornixxCRM.Views.Settings.PasswordPromptWindow(target.AppPassword);
                prompt.Owner = System.Windows.Application.Current.MainWindow;
                if (prompt.ShowDialog() != true)
                {
                    // Fallback to another unlocked company if available
                    var unlocked = Companies.FirstOrDefault(c => string.IsNullOrWhiteSpace(c.AppPassword));
                    if (unlocked != null)
                    {
                        target = unlocked;
                    }
                    else
                    {
                        // Safe exit if no unlocked company exists and they cancelled the prompt
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                }
            }

            SelectedCompany = target;
            _previousCompany = target;
            _session.ActiveCompany = SelectedCompany;
        }
        _navigationService.NavigateTo<DashboardViewModel>();
    }

    partial void OnSelectedCompanyChanged(Company? value)
    {
        if (_isApplyingCompanySelection) return;
        if (value == null) return;
        if (_session.ActiveCompany?.Id == value.Id) return;

        if (!string.IsNullOrWhiteSpace(value.AppPassword))
        {
            var prompt = new FornixxCRM.Views.Settings.PasswordPromptWindow(value.AppPassword);
            prompt.Owner = System.Windows.Application.Current.MainWindow;
            if (prompt.ShowDialog() != true)
            {
                // Revert selection
                _isApplyingCompanySelection = true;
                SelectedCompany = _previousCompany;
                _isApplyingCompanySelection = false;
                return;
            }
        }

        _previousCompany = value;
        _session.ActiveCompany = value;
        _navigationService.NavigateTo<DashboardViewModel>();
    }

    [RelayCommand]
    private void NavigateToDashboard() => _navigationService.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private void NavigateToCustomers() => _navigationService.NavigateTo<CustomerViewModel>();

    [RelayCommand]
    private void NavigateToProducts() => _navigationService.NavigateTo<ProductViewModel>();

    [RelayCommand]
    private void NavigateToQuotations()
        => _navigationService.NavigateTo<DocumentViewModel>(vm => vm.FilterType = Models.DocumentType.Quotation);

    [RelayCommand]
    private void NavigateToInvoices()
        => _navigationService.NavigateTo<DocumentViewModel>(vm => vm.FilterType = Models.DocumentType.Invoice);

    [RelayCommand]
    private void NavigateToReports() => _navigationService.NavigateTo<ReportsViewModel>();

    [RelayCommand]
    private void NavigateToCompanies() => _navigationService.NavigateTo<CompanyViewModel>();

    [RelayCommand]
    private void NavigateToSettings() => _navigationService.NavigateTo<SettingsViewModel>();

    public async Task RefreshCompaniesAsync()
    {
        var list = await _companyService.GetAllCompaniesAsync();
        Companies = list;
        var target = list.FirstOrDefault(c => c.Id == _session.ActiveCompanyId)
            ?? list.FirstOrDefault();
        if (target == null)
        {
            SelectedCompany = null;
            return;
        }
        // Avoid re-assigning same company (prevents unnecessary navigation away from current page).
        if (SelectedCompany?.Id != target.Id)
        {
            SelectedCompany = target;
            _previousCompany = target;
        }
    }
}
