using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;

namespace KarzounERP.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly NavigationService _navigationService;
    private readonly AppSession _session;
    private readonly ICompanyService _companyService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _activeCompanyName = LocalizationManager.Get("Msg_NoCompany");

    [ObservableProperty]
    private List<Company> _companies = new();

    [ObservableProperty]
    private Company? _selectedCompany;

    [ObservableProperty] private string _notificationMessage = string.Empty;
    [ObservableProperty] private bool _isNotificationVisible;
    [ObservableProperty] private string _notificationIcon = "Information";
    [ObservableProperty] private string _notificationColor = "#1976D2";

    private System.Threading.CancellationTokenSource? _notificationCts;

    public MainViewModel(NavigationService navigationService, AppSession session,
        ICompanyService companyService, INotificationService notificationService)
    {
        _navigationService = navigationService;
        _session = session;
        _companyService = companyService;
        _notificationService = notificationService;

        _navigationService.NavigationRequested += (_, vm) => CurrentViewModel = vm;
        _session.ActiveCompanyChanged += (_, c) =>
        {
            ThemeManager.ApplyTheme(c?.Id ?? 0);
            ActiveCompanyName = c?.Name ?? LocalizationManager.Get("Msg_NoCompany");
        };
        // Refresh sidebar dropdown whenever a company is added / edited / deleted
        _session.CompaniesChanged += async (_, _) => await RefreshCompaniesAsync();

        LocalizationManager.LanguageChanged += async (_, _) => await OnLanguageChangedAsync();

        _notificationService.NotificationTriggered += (msg, type, duration) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => ShowNotification(msg, type, duration));
        };
    }

    private void ShowNotification(string message, NotificationType type, int durationMs)
    {
        _notificationCts?.Cancel();
        _notificationCts = new System.Threading.CancellationTokenSource();
        var token = _notificationCts.Token;

        NotificationMessage = message;
        IsNotificationVisible = true;

        switch (type)
        {
            case NotificationType.Success:
                NotificationColor = "#2E7D32"; // green
                NotificationIcon = "CheckCircle";
                break;
            case NotificationType.Error:
                NotificationColor = "#D32F2F"; // red
                NotificationIcon = "AlertCircle";
                break;
            case NotificationType.Warning:
                NotificationColor = "#F57C00"; // orange
                NotificationIcon = "Alert";
                break;
            case NotificationType.Info:
            default:
                NotificationColor = "#1976D2"; // blue
                NotificationIcon = "Information";
                break;
        }

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(durationMs, token);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        IsNotificationVisible = false;
                    }
                });
            }
            catch (TaskCanceledException) { }
        });
    }

    [RelayCommand]
    private void DismissNotification()
    {
        _notificationCts?.Cancel();
        IsNotificationVisible = false;
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
                var prompt = new KarzounERP.Views.Settings.PasswordPromptWindow(target.AppPassword);
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
            var prompt = new KarzounERP.Views.Settings.PasswordPromptWindow(value.AppPassword);
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
    private void NavigateToAppearance() => _navigationService.NavigateTo<AppearanceViewModel>();

    [RelayCommand]
    private void NavigateToSettings() => _navigationService.NavigateTo<SettingsViewModel>();

    [RelayCommand]
    private void NavigateToLogs() => _navigationService.NavigateTo<LogViewModel>();

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
