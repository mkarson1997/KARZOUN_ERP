using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;
using System.Windows;

namespace FornixxCRM.ViewModels;

public partial class CompanyViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly ICompanyService _companyService;
    private readonly AppSession _session;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private List<Company> _companies = new();
    [ObservableProperty] private Company? _selectedCompany;

    public CompanyViewModel(ICompanyService companyService, AppSession session,
        NavigationService navigationService)
    {
        _companyService = companyService;
        _session = session;
        _navigationService = navigationService;
    }

    public async Task LoadAsync()
    {
        Companies = await _companyService.GetAllCompaniesAsync();
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
        MessageBox.Show(
            string.Format(LocalizationManager.Get("Msg_CompanyActivated"), company.Name),
            LocalizationManager.Get("Msg_Success"), MessageBoxButton.OK, MessageBoxImage.Information);
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
