using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;
using Microsoft.Win32;
using System.Windows;

namespace KarzounERP.ViewModels;

public partial class SettingsViewModel : BaseViewModel, ILocalizableViewModel, ILoadableViewModel
{
    private readonly ICompanyService _companyService;
    private readonly IBackupService _backupService;
    private readonly AppSession _session;
    private readonly INotificationService _notificationService;
    private CompanyLocalizedSetting? _loadedLocalizedSetting;

    [ObservableProperty] private Company? _company;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _commercialName = string.Empty;
    [ObservableProperty] private string _currency = "USD";
    [ObservableProperty] private bool _taxEnabled;
    [ObservableProperty] private decimal _taxRate;
    [ObservableProperty] private string _invoicePrefix = "INV";
    [ObservableProperty] private string _quotationPrefix = "QUO";
    [ObservableProperty] private string _defaultInvoiceNotes = string.Empty;
    [ObservableProperty] private string _defaultQuotationNotes = string.Empty;
    [ObservableProperty] private string _footerText = string.Empty;
    [ObservableProperty] private string _paymentInfo = string.Empty;
    [ObservableProperty] private string _logoPath = string.Empty;
    [ObservableProperty] private string _stampPath = string.Empty;
    [ObservableProperty] private string _qrCodeTemplate = string.Empty;
    [ObservableProperty] private string _backupFolder = string.Empty;
    [ObservableProperty] private string _nextInvoicePreview = string.Empty;
    [ObservableProperty] private string _nextQuotationPreview = string.Empty;
    [ObservableProperty] private int _nextInvoiceNumber = 1;
    [ObservableProperty] private int _nextQuotationNumber = 1;
    [ObservableProperty] private int _numberPadding = 4;
    [ObservableProperty] private bool _passwordEnabled;
    [ObservableProperty] private string _passwordInput = string.Empty;
    [ObservableProperty] private bool _showProductImageInQuotation;
    [ObservableProperty] private bool _showCustomerContactInPdf;
    [ObservableProperty] private bool _autoBackupEnabled;
    [ObservableProperty] private int _autoBackupIntervalMinutes = 30;

    [ObservableProperty] private bool _isNameInvalid;
    [ObservableProperty] private bool _isCurrencyInvalid;
    [ObservableProperty] private bool _isInvoicePrefixInvalid;
    [ObservableProperty] private bool _isQuotationPrefixInvalid;
    [ObservableProperty] private bool _isBackupIntervalInvalid;
    [ObservableProperty] private bool _isPasswordInvalid;

    public string AutoBackupLocationText => string.Format(LocalizationManager.Get("Settings_AutoBackupLocation"), BackupFolder);

    partial void OnBackupFolderChanged(string value)
    {
        OnPropertyChanged(nameof(AutoBackupLocationText));
    }


    // Language selection
    private string _selectedLanguage = LocalizationManager.Language;
    public bool IsArabic
    {
        get => _selectedLanguage == "ar";
        set { if (value) { _selectedLanguage = "ar"; OnPropertyChanged(); OnPropertyChanged(nameof(IsTurkish)); OnPropertyChanged(nameof(IsEnglish)); } }
    }
    public bool IsTurkish
    {
        get => _selectedLanguage == "tr";
        set { if (value) { _selectedLanguage = "tr"; OnPropertyChanged(); OnPropertyChanged(nameof(IsArabic)); OnPropertyChanged(nameof(IsEnglish)); } }
    }
    public bool IsEnglish
    {
        get => _selectedLanguage == "en";
        set { if (value) { _selectedLanguage = "en"; OnPropertyChanged(); OnPropertyChanged(nameof(IsArabic)); OnPropertyChanged(nameof(IsTurkish)); } }
    }

    public SettingsViewModel(ICompanyService companyService, IBackupService backupService, AppSession session, INotificationService notificationService)
    {
        _companyService = companyService;
        _backupService = backupService;
        _session = session;
        _notificationService = notificationService;
        _session.ActiveCompanyChanged += async (_, _) => await LoadAsync();
    }

