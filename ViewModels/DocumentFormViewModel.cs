using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using FornixxCRM.Helpers;

using FornixxCRM.Models;

using FornixxCRM.Services.Interfaces;

using FornixxCRM.ViewModels.Base;

using Microsoft.Win32;

using System.Windows;



namespace FornixxCRM.ViewModels;



public partial class DocumentFormViewModel : BaseViewModel, ILocalizableViewModel

{

    private readonly IDocumentService _documentService;

    private readonly ICustomerService _customerService;

    private readonly IProductService _productService;

    private readonly IPdfService _pdfService;

    private readonly AppSession _session;

    private readonly NavigationService _navigationService;

    private int _editingId = 0;

    private int _companyId;



    // Header

    [ObservableProperty] private string _pageTitle = LocalizationManager.Get("DocForm_NewDocument");

    [ObservableProperty] private DocumentType _documentType = DocumentType.Quotation;

    [ObservableProperty] private string _documentNumber = string.Empty;

    [ObservableProperty] private DocumentStatus _status = DocumentStatus.Draft;

    [ObservableProperty] private DateTime _date = DateTime.Today;

    [ObservableProperty] private DateTime? _dueDate;

    [ObservableProperty] private Customer? _selectedCustomer;

    [ObservableProperty] private List<Customer> _customers = new();

    [ObservableProperty] private string _languageCode = "ar";



    // Items

    [ObservableProperty] private ObservableCollection<LineItemViewModel> _items = new();



    // Totals

    [ObservableProperty] private decimal _subtotal;

    [ObservableProperty] private decimal _discountAmount;

    [ObservableProperty] private decimal _discountPercent;

    [ObservableProperty] private bool _usePercentDiscount;

    [ObservableProperty] private decimal _taxRate;

    [ObservableProperty] private bool _taxEnabled;

    [ObservableProperty] private decimal _taxAmount;

    [ObservableProperty] private decimal _grandTotal;



    // Payment tracking
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private DateTime? _paymentDate;

    public decimal RemainingAmount => GrandTotal - PaidAmount;
    public bool ShowPaymentFields => DocumentType == DocumentType.Invoice && (Status == DocumentStatus.Paid || Status == DocumentStatus.PartiallyPaid);



    // Notes

    [ObservableProperty] private string _notes = string.Empty;

    [ObservableProperty] private string _paymentAddress = string.Empty;

    [ObservableProperty] private string _footerText = string.Empty;

    [ObservableProperty] private string _shippingNote = string.Empty;



    [ObservableProperty] private string _validationError = string.Empty;

    [ObservableProperty] private string _currency = "USD";



    public IEnumerable<DocumentStatus> AllStatuses => ArabicEnumHelper.AllDocumentStatuses;

    public IEnumerable<ProductType> AllProductTypes => ArabicEnumHelper.AllProductTypes;

    public IReadOnlyList<string> LanguageCodes { get; } = new[] { "ar", "tr", "en" };



    public DocumentFormViewModel(IDocumentService documentService, ICustomerService customerService,

        IProductService productService, IPdfService pdfService,

        AppSession session, NavigationService navigationService)

    {

        _documentService = documentService;

        _customerService = customerService;

        _productService = productService;

        _pdfService = pdfService;

        _session = session;

        _navigationService = navigationService;

    }



    public void RefreshLocalization()

    {

        if (_editingId == 0)

        {

            PageTitle = DocumentType == DocumentType.Invoice

                ? LocalizationManager.Get("DocForm_NewInvoice")

                : LocalizationManager.Get("DocForm_NewQuotation");

            LanguageCode = LocalizationManager.Language;

        }

        else

        {

            PageTitle = DocumentType == DocumentType.Invoice

                ? LocalizationManager.Get("DocForm_EditInvoice")

                : LocalizationManager.Get("DocForm_EditQuotation");

        }

    }



    public async void PrepareNew(int companyId, DocumentType type)

    {

        _editingId = 0; _companyId = companyId;

        DocumentType = type;

        PageTitle = type == DocumentType.Invoice

            ? LocalizationManager.Get("DocForm_NewInvoice")

            : LocalizationManager.Get("DocForm_NewQuotation");

        DocumentNumber = await _documentService.GetNextDocumentNumberPreviewAsync(companyId, type);

        Status = DocumentStatus.Draft; Date = DateTime.Today; DueDate = null;

        SelectedCustomer = null; Notes = PaymentAddress = FooterText = ShippingNote = string.Empty;

        DiscountAmount = DiscountPercent = TaxRate = PaidAmount = 0; UsePercentDiscount = false;
        PaymentDate = DateTime.Today;

        Currency = _session.ActiveCompanyCurrency;

        LanguageCode = LocalizationManager.Language;



        var company = _session.ActiveCompany;

        if (company != null)

        {

            TaxEnabled = company.TaxEnabled;

            TaxRate = company.TaxRate;

            Notes = type == DocumentType.Invoice ? company.DefaultInvoiceNotes ?? "" : company.DefaultQuotationNotes ?? "";

            FooterText = company.FooterText ?? "";

            PaymentAddress = company.PaymentInfo ?? "";

        }



        Items = new ObservableCollection<LineItemViewModel>();

        Items.CollectionChanged += (_, _) => RecalculateTotals();

        Customers = await _customerService.GetCustomersAsync(companyId);

    }



