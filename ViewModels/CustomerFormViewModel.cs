using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;

namespace KarzounERP.ViewModels;

public partial class CustomerFormViewModel : BaseViewModel
{
    private readonly ICustomerService _customerService;
    private readonly ICompanyService _companyService;
    private readonly INotificationService _notificationService;
    private int _editingId = 0;
    private int _companyId;

    [ObservableProperty] private string _windowTitle = LocalizationManager.Get("CustForm_TitleNew");
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private List<string> _companyNameOptions = new();
    [ObservableProperty] private string _externalCompanyColor = "#7B1FA2";
    [ObservableProperty] private CommercialMindset _commercialMindset = CommercialMindset.Simple;
    [ObservableProperty] private FollowUpStage _followUpStage = FollowUpStage.New;
    [ObservableProperty] private string _currentObjection = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private ImportanceLevel _importance = ImportanceLevel.Normal;
    [ObservableProperty] private DateTime? _lastFollowUpDate;
    [ObservableProperty] private DateTime? _nextFollowUpDate;
    [ObservableProperty] private string _validationError = string.Empty;
    [ObservableProperty] private bool _isFullNameInvalid;

    public IEnumerable<CommercialMindset> AllCommercialMindsets => ArabicEnumHelper.AllCommercialMindsets;
    public IEnumerable<FollowUpStage> AllFollowUpStages => ArabicEnumHelper.AllFollowUpStages;
    public IEnumerable<ImportanceLevel> AllImportanceLevels => ArabicEnumHelper.AllImportanceLevels;

    public bool? DialogResult { get; private set; }
    public event EventHandler? RequestClose;

    public bool IsExternalCompanyName =>
        !string.IsNullOrWhiteSpace(CompanyName) &&
        !CompanyNameOptions.Any(c => string.Equals(c, CompanyName.Trim(), StringComparison.OrdinalIgnoreCase));

    public CustomerFormViewModel(ICustomerService customerService, ICompanyService companyService, INotificationService notificationService)
    {
        _customerService = customerService;
        _companyService = companyService;
        _notificationService = notificationService;
    }

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
        ExternalCompanyColor = "#7B1FA2";
        LoadCompanyOptions();
    }

    public void LoadFromCustomer(Customer c)
    {
        _editingId = c.Id; _companyId = c.CompanyId;
        WindowTitle = LocalizationManager.Get("CustForm_TitleEdit");
        FullName = c.FullName; Country = c.Country ?? ""; Phone = c.Phone ?? "";
        Email = c.Email ?? ""; CompanyName = c.CompanyName ?? "";
        CommercialMindset = c.CommercialMindset; FollowUpStage = c.FollowUpStage;
        CurrentObjection = c.CurrentObjection ?? ""; Notes = c.Notes ?? "";
        ExternalCompanyColor = string.IsNullOrWhiteSpace(c.ColorMarker) ? "#7B1FA2" : c.ColorMarker;
        Importance = c.Importance; LastFollowUpDate = c.LastFollowUpDate; NextFollowUpDate = c.NextFollowUpDate;
        LoadCompanyOptions();
    }

    partial void OnCompanyNameChanged(string value)
    {
        OnPropertyChanged(nameof(IsExternalCompanyName));
    }

    partial void OnCompanyNameOptionsChanged(List<string> value)
    {
        OnPropertyChanged(nameof(IsExternalCompanyName));
    }

    private void LoadCompanyOptions()
    {
        try
        {
            CompanyNameOptions = _companyService.GetAllCompaniesAsync()
                .GetAwaiter()
                .GetResult()
                .Select(c => string.IsNullOrWhiteSpace(c.CommercialName) ? c.Name : c.CommercialName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();
        }
        catch
        {
            CompanyNameOptions = new List<string>();
        }
    }

    [RelayCommand]
    private void PickExternalCompanyColor()
    {
        var selected = SimpleColorPicker.PickColor(ExternalCompanyColor);
        if (!string.IsNullOrWhiteSpace(selected))
            ExternalCompanyColor = selected;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsFullNameInvalid = string.IsNullOrWhiteSpace(FullName);
        if (IsFullNameInvalid)
        {
            ValidationError = LocalizationManager.Get("Msg_RequiredFields") ?? "Please fill in the required fields.";
            _notificationService.Error(ValidationError);
            RaiseRequestFocus(nameof(FullName));
            return;
        }
        ValidationError = string.Empty;

        var customer = _editingId > 0
            ? await _customerService.GetCustomerAsync(_editingId) ?? new Customer()
            : new Customer();

        customer.CompanyId = _companyId;
        customer.FullName = FullName.Trim(); customer.Country = Country.Trim();
        customer.Phone = Phone.Trim(); customer.Email = Email.Trim();
        customer.CompanyName = CompanyName.Trim(); customer.CommercialMindset = CommercialMindset;
        customer.ColorMarker = IsExternalCompanyName ? ExternalCompanyColor : null;
        customer.FollowUpStage = FollowUpStage; customer.CurrentObjection = CurrentObjection.Trim();
        customer.Notes = Notes.Trim(); customer.Importance = Importance;
        customer.LastFollowUpDate = LastFollowUpDate;
        customer.NextFollowUpDate = NextFollowUpDate;

        bool isEdit = _editingId > 0;
        if (isEdit) await _customerService.UpdateCustomerAsync(customer);
        else await _customerService.AddCustomerAsync(customer);

        _notificationService.Success(isEdit
            ? string.Format(LocalizationManager.Get("Msg_CustomerUpdated") ?? "Customer '{0}' updated successfully.", customer.FullName)
            : string.Format(LocalizationManager.Get("Msg_CustomerCreated") ?? "Customer '{0}' created successfully.", customer.FullName));

        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() { DialogResult = false; RequestClose?.Invoke(this, EventArgs.Empty); }
}
