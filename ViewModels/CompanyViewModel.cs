using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels.Base;
using System.ComponentModel;
using System.Windows;

namespace KarzounERP.ViewModels;

public partial class CompanyViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly ICompanyService _companyService;
    private readonly AppSession _session;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<Company> _companies = new();
    [ObservableProperty] private Company? _selectedCompany;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private bool? _areAllCompaniesSelected;
    private bool _updatingSelection;

    private readonly INotificationService _notificationService;

    public CompanyViewModel(ICompanyService companyService, AppSession session,
        NavigationService navigationService, INotificationService notificationService)
    {
        _companyService = companyService;
        _session = session;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    public async Task LoadAsync()
    {
        Companies = await _companyService.GetAllCompaniesAsync();
        WireCompanySelectionNotifications();
        UpdateSelectionState();
    }

    partial void OnCompaniesChanged(List<Company> value) => WireCompanySelectionNotifications();

    partial void OnAreAllCompaniesSelectedChanged(bool? value)
    {
        if (_updatingSelection || !value.HasValue) return;
        foreach (var company in Companies)
            company.IsSelected = value.Value;
        UpdateSelectionState();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var company in Companies)
            company.IsSelected = true;
        UpdateSelectionState();
    }

    [RelayCommand]
    private void SelectionChanged() => UpdateSelectionState();

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var company in Companies)
            company.IsSelected = false;
        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        SelectedCount = Companies.Count(c => c.IsSelected);
        _updatingSelection = true;
        AreAllCompaniesSelected = SelectedCount == 0 ? false : SelectedCount == Companies.Count ? true : null;
        _updatingSelection = false;
    }

    private void WireCompanySelectionNotifications()
    {
        foreach (var company in Companies)
        {
            company.PropertyChanged -= CompanySelectionChanged;
            company.PropertyChanged += CompanySelectionChanged;
        }
    }

    private void CompanySelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_updatingSelection && e.PropertyName == nameof(Company.IsSelected))
            UpdateSelectionState();
    }

    [RelayCommand]
    private void AddCompany()
    {
        var vm = App.Services.GetRequiredService<CompanyFormViewModel>();
        vm.PrepareNew();
        ShowCompanyForm(vm);
    }

    [RelayCommand]
    private void EditCompany(Company? company)
    {
        if (company == null) return;
        var vm = App.Services.GetRequiredService<CompanyFormViewModel>();
        vm.LoadFromCompany(company);
        ShowCompanyForm(vm);
    }

    [RelayCommand]
    private async Task DeleteCompanyAsync(Company? company)
    {
        if (company == null) return;

        bool hasData = await _companyService.CompanyHasDataAsync(company.Id);
        if (hasData)
        {
            MessageBox.Show(LocalizationManager.Get("Msg_CompanyHasData"),
                LocalizationManager.Get("Msg_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            string.Format(LocalizationManager.Get("Msg_ConfirmDeleteCompany"), company.Name),
            LocalizationManager.Get("Msg_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            await _companyService.DeleteCompanyAsync(company.Id);
            _notificationService.Success(string.Format(LocalizationManager.Get("Msg_CompanyDeleted") ?? "Company '{0}' deleted successfully.", company.Name));
            await LoadAsync();
            _session.NotifyCompaniesChanged(); // refresh sidebar dropdown
        }
    }

    [RelayCommand]
    private async Task SetActiveCompanyAsync(Company? company)
    {
        if (company == null) return;
        _session.ActiveCompany = company;
        await LoadAsync();
        _notificationService.Success(string.Format(LocalizationManager.Get("Msg_CompanyActivated"), company.Name));
    }

    private async void ShowCompanyForm(CompanyFormViewModel vm)
    {
        var dialog = new Views.Companies.CompanyFormDialog { DataContext = vm };
        if (dialog.ShowDialog() == true)
        {
            await LoadAsync();
            // Tell MainViewModel to refresh the sidebar company dropdown immediately
            _session.NotifyCompaniesChanged();
        }
    }
}