    public async Task LoadAsync()
    {
        SyncLanguageFromManager();

        if (!_session.HasActiveCompany) return;
        Company = await _companyService.GetCompanyAsync(_session.ActiveCompanyId);
        if (Company == null) return;

        Name = Company.Name; CommercialName = Company.CommercialName;
        Currency = Company.Currency; TaxEnabled = Company.TaxEnabled; TaxRate = Company.TaxRate;
        InvoicePrefix = Company.InvoicePrefix; QuotationPrefix = Company.QuotationPrefix;
        NextInvoiceNumber = Company.NextInvoiceNumber; NextQuotationNumber = Company.NextQuotationNumber;
        NumberPadding = Company.NumberPadding;
        LogoPath = Company.LogoPath ?? "";
        StampPath = Company.StampPath ?? "";
        ShowProductImageInQuotation = Company.ShowProductImageInQuotation;
        ShowCustomerContactInPdf = Company.ShowCustomerContactInPdf;
        AutoBackupEnabled = Company.AutoBackupEnabled;
        AutoBackupIntervalMinutes = Company.AutoBackupIntervalMinutes > 0 ? Company.AutoBackupIntervalMinutes : 30;
        BackupFolder = _backupService.ResolveBackupFolder(Company.BackupFolder);
        PasswordEnabled = !string.IsNullOrWhiteSpace(Company.AppPassword);
        PasswordInput = PasswordEnabled ? "********" : string.Empty;
        UpdateNumberPreviews();

        _loadedLocalizedSetting = await _companyService.GetLocalizedSettingAsync(Company.Id, LocalizationManager.Language);
        if (_loadedLocalizedSetting != null)
        {
            DefaultInvoiceNotes = _loadedLocalizedSetting.DefaultInvoiceNotes ?? "";
            DefaultQuotationNotes = _loadedLocalizedSetting.DefaultQuotationNotes ?? "";
            FooterText = _loadedLocalizedSetting.LegalFooterText ?? "";
            PaymentInfo = _loadedLocalizedSetting.DefaultPaymentDetails ?? "";
            QrCodeTemplate = _loadedLocalizedSetting.QrTemplateText ?? "";
        }
        else
        {
            DefaultInvoiceNotes = "";
            DefaultQuotationNotes = "";
            FooterText = "";
            PaymentInfo = "";
            QrCodeTemplate = "";
        }
    }

    private void SyncLanguageFromManager()
    {
        _selectedLanguage = LocalizationManager.Language;
        OnPropertyChanged(nameof(IsArabic));
        OnPropertyChanged(nameof(IsTurkish));
        OnPropertyChanged(nameof(IsEnglish));
    }

    private void UpdateNumberPreviews()
    {
        if (Company == null) return;
        var padding = NumberPadding > 0 ? NumberPadding : 4;
        NextInvoicePreview = $"{InvoicePrefix}-{NextInvoiceNumber.ToString("D" + padding)}";
        NextQuotationPreview = $"{QuotationPrefix}-{NextQuotationNumber.ToString("D" + padding)}";
    }

    partial void OnInvoicePrefixChanged(string value) => UpdateNumberPreviews();
    partial void OnQuotationPrefixChanged(string value) => UpdateNumberPreviews();
    partial void OnNextInvoiceNumberChanged(int value) => UpdateNumberPreviews();
    partial void OnNextQuotationNumberChanged(int value) => UpdateNumberPreviews();
    partial void OnNumberPaddingChanged(int value) => UpdateNumberPreviews();

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (Company == null) return;

        IsNameInvalid = string.IsNullOrWhiteSpace(Name);
        IsCurrencyInvalid = string.IsNullOrWhiteSpace(Currency);
        IsInvoicePrefixInvalid = string.IsNullOrWhiteSpace(InvoicePrefix);
        IsQuotationPrefixInvalid = string.IsNullOrWhiteSpace(QuotationPrefix);
        IsBackupIntervalInvalid = AutoBackupEnabled && AutoBackupIntervalMinutes < 1;
        IsPasswordInvalid = PasswordEnabled && string.IsNullOrWhiteSpace(PasswordInput);

        if (IsNameInvalid || IsCurrencyInvalid || IsInvoicePrefixInvalid || IsQuotationPrefixInvalid || IsBackupIntervalInvalid || IsPasswordInvalid)
        {
            _notificationService.Error(LocalizationManager.Get("Msg_RequiredFields") ?? "Please fill in the required fields.");
            if (IsNameInvalid) RaiseRequestFocus(nameof(Name));
            else if (IsCurrencyInvalid) RaiseRequestFocus(nameof(Currency));
            else if (IsInvoicePrefixInvalid) RaiseRequestFocus(nameof(InvoicePrefix));
            else if (IsQuotationPrefixInvalid) RaiseRequestFocus(nameof(QuotationPrefix));
            else if (IsBackupIntervalInvalid) RaiseRequestFocus(nameof(AutoBackupIntervalMinutes));
            else if (IsPasswordInvalid) RaiseRequestFocus(nameof(PasswordInput));
            return;
        }

