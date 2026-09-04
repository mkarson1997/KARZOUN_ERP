using KarzounERP.ViewModels;
using KarzounERP.Services.Interfaces;
using KarzounERP.Helpers;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;

namespace KarzounERP;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly AppSession _session;
    private readonly IBackupService _backupService;
    private readonly DispatcherTimer _autoBackupTimer = new();
    private Button? _activeNavigationButton;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _session = App.Services.GetRequiredService<AppSession>();
        _backupService = App.Services.GetRequiredService<IBackupService>();
        DataContext = viewModel;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentViewModel))
                UpdateNavigationSelection(_viewModel.CurrentViewModel);
        };
        // Apply FlowDirection based on selected language AFTER InitializeComponent
        // so it overrides the XAML attribute value.
        var fd = Helpers.LocalizationManager.FlowDirection;
        var lang = Helpers.LocalizationManager.Language;
        this.FlowDirection = fd;
        Helpers.AppLogger.LogInfo($"[UI Direction] Language={lang} FlowDirection={fd}");

        Helpers.LocalizationManager.LanguageChanged += (_, _) =>
        {
            var fdLive = Helpers.LocalizationManager.FlowDirection;
            var langLive = Helpers.LocalizationManager.Language;
            FlowDirection = fdLive;
            Helpers.AppLogger.LogInfo($"[UI Direction] Language={langLive} FlowDirection={fdLive}");
        };

        _session.ActiveCompanyChanged += (_, _) => RefreshAutoBackupTimer();
        _autoBackupTimer.Tick += (_, _) => RunAutoBackup();
    }

    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            SetActiveNavigationButton(button);
    }

    private void UpdateNavigationSelection(object? viewModel)
    {
        var button = viewModel switch
        {
            DashboardViewModel => DashboardNavButton,
            CustomerViewModel => CustomersNavButton,
            ProductViewModel => ProductsNavButton,
            DocumentViewModel document when document.FilterType == Models.DocumentType.Invoice => InvoicesNavButton,
            DocumentViewModel => QuotationsNavButton,
            ReportsViewModel => ReportsNavButton,
            CompanyViewModel => CompaniesNavButton,
            AppearanceViewModel => AppearanceNavButton,
            SettingsViewModel => SettingsNavButton,
            LogViewModel => LogsNavButton,
            _ => null
        };

        if (button != null)
            SetActiveNavigationButton(button);
    }

    private void SetActiveNavigationButton(Button button)
    {
        if (_activeNavigationButton == button)
            return;

        var buttons = new[]
        {
            DashboardNavButton, CustomersNavButton, ProductsNavButton,
            QuotationsNavButton, InvoicesNavButton, ReportsNavButton,
            CompaniesNavButton, AppearanceNavButton, SettingsNavButton, LogsNavButton
        };

        foreach (var navButton in buttons)
            navButton.Style = (Style)FindResource(navButton == button ? "NavButtonActiveStyle" : "NavButtonStyle");

        _activeNavigationButton = button;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _viewModel.InitializeAsync();
        RefreshAutoBackupTimer();
    }

    private void RefreshAutoBackupTimer()
    {
        _autoBackupTimer.Stop();
        var company = _session.ActiveCompany;
        if (company == null)
        {
            AppLogger.LogInfo("[AutoBackup] Auto backup stopped: active company is null.");
            return;
        }

        if (!company.AutoBackupEnabled)
        {
            AppLogger.LogInfo($"[AutoBackup] Auto backup is disabled for company: {company.Name}.");
            return;
        }

        if (company.AutoBackupIntervalMinutes <= 0)
        {
            AppLogger.LogInfo($"[AutoBackup] Auto backup is disabled because interval is <= 0: {company.AutoBackupIntervalMinutes}m.");
            return;
        }

        _autoBackupTimer.Interval = TimeSpan.FromMinutes(company.AutoBackupIntervalMinutes);
        _autoBackupTimer.Start();
        
        var targetFolder = _backupService.ResolveBackupFolder(company.BackupFolder);
        var nextTime = DateTime.Now.Add(_autoBackupTimer.Interval);
        AppLogger.LogInfo($"[AutoBackup] Enabled. Interval: {company.AutoBackupIntervalMinutes}m. Active backup path: {targetFolder}. Next scheduled backup: {nextTime:yyyy-MM-dd HH:mm:ss}");
    }

    private void RunAutoBackup()
    {
        var company = _session.ActiveCompany;
        if (company == null) return;
        
        var targetFolder = _backupService.ResolveBackupFolder(company.BackupFolder);
        AppLogger.LogInfo($"[AutoBackup] Automatic backup started. Target folder: {targetFolder}");
        try
        {
            var path = _backupService.BackupDatabase(targetFolder);
            var nextTime = DateTime.Now.Add(_autoBackupTimer.Interval);
            AppLogger.LogInfo($"[AutoBackup] Automatic backup succeeded. Saved to: {path}. Next scheduled backup: {nextTime:yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"[AutoBackup] Automatic backup failed. Target folder: {targetFolder}", ex);
        }
    }
}
