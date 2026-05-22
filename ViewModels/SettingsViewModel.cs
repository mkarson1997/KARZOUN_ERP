using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;
using Microsoft.Win32;
using System.Windows;

namespace FornixxCRM.ViewModels;

public partial class SettingsViewModel : BaseViewModel, ILocalizableViewModel
{
    private readonly ICompanyService _companyService;
    private readonly IBackupService _backupService;
    private readonly AppSession _session;

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

    public SettingsViewModel(ICompanyService companyService, IBackupService backupService, AppSession session)
    {
        _companyService = companyService;
        _backupService = backupService;
        _session = session;
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
        DefaultInvoiceNotes = Company.DefaultInvoiceNotes ?? "";
        DefaultQuotationNotes = Company.DefaultQuotationNotes ?? "";
        FooterText = Company.FooterText ?? ""; PaymentInfo = Company.PaymentInfo ?? "";
        LogoPath = Company.LogoPath ?? "";
        StampPath = Company.StampPath ?? "";
        QrCodeTemplate = Company.QrCodeTemplate ?? "";
        BackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FornixxCRM_Backups");
        PasswordEnabled = !string.IsNullOrWhiteSpace(Company.AppPassword);
        PasswordInput = PasswordEnabled ? "********" : string.Empty;
        UpdateNumberPreviews();
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
        Company.Name = Name.Trim(); Company.CommercialName = CommercialName.Trim();
        Company.Currency = Currency.Trim(); Company.TaxEnabled = TaxEnabled; Company.TaxRate = TaxRate;
        Company.InvoicePrefix = string.IsNullOrWhiteSpace(InvoicePrefix) ? "INV" : InvoicePrefix.Trim();
        Company.QuotationPrefix = string.IsNullOrWhiteSpace(QuotationPrefix) ? "QUO" : QuotationPrefix.Trim();
        Company.NextInvoiceNumber = NextInvoiceNumber > 0 ? NextInvoiceNumber : 1;
        Company.NextQuotationNumber = NextQuotationNumber > 0 ? NextQuotationNumber : 1;
        Company.NumberPadding = NumberPadding > 0 ? NumberPadding : 4;
        Company.DefaultInvoiceNotes = DefaultInvoiceNotes;
        Company.DefaultQuotationNotes = DefaultQuotationNotes;
        Company.FooterText = FooterText; Company.PaymentInfo = PaymentInfo;
        Company.LogoPath = string.IsNullOrWhiteSpace(LogoPath) ? null : LogoPath;
        Company.StampPath = string.IsNullOrWhiteSpace(StampPath) ? null : StampPath;
        Company.QrCodeTemplate = QrCodeTemplate;

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
        _session.ActiveCompany = Company;
        StatusMessage = LocalizationManager.Get("Msg_SettingsSaved");
        await Task.Delay(3000);
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void SaveLanguage()
    {
        AppLogger.LogInfo($"[Settings] User selected language: {_selectedLanguage}");
        LocalizationManager.ApplyLanguage(_selectedLanguage, persist: true);
        StatusMessage = LocalizationManager.Get("Msg_LangSaved");
        AppLogger.LogInfo($"[Settings] Language applied live: {LocalizationManager.Language}");
    }

    public void RefreshLocalization() => SyncLanguageFromManager();

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
            var folder = string.IsNullOrWhiteSpace(BackupFolder)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FornixxCRM_Backups")
                : BackupFolder;
            var path = _backupService.BackupDatabase(folder);
            StatusMessage = $"✓ {LocalizationManager.Get("Sett_BackupNow")}: {path}";
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Backup failed", ex);
            StatusMessage = $"✗ {ex.Message}";
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
            MessageBox.Show(LocalizationManager.Get("Msg_RestoreSuccess"),
                LocalizationManager.Get("Msg_RestoreSuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show(LocalizationManager.Get("Msg_RestoreFail"),
                LocalizationManager.Get("Msg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