        bool previouslyPasswordProtected = !string.IsNullOrWhiteSpace(Company.AppPassword);
        if (previouslyPasswordProtected && !PasswordEnabled)
        {
            var result = MessageBox.Show(
                LocalizationManager.Get("Msg_ConfirmDisablePassword") ?? "Are you sure you want to disable password protection?",
                LocalizationManager.Get("Msg_Confirmation") ?? "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                PasswordEnabled = true;
                return;
            }
        }

        Company.Name = Name.Trim(); Company.CommercialName = CommercialName.Trim();
        Company.Currency = Currency.Trim(); Company.TaxEnabled = TaxEnabled; Company.TaxRate = TaxRate;
        Company.InvoicePrefix = string.IsNullOrWhiteSpace(InvoicePrefix) ? "INV" : InvoicePrefix.Trim();
        Company.QuotationPrefix = string.IsNullOrWhiteSpace(QuotationPrefix) ? "QUO" : QuotationPrefix.Trim();
        Company.NextInvoiceNumber = NextInvoiceNumber > 0 ? NextInvoiceNumber : 1;
        Company.NextQuotationNumber = NextQuotationNumber > 0 ? NextQuotationNumber : 1;
        Company.NumberPadding = NumberPadding > 0 ? NumberPadding : 4;
        Company.LogoPath = string.IsNullOrWhiteSpace(LogoPath) ? null : LogoPath;
        Company.StampPath = string.IsNullOrWhiteSpace(StampPath) ? null : StampPath;
        Company.ShowProductImageInQuotation = ShowProductImageInQuotation;
        Company.ShowCustomerContactInPdf = ShowCustomerContactInPdf;
        Company.AutoBackupEnabled = AutoBackupEnabled;
        Company.AutoBackupIntervalMinutes = AutoBackupIntervalMinutes > 0 ? AutoBackupIntervalMinutes : 30;
        Company.BackupFolder = BackupFolder;

        if (PasswordEnabled)
        {
            if (!string.IsNullOrWhiteSpace(PasswordInput) && PasswordInput != "********")
            {
                Company.AppPassword = PasswordHasher.HashPassword(PasswordInput.Trim());
            }
        }
        else
        {
            Company.AppPassword = null;
        }

        await _companyService.UpdateCompanyAsync(Company);

        var localized = _loadedLocalizedSetting ?? new CompanyLocalizedSetting
        {
            CompanyId = Company.Id,
            LanguageCode = LocalizationManager.Language
        };
        localized.DefaultInvoiceNotes = DefaultInvoiceNotes;
        localized.DefaultQuotationNotes = DefaultQuotationNotes;
        localized.LegalFooterText = FooterText;
        localized.DefaultPaymentDetails = PaymentInfo;
        localized.QrTemplateText = QrCodeTemplate;

        await _companyService.SaveLocalizedSettingAsync(localized);
        _loadedLocalizedSetting = localized;