    public async Task LoadDocumentAsync(int id)

    {

        var doc = await _documentService.GetDocumentAsync(id);

        if (doc == null) return;

        _editingId = id; _companyId = doc.CompanyId;

        DocumentType = doc.Type;

        PageTitle = doc.Type == DocumentType.Invoice

            ? LocalizationManager.Get("DocForm_EditInvoice")

            : LocalizationManager.Get("DocForm_EditQuotation");

        DocumentNumber = doc.DocumentNumber; Status = doc.Status;

        Date = doc.Date; DueDate = doc.DueDate; Notes = doc.Notes ?? "";

        PaymentAddress = doc.PaymentAddress ?? ""; FooterText = doc.FooterText ?? "";

        ShippingNote = doc.ShippingNote ?? ""; TaxRate = doc.TaxRate;

        TaxEnabled = doc.TaxRate > 0;

        DiscountAmount = doc.DiscountAmount; DiscountPercent = doc.DiscountPercent ?? 0;

        UsePercentDiscount = doc.DiscountPercent.HasValue && doc.DiscountPercent > 0;

        Currency = _session.ActiveCompanyCurrency;

        PaidAmount = doc.PaidAmount;
        PaymentDate = doc.PaymentDate ?? DateTime.Today;

        LanguageCode = string.IsNullOrWhiteSpace(doc.LanguageCode) ? LocalizationManager.Language : doc.LanguageCode;



        Customers = await _customerService.GetCustomersAsync(_companyId);

        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == doc.CustomerId);



        Items = new ObservableCollection<LineItemViewModel>(

            doc.Items.Select(LineItemViewModel.FromModel));

        foreach (var item in Items) item.Changed += (_, _) => RecalculateTotals();

        Items.CollectionChanged += (_, _) => RecalculateTotals();

