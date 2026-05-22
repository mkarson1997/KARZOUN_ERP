using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FornixxCRM.Helpers;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels.Base;

namespace FornixxCRM.ViewModels;

public partial class CustomerDetailViewModel : BaseViewModel, ILoadableViewModel
{
    private readonly ICustomerService _customerService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private Customer? _customer;
    [ObservableProperty] private List<SalesDocument> _documents = new();
    [ObservableProperty] private List<CustomerNote> _notesHistory = new();
    [ObservableProperty] private string _newNoteText = string.Empty;
    [ObservableProperty] private int _customerId;

    public CustomerDetailViewModel(ICustomerService customerService, NavigationService navigationService)
    {
        _customerService = customerService;
        _navigationService = navigationService;
    }

    partial void OnCustomerIdChanged(int value)
    {
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (CustomerId <= 0) return;
        SetBusy(true, LocalizationManager.Get("Msg_LoadingCustomerDetails"));
        try
        {
            Customer = await _customerService.GetCustomerAsync(CustomerId);
            Documents = await _customerService.GetCustomerDocumentsAsync(CustomerId);
            NotesHistory = await _customerService.GetNotesHistoryAsync(CustomerId);
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<CustomerViewModel>();

    [RelayCommand]
    private async Task AddNoteAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNoteText)) return;
        var note = new CustomerNote
        {
            CustomerId = CustomerId,
            NoteText = NewNoteText.Trim()
        };
        await _customerService.AddNoteAsync(note);
        NewNoteText = string.Empty;
        NotesHistory = await _customerService.GetNotesHistoryAsync(CustomerId);
    }

    [RelayCommand]
    private async Task DeleteNoteAsync(CustomerNote? note)
    {
        if (note == null) return;
        var confirmResult = System.Windows.MessageBox.Show(
            LocalizationManager.Get("Msg_DeleteConfirm"),
            LocalizationManager.Get("Msg_DeleteTitle"),
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirmResult == System.Windows.MessageBoxResult.Yes)
        {
            await _customerService.DeleteNoteAsync(note.Id);
            NotesHistory = await _customerService.GetNotesHistoryAsync(CustomerId);
        }
    }
}
