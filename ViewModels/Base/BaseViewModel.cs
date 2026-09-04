using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace KarzounERP.ViewModels.Base;

public partial class BaseViewModel : ObservableObject
{
    public event Action<string>? RequestFocus;

    protected void RaiseRequestFocus(string fieldName)
    {
        RequestFocus?.Invoke(fieldName);
    }

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