        RecalculateTotals();

    }



    partial void OnDiscountAmountChanged(decimal value) => RecalculateTotals();

    partial void OnDiscountPercentChanged(decimal value) => RecalculateTotals();

    partial void OnTaxRateChanged(decimal value) => RecalculateTotals();

    partial void OnUsePercentDiscountChanged(bool value) => RecalculateTotals();



    private void RecalculateTotals()

    {

        Subtotal = Items.Sum(i => i.LineTotal);

        decimal effectiveDiscount = UsePercentDiscount

            ? Subtotal * (DiscountPercent / 100m)

            : DiscountAmount;

        decimal afterDiscount = Subtotal - effectiveDiscount;

        TaxAmount = TaxEnabled ? afterDiscount * (TaxRate / 100m) : 0;

        GrandTotal = afterDiscount + TaxAmount;
    }

    partial void OnDocumentTypeChanged(DocumentType value)
    {
        OnPropertyChanged(nameof(ShowPaymentFields));
    }

    partial void OnStatusChanged(DocumentStatus value)
    {
        OnPropertyChanged(nameof(ShowPaymentFields));
        if (value == DocumentStatus.Paid && PaidAmount == 0)
        {
            PaidAmount = GrandTotal;
        }
    }

    partial void OnPaidAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(RemainingAmount));
    }

    partial void OnGrandTotalChanged(decimal value)
    {
        OnPropertyChanged(nameof(RemainingAmount));
    }



    [RelayCommand]

    private void AddItem()

    {

        var item = new LineItemViewModel { Quantity = 1, UnitPrice = 0 };

        item.Changed += (_, _) => RecalculateTotals();

        Items.Add(item);

    }



    [RelayCommand]

    private void RemoveItem(LineItemViewModel? item)

    {

        if (item != null) Items.Remove(item);

    }



    /// <summary>

    /// Saves the document to the database and updates _editingId.

    /// Does NOT navigate away. Returns true on success.

    /// </summary>

    private async Task<bool> SaveInternalAsync()

    {

        try

        {

            var document = BuildDocumentModel();

            var items = Items.Select(i => i.ToModel()).ToList();

            if (_editingId > 0)

            {

                await _documentService.UpdateDocumentAsync(document, items);

            }

            else

            {

                var finalNumber = await _documentService.GetNextDocumentNumberAsync(_companyId, DocumentType);

                document.DocumentNumber = finalNumber;

                DocumentNumber = finalNumber;

                var saved = await _documentService.CreateDocumentAsync(document, items);

                _editingId = saved.Id;

            }

            return true;

        }

        catch (Exception ex)

        {

            AppLogger.LogError("Document save failed", ex);

            MessageBox.Show($"{LocalizationManager.Get("Msg_SaveError")}: {ex.Message}",

                LocalizationManager.Get("Msg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);

            return false;

        }

    }



    [RelayCommand]

    private async Task SaveAsync()

    {

        if (!ValidateForm()) return;

        SetBusy(true, LocalizationManager.Get("Msg_Saving"));

        bool ok = await SaveInternalAsync();

        SetBusy(false);

        if (ok) _navigationService.NavigateTo<DocumentViewModel>(vm => vm.FilterType = DocumentType);

    }



    [RelayCommand]

    private async Task ExportPdfAsync()

    {

        if (!ValidateForm()) return;

        SetBusy(true, LocalizationManager.Get("Msg_Generating"));

        try

        {

            // If new document, save first so we have an ID and Company/Customer loaded

            if (_editingId == 0)

            {

                bool saved = await SaveInternalAsync();

                if (!saved || _editingId == 0) return;

            }



            var doc = await _documentService.GetDocumentAsync(_editingId);

            if (doc == null) return;



            var dlg = new SaveFileDialog

            {

                Filter = "PDF Files|*.pdf",

                FileName = $"{doc.DocumentNumber}.pdf",

                Title = "PDF"

            };

            if (dlg.ShowDialog() == true)

            {

                var finalLanguage = LocalizationManager.Language;

                AppLogger.Info($"[PDF EXPORT] Source=Form, AppLang={LocalizationManager.Language}, DocLang={LanguageCode}, FinalLang={finalLanguage}, DocNo={DocumentNumber}");

                var bytes = _pdfService.GeneratePdf(doc, doc.Company, doc.Customer, finalLanguage);

                await File.WriteAllBytesAsync(dlg.FileName, bytes);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo

                {

                    FileName = dlg.FileName, UseShellExecute = true

                });

            }

        }

        catch (Exception ex)

        {

            AppLogger.LogError("PDF export failed", ex);

            MessageBox.Show($"{LocalizationManager.Get("Msg_ExportError")}: {ex.Message}",

                LocalizationManager.Get("Msg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);

        }

        finally { SetBusy(false); }

    }



    [RelayCommand]

    private void Cancel() => _navigationService.NavigateTo<DocumentViewModel>(vm => vm.FilterType = DocumentType);



    private bool ValidateForm()

    {

        if (SelectedCustomer == null)

        {

            ValidationError = LocalizationManager.Get("Msg_ValidationCustomer");

            return false;

        }

        if (!Items.Any())

        {

            ValidationError = LocalizationManager.Get("Msg_ValidationItems");

            return false;

        }

        if (Items.Any(i => string.IsNullOrWhiteSpace(i.ProductName)))

        {

            ValidationError = LocalizationManager.Get("Msg_ValidationItemName");

            return false;

        }

        if (Items.Any(i => i.Quantity <= 0))

        {

            ValidationError = LocalizationManager.Get("Msg_ValidationQty");

            return false;

        }

        if (Items.Any(i => i.UnitPrice < 0))

        {

            ValidationError = LocalizationManager.Get("Msg_ValidationPrice");

            return false;

        }

        ValidationError = string.Empty;

        return true;

    }



    private SalesDocument BuildDocumentModel() => new()

    {

        Id = _editingId, CompanyId = _companyId,

        CustomerId = SelectedCustomer!.Id, DocumentNumber = DocumentNumber,

        Type = DocumentType, Status = Status, Date = Date, DueDate = DueDate,

        Notes = Notes, PaymentAddress = PaymentAddress, FooterText = FooterText,

        ShippingNote = ShippingNote, TaxRate = TaxEnabled ? TaxRate : 0,

        DiscountAmount = UsePercentDiscount ? Subtotal * (DiscountPercent / 100m) : DiscountAmount,

        DiscountPercent = UsePercentDiscount ? DiscountPercent : null,

        Subtotal = Subtotal, TaxAmount = TaxAmount, GrandTotal = GrandTotal,
        PaidAmount = ShowPaymentFields ? PaidAmount : 0,
        PaymentDate = ShowPaymentFields ? PaymentDate : null,
        LanguageCode = ExportLanguage.ForNewDocument(LanguageCode)
    };

}

