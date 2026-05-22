using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Reports;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;
using Microsoft.Win32;
using System.Windows;

namespace FornixxCRM.ViewModels;

public partial class ReportsViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly IDocumentService _documentService;
    private readonly IExcelService _excelService;
    private readonly AppSession _session;

    [ObservableProperty] private DateTime _fromDate = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private int _totalInvoices;
    [ObservableProperty] private int _totalQuotations;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private decimal _unpaidAmount;
    [ObservableProperty] private List<MonthlySummary> _monthlySummary = new();
    [ObservableProperty] private List<TopCustomer> _topCustomers = new();
    [ObservableProperty] private string _currency = "USD";

    private List<SalesDocument> _allDocuments = new();

    public ReportsViewModel(IDocumentService documentService, IExcelService excelService, AppSession session)
    {
        _documentService = documentService;
        _excelService = excelService;
        _session = session;
    }

    public async Task LoadAsync()
    {
        if (!_session.HasActiveCompany) return;
        Currency = _session.ActiveCompanyCurrency;
        SetBusy(true, LocalizationManager.Get("Msg_Loading"));
        try
        {
            _allDocuments = await _documentService.GetDocumentsAsync(
                _session.ActiveCompanyId, fromDate: FromDate, toDate: ToDate);
            CalculateStats();
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private void CalculateStats()
    {
        var invoices = _allDocuments.Where(d => d.Type == DocumentType.Invoice).ToList();
        var quotations = _allDocuments.Where(d => d.Type == DocumentType.Quotation).ToList();

        TotalInvoices = invoices.Count;
        TotalQuotations = quotations.Count;
        TotalAmount = invoices.Sum(d => d.GrandTotal);
        PaidAmount = invoices.Sum(d => d.PaidAmount);
        UnpaidAmount = invoices.Where(d => d.Status != DocumentStatus.Cancelled).Sum(d => d.GrandTotal - d.PaidAmount);

        MonthlySummary = invoices
            .GroupBy(d => new { d.Date.Year, d.Date.Month })
            .Select(g => new MonthlySummary
            {
                Year = g.Key.Year, Month = g.Key.Month,
                MonthLabel = LocalizationManager.FormatMonthYear(g.Key.Month, g.Key.Year),
                InvoiceCount = g.Count(), TotalAmount = g.Sum(d => d.GrandTotal),
                PaidAmount = g.Sum(d => d.PaidAmount),
                UnpaidAmount = g.Where(d => d.Status != DocumentStatus.Cancelled).Sum(d => d.GrandTotal - d.PaidAmount)
            })
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).ToList();

        TopCustomers = invoices
            .GroupBy(d => new { d.CustomerId, Name = d.Customer?.FullName ?? "" })
            .Select(g => new TopCustomer
            {
                Id = g.Key.CustomerId, Name = g.Key.Name,
                TotalAmount = g.Sum(d => d.GrandTotal), DocumentCount = g.Count()
            })
            .OrderByDescending(tc => tc.TotalAmount).Take(10).ToList();
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = "sales_report.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _excelService.ExportSalesReport(_allDocuments, dlg.FileName);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dlg.FileName, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(LocalizationManager.Get("Msg_PdfError"), ex.Message),
                LocalizationManager.Get("Msg_Error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
