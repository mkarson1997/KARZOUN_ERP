using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using KarzounERP.Helpers;

using KarzounERP.Models;

using KarzounERP.Services.Interfaces;

using KarzounERP.ViewModels.Base;

using System.ComponentModel;
using System.Windows;



namespace KarzounERP.ViewModels;



public partial class DocumentViewModel : BaseViewModel, ILoadableViewModel, ILocalizableViewModel

{

    private readonly IDocumentService _documentService;

    private readonly IPdfService _pdfService;

    private readonly AppSession _session;

    private readonly NavigationService _navigationService;

    private readonly INotificationService _notificationService;



    [ObservableProperty] private List<SalesDocument> _documents = new();

    [ObservableProperty] private SalesDocument? _selectedDocument;

    [ObservableProperty] private DocumentType? _filterType;

    [ObservableProperty] private DocumentStatus? _filterStatus;
    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;

    [ObservableProperty] private string _pageTitle = LocalizationManager.Get("DocList_TitleAll");

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private bool? _areAllDocumentsSelected;
    private bool _updatingSelection;



    public List<DocumentStatus?> AllStatuses { get; } =

        new List<DocumentStatus?> { null }.Concat(ArabicEnumHelper.AllDocumentStatuses.Cast<DocumentStatus?>()).ToList();



    public DocumentViewModel(IDocumentService documentService, IPdfService pdfService,

        AppSession session, NavigationService navigationService, INotificationService notificationService)

    {

        _documentService = documentService;

        _pdfService = pdfService;

        _session = session;

        _navigationService = navigationService;

        _notificationService = notificationService;

    }



    partial void OnFilterTypeChanged(DocumentType? value) => RefreshPageTitle();



    public void RefreshLocalization() => RefreshPageTitle();



    private void RefreshPageTitle()

    {

        PageTitle = FilterType switch

        {

            DocumentType.Quotation => LocalizationManager.Get("DocList_TitleQ"),

            DocumentType.Invoice => LocalizationManager.Get("DocList_TitleI"),

            _ => LocalizationManager.Get("DocList_TitleAll")

        };

    }



    public async Task LoadAsync()

    {

        if (!_session.HasActiveCompany) return;

        SetBusy(true, LocalizationManager.Get("Msg_LoadingDocuments"));

        try

        {

            Documents = await _documentService.GetDocumentsAsync(

                _session.ActiveCompanyId, FilterType, FilterStatus, null, FilterFromDate, FilterToDate);

            WireDocumentSelectionNotifications();
            TotalCount = Documents.Count;
            UpdateSelectionState();

        }

        finally { SetBusy(false); }

    }



    [RelayCommand]

    private async Task FilterChangedAsync() => await LoadAsync();

    partial void OnDocumentsChanged(List<SalesDocument> value) => WireDocumentSelectionNotifications();

