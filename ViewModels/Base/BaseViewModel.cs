using CommunityToolkit.Mvvm.ComponentModel;

namespace FornixxCRM.ViewModels.Base;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    protected void SetBusy(bool busy, string message = "")
    {
        IsBusy = busy;
        StatusMessage = message;
    }
}
