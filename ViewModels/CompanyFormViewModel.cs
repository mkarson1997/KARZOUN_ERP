using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;
using Microsoft.Win32;

namespace FornixxCRM.ViewModels;

public partial class CompanyFormViewModel : BaseViewModel
{
    private readonly ICompanyService _companyService;
    private int _editingId = 0;

    [ObservableProperty] private string _windowTitle = LocalizationManager.Get("CompForm_TitleNew");
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _commercialName = string.Empty;
    [ObservableProperty] private string _logoPath = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _currency = "USD";
    [ObservableProperty] private string _paymentInfo = string.Empty;
    [ObservableProperty] private string _defaultInvoiceNotes = string.Empty;
    [ObservableProperty] private string _defaultQuotationNotes = string.Empty;
    [ObservableProperty] private string _footerText = string.Empty;
    [ObservableProperty] private bool _taxEnabled;
    [ObservableProperty] private decimal _taxRate;
    [ObservableProperty] private string _invoicePrefix = "INV";
    [ObservableProperty] private string _quotationPrefix = "QUO";
    [ObservableProperty] private string _validationError = string.Empty;

    public bool? DialogResult { get; private set; }
    public event EventHandler? RequestClose;

    public CompanyFormViewModel(ICompanyService companyService) => _companyService = companyService;

    public void PrepareNew()
    {
        _editingId = 0;
        WindowTitle = LocalizationManager.Get("CompForm_TitleNewFull");
        Name = CommercialName = Phone = Email = Address = Country = PaymentInfo =
            DefaultInvoiceNotes = DefaultQuotationNotes = FooterText = LogoPath = string.Empty;
        Currency = "USD"; InvoicePrefix = "INV"; QuotationPrefix = "QUO";
        TaxEnabled = false; TaxRate = 0;
    }

    public void LoadFromCompany(Company c)
    {
        _editingId = c.Id;
        WindowTitle = LocalizationManager.Get("CompForm_TitleEdit");
        Name = c.Name; CommercialName = c.CommercialName; LogoPath = c.LogoPath ?? "";
        Phone = c.Phone ?? ""; Email = c.Email ?? ""; Address = c.Address ?? "";
        Country = c.Country ?? ""; Currency = c.Currency;
        PaymentInfo = c.PaymentInfo ?? ""; DefaultInvoiceNotes = c.DefaultInvoiceNotes ?? "";
        DefaultQuotationNotes = c.DefaultQuotationNotes ?? ""; FooterText = c.FooterText ?? "";
        TaxEnabled = c.TaxEnabled; TaxRate = c.TaxRate;
        InvoicePrefix = c.InvoicePrefix; QuotationPrefix = c.QuotationPrefix;
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
    private async Task SaveAsync()
    {
        var trimmedName = Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName) ||
            trimmedName.Equals("....", StringComparison.OrdinalIgnoreCase))
        {
            ValidationError = LocalizationManager.Get("Msg_ValidationCompanyName");
            return;
        }
        ValidationError = string.Empty;

        var company = _editingId > 0
            ? await _companyService.GetCompanyAsync(_editingId) ?? new Company()
            : new Company();

        company.Name = trimmedName;
        // CommercialName defaults to company name if left blank
        company.CommercialName = string.IsNullOrWhiteSpace(CommercialName)
            ? trimmedName
            : CommercialName.Trim();
        company.LogoPath = string.IsNullOrWhiteSpace(LogoPath) ? null : LogoPath.Trim();
        company.Phone = Phone.Trim();
        company.Email = Email.Trim();
        company.Address = Address.Trim();
        company.Country = Country.Trim();
        // Currency defaults to USD if blank
        company.Currency = string.IsNullOrWhiteSpace(Currency) ? "USD" : Currency.Trim();
        company.PaymentInfo = PaymentInfo.Trim();
        company.DefaultInvoiceNotes = DefaultInvoiceNotes.Trim();
        company.DefaultQuotationNotes = DefaultQuotationNotes.Trim();
        company.FooterText = FooterText.Trim();
        company.TaxEnabled = TaxEnabled;
        company.TaxRate = TaxRate;
        company.InvoicePrefix = string.IsNullOrWhiteSpace(InvoicePrefix) ? "INV" : InvoicePrefix.Trim();
        company.QuotationPrefix = string.IsNullOrWhiteSpace(QuotationPrefix) ? "QUO" : QuotationPrefix.Trim();

        if (_editingId > 0)
            await _companyService.UpdateCompanyAsync(company);
        else
            await _companyService.AddCompanyAsync(company);

        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