    partial void OnAreAllDocumentsSelectedChanged(bool? value)
    {
        if (_updatingSelection || !value.HasValue) return;
        foreach (var document in Documents)
            document.IsSelected = value.Value;
        UpdateSelectionState();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var document in Documents)
            document.IsSelected = true;
        UpdateSelectionState();
    }

    [RelayCommand]
    private void SelectionChanged() => UpdateSelectionState();

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var document in Documents)
            document.IsSelected = false;
        UpdateSelectionState();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selected = Documents.Where(d => d.IsSelected).ToList();
        if (selected.Count == 0) return;
        var result = MessageBox.Show(
            string.Format(LocalizationManager.Get("Msg_ConfirmDeleteSelected"), selected.Count),
            LocalizationManager.Get("Msg_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        foreach (var document in selected)
            await _documentService.DeleteDocumentAsync(document.Id);
        _notificationService.Success(string.Format(LocalizationManager.Get("Msg_SelectedDeleted"), selected.Count));
        await LoadAsync();
    }

    private void UpdateSelectionState()
    {
        SelectedCount = Documents.Count(d => d.IsSelected);
        _updatingSelection = true;
        AreAllDocumentsSelected = SelectedCount == 0 ? false : SelectedCount == Documents.Count ? true : null;
        _updatingSelection = false;
    }

    private void WireDocumentSelectionNotifications()
    {
        foreach (var document in Documents)
        {
            document.PropertyChanged -= DocumentSelectionChanged;
            document.PropertyChanged += DocumentSelectionChanged;
        }
    }

    private void DocumentSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_updatingSelection && e.PropertyName == nameof(SalesDocument.IsSelected))
            UpdateSelectionState();
    }

    [RelayCommand]

    private async Task ResetFiltersAsync()

    {

        FilterStatus = null;

        FilterFromDate = null;

        FilterToDate = null;

        await LoadAsync();

    }



    [RelayCommand]

    private void CreateDocument()

    {

        var vm = App.Services.GetRequiredService<DocumentFormViewModel>();

        vm.PrepareNew(_session.ActiveCompanyId,

            FilterType == DocumentType.Invoice ? DocumentType.Invoice : DocumentType.Quotation);

        _navigationService.NavigateTo(vm);

    }



    [RelayCommand]

    private async Task EditDocumentAsync(SalesDocument? doc)

    {

        if (doc == null) return;

        var vm = App.Services.GetRequiredService<DocumentFormViewModel>();

        await vm.LoadDocumentAsync(doc.Id);

        _navigationService.NavigateTo(vm);

    }



    [RelayCommand]

    private async Task DeleteDocumentAsync(SalesDocument? doc)

    {

        if (doc == null) return;

        var result = MessageBox.Show(

            string.Format(LocalizationManager.Get("Msg_ConfirmDeleteDocument"), doc.DocumentNumber),

            LocalizationManager.Get("Msg_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)

        {

            await _documentService.DeleteDocumentAsync(doc.Id);

            _notificationService.Success(string.Format(LocalizationManager.Get("Msg_DocumentDeleted") ?? "Document '{0}' deleted successfully.", doc.DocumentNumber));

            await LoadAsync();

        }

    }



    [RelayCommand]

    private async Task ExportPdfAsync(SalesDocument? doc)

    {

        if (doc == null) return;

        SetBusy(true, LocalizationManager.Get("Msg_GeneratingPdf"));

        try

        {

            var full = await _documentService.GetDocumentAsync(doc.Id);

            if (full == null) return;

            var finalLanguage = LocalizationManager.Language;

            AppLogger.Info($"[PDF EXPORT] Source=List, AppLang={LocalizationManager.Language}, DocLang={full.LanguageCode}, FinalLang={finalLanguage}, DocNo={full.DocumentNumber}");

            _pdfService.SaveAndOpenPdf(full, full.Company, full.Customer, finalLanguage);

            _notificationService.Success(LocalizationManager.Get("Msg_PdfExportSuccess") ?? "PDF exported successfully.");

        }

        catch (Exception ex)

        {

            MessageBox.Show(

                string.Format(LocalizationManager.Get("Msg_PdfError"), ex.Message),

                LocalizationManager.Get("Msg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);

        }

        finally { SetBusy(false); }

    }



    [RelayCommand]

    private async Task ConvertToInvoiceAsync(SalesDocument? doc)

    {

        if (doc == null || doc.Type != DocumentType.Quotation) return;

        var result = MessageBox.Show(

            string.Format(LocalizationManager.Get("Msg_ConfirmConvertQuotation"), doc.DocumentNumber),

            LocalizationManager.Get("Msg_Confirmation"), MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)

        {

            SetBusy(true, LocalizationManager.Get("Msg_Saving"));

            try

            {

                var invoice = await _documentService.ConvertToInvoiceAsync(doc.Id);

                await LoadAsync();

                _notificationService.Success(string.Format(LocalizationManager.Get("Msg_InvoiceCreated") ?? "Invoice '{0}' created successfully.", invoice.DocumentNumber));

            }

            finally { SetBusy(false); }

        }

    }



    [RelayCommand]

    private async Task DuplicateDocumentAsync(SalesDocument? doc)

    {

        if (doc == null) return;

        SetBusy(true);

        try

        {

            var dup = await _documentService.DuplicateDocumentAsync(doc.Id);

            await LoadAsync();

            _notificationService.Success(string.Format(LocalizationManager.Get("Msg_DocumentDuplicated") ?? "Document '{0}' duplicated.", dup.DocumentNumber));

        }

        finally { SetBusy(false); }

    }

}

