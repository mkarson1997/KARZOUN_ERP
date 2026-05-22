using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;

namespace FornixxCRM.ViewModels;

public partial class CustomerFormViewModel : BaseViewModel
{
    private readonly ICustomerService _customerService;
    private int _editingId = 0;
    private int _companyId;

    [ObservableProperty] private string _windowTitle = LocalizationManager.Get("CustForm_TitleNew");
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private CommercialMindset _commercialMindset = CommercialMindset.Simple;
    [ObservableProperty] private FollowUpStage _followUpStage = FollowUpStage.New;
    [ObservableProperty] private string _currentObjection = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private ImportanceLevel _importance = ImportanceLevel.Normal;
    [ObservableProperty] private DateTime? _lastFollowUpDate;
    [ObservableProperty] private DateTime? _nextFollowUpDate;
    [ObservableProperty] private string _validationError = string.Empty;

    public IEnumerable<CommercialMindset> AllCommercialMindsets => ArabicEnumHelper.AllCommercialMindsets;
    public IEnumerable<FollowUpStage> AllFollowUpStages => ArabicEnumHelper.AllFollowUpStages;
    public IEnumerable<ImportanceLevel> AllImportanceLevels => ArabicEnumHelper.AllImportanceLevels;

    public bool? DialogResult { get; private set; }
    public event EventHandler? RequestClose;

    public CustomerFormViewModel(ICustomerService customerService) => _customerService = customerService;

    public void PrepareNew(int companyId)
    {
        _editingId = 0; _companyId = companyId;
        WindowTitle = LocalizationManager.Get("CustForm_TitleNewFull");
        FullName = Country = Phone = Email = CompanyName = CurrentObjection = Notes = string.Empty;
        CommercialMindset = CommercialMindset.Simple;
        FollowUpStage = FollowUpStage.New;
        Importance = ImportanceLevel.Normal;
        LastFollowUpDate = null;
        NextFollowUpDate = null;
    }

    public void LoadFromCustomer(Customer c)
    {
        _editingId = c.Id; _companyId = c.CompanyId;
        WindowTitle = LocalizationManager.Get("CustForm_TitleEdit");
        FullName = c.FullName; Country = c.Country ?? ""; Phone = c.Phone ?? "";
        Email = c.Email ?? ""; CompanyName = c.CompanyName ?? "";
        CommercialMindset = c.CommercialMindset; FollowUpStage = c.FollowUpStage;
        CurrentObjection = c.CurrentObjection ?? ""; Notes = c.Notes ?? "";
        Importance = c.Importance; LastFollowUpDate = c.LastFollowUpDate; NextFollowUpDate = c.NextFollowUpDate;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName)) { ValidationError = LocalizationManager.Get("Msg_ValidationCustomerName"); return; }
        ValidationError = string.Empty;

        var customer = _editingId > 0
            ? await _customerService.GetCustomerAsync(_editingId) ?? new Customer()
            : new Customer();

        customer.CompanyId = _companyId;
        customer.FullName = FullName.Trim(); customer.Country = Country.Trim();
        customer.Phone = Phone.Trim(); customer.Email = Email.Trim();
        customer.CompanyName = CompanyName.Trim(); customer.CommercialMindset = CommercialMindset;
        customer.FollowUpStage = FollowUpStage; customer.CurrentObjection = CurrentObjection.Trim();
        customer.Notes = Notes.Trim(); customer.Importance = Importance;
        customer.LastFollowUpDate = LastFollowUpDate;
        customer.NextFollowUpDate = NextFollowUpDate;

        if (_editingId > 0) await _customerService.UpdateCustomerAsync(customer);
        else await _customerService.AddCustomerAsync(customer);

        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() { DialogResult = false; RequestClose?.Invoke(this, EventArgs.Empty); }
}