        _session.ActiveCompany = Company;
        _notificationService.Success(LocalizationManager.Get("Msg_SettingsSaved") ?? "Settings saved successfully.");
        StatusMessage = LocalizationManager.Get("Msg_SettingsSaved");
        await Task.Delay(3000);
        StatusMessage = string.Empty;
    }

    private bool HasUnsavedChanges()
    {
        if (_loadedLocalizedSetting == null) return false;
        return DefaultInvoiceNotes != (_loadedLocalizedSetting.DefaultInvoiceNotes ?? string.Empty) ||
               DefaultQuotationNotes != (_loadedLocalizedSetting.DefaultQuotationNotes ?? string.Empty) ||
               FooterText != (_loadedLocalizedSetting.LegalFooterText ?? string.Empty) ||
               PaymentInfo != (_loadedLocalizedSetting.DefaultPaymentDetails ?? string.Empty) ||
               QrCodeTemplate != (_loadedLocalizedSetting.QrTemplateText ?? string.Empty);
    }

    [RelayCommand]
    private async Task SaveLanguageAsync()
    {
        AppLogger.LogInfo($"[Settings] User selected language: {_selectedLanguage}");

        if (HasUnsavedChanges())
        {
            var result = MessageBox.Show(
                LocalizationManager.Get("Msg_UnsavedChangesConfirm"),
                LocalizationManager.Get("Msg_Warning"),
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await SaveSettingsAsync();
            }
            else if (result == MessageBoxResult.Cancel)
            {
                SyncLanguageFromManager();
                return;
            }
        }

        LocalizationManager.ApplyLanguage(_selectedLanguage, persist: true);
        _notificationService.Success(LocalizationManager.Get("Msg_LangSaved") ?? "Language applied.");
        StatusMessage = LocalizationManager.Get("Msg_LangSaved");
        AppLogger.LogInfo($"[Settings] Language applied live: {LocalizationManager.Language}");
    }

    public void RefreshLocalization()
    {
        SyncLanguageFromManager();
        OnPropertyChanged(nameof(AutoBackupLocationText));
    }

    [RelayCommand]
    private void BrowseLogo()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Title = LocalizationManager.Get("Dialog_SelectLogoTitle")
        };
        if (dlg.ShowDialog() == true) LogoPath = dlg.FileName;
    }

    [RelayCommand]
    private void BrowseStamp()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Title = LocalizationManager.Get("Dialog_SelectStampTitle")
        };
        if (dlg.ShowDialog() == true) StampPath = dlg.FileName;
    }

    [RelayCommand]
    private void BrowseBackupFolder()
    {
        var dlg = new SaveFileDialog
        {
            Title = LocalizationManager.Get("Dialog_SelectBackupFolderTitle"),
            Filter = "Folder|*.",
            FileName = "select_this_folder",
            InitialDirectory = string.IsNullOrWhiteSpace(BackupFolder)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : BackupFolder
        };
        if (dlg.ShowDialog() == true)
        {
            var dir = System.IO.Path.GetDirectoryName(dlg.FileName);
            if (!string.IsNullOrWhiteSpace(dir)) BackupFolder = dir;
        }
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        SetBusy(true, LocalizationManager.Get("Msg_Saving"));
        try
        {
            var folder = _backupService.ResolveBackupFolder(BackupFolder);
            var path = _backupService.BackupDatabase(folder);
            StatusMessage = $"✓ {LocalizationManager.Get("Sett_BackupNow")}: {path}";
            _notificationService.Success(LocalizationManager.Get("Msg_BackupSuccess") ?? "Database backup created successfully.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Backup failed", ex);
            StatusMessage = $"✗ {ex.Message}";
            _notificationService.Error($"{LocalizationManager.Get("Msg_BackupFailed") ?? "Database backup failed"}: {ex.Message}");
        }
        finally { SetBusy(false); }
        await Task.Delay(5000);
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void RestoreBackup()
    {
        var result = MessageBox.Show(
            LocalizationManager.Get("Msg_RestoreConfirm"),
            LocalizationManager.Get("Msg_RestoreWarning"),
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var dlg = new OpenFileDialog
        {
            Filter = "Database Files|*.db",
            Title = LocalizationManager.Get("Dialog_SelectBackupFileTitle")
        };
        if (dlg.ShowDialog() != true) return;

        bool ok = _backupService.RestoreDatabase(dlg.FileName);
        if (ok)
        {
            MessageBox.Show(LocalizationManager.Get("Msg_RestoreSuccess") ?? "Database restored successfully. The application will now restart.",
                LocalizationManager.Get("Msg_RestoreSuccessTitle") ?? "Restore Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            System.Diagnostics.Process.Start(Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "KARZOUN_ERP.exe"));
            Application.Current.Shutdown();
        }
        else
        {
            MessageBox.Show(LocalizationManager.Get("Msg_RestoreFail") ?? "Failed to restore database.",
                LocalizationManager.Get("Msg_Error") ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ClearLogo()
    {
        if (string.IsNullOrEmpty(LogoPath)) return;
        var result = MessageBox.Show(
            LocalizationManager.Get("Msg_ConfirmClearLogo") ?? "Are you sure you want to remove the company logo?",
            LocalizationManager.Get("Msg_Confirmation") ?? "Confirmation",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            LogoPath = string.Empty;
        }
    }

    [RelayCommand]
    private void ClearStamp()
    {
        if (string.IsNullOrEmpty(StampPath)) return;
        var result = MessageBox.Show(
            LocalizationManager.Get("Msg_ConfirmClearStamp") ?? "Are you sure you want to remove the stamp / signature?",
            LocalizationManager.Get("Msg_Confirmation") ?? "Confirmation",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            StampPath = string.Empty;
        }
    }
}
