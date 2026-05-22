using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using FornixxCRM.Helpers;

using FornixxCRM.Models;

using FornixxCRM.Services.Interfaces;

using FornixxCRM.ViewModels.Base;

using System.Windows;



namespace FornixxCRM.ViewModels;



public partial class DocumentViewModel : BaseViewModel, ILoadableViewModel, ILocalizableViewModel

{

    private readonly IDocumentService _documentService;

    private readonly IPdfService _pdfService;

    private readonly AppSession _session;

    private readonly NavigationService _navigationService;



    [ObservableProperty] private List<SalesDocument> _documents = new();

    [ObservableProperty] private SalesDocument? _selectedDocument;

    [ObservableProperty] private DocumentType? _filterType;

    [ObservableProperty] private DocumentStatus? _filterStatus;
    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;

    [ObservableProperty] private string _pageTitle = LocalizationManager.Get("DocList_TitleAll");

    [ObservableProperty] private int _totalCount;



    public List<DocumentStatus?> AllStatuses { get; } =

        new List<DocumentStatus?> { null }.Concat(Enum.GetValues<DocumentStatus>().Cast<DocumentStatus?>()).ToList();



    public DocumentViewModel(IDocumentService documentService, IPdfService pdfService,

        AppSession session, NavigationService navigationService)

    {

        _documentService = documentService;

        _pdfService = pdfService;

        _session = session;

        _navigationService = navigationService;

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

            TotalCount = Documents.Count;

        }

        finally { SetBusy(false); }

    }



    [RelayCommand]

    private async Task FilterChangedAsync() => await LoadAsync();

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

                MessageBox.Show(

                    string.Format(LocalizationManager.Get("Msg_InvoiceCreated"), invoice.DocumentNumber),

                    LocalizationManager.Get("Msg_Success"), MessageBoxButton.OK, MessageBoxImage.Information);

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

            MessageBox.Show(

                string.Format(LocalizationManager.Get("Msg_DocumentDuplicated"), dup.DocumentNumber),

                LocalizationManager.Get("Msg_Success"), MessageBoxButton.OK, MessageBoxImage.Information);

        }

        finally { SetBusy(false); }

    }

}

