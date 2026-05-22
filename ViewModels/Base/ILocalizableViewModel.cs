namespace FornixxCRM.ViewModels.Base;

/// <summary>ViewModels that cache localized strings must refresh when language changes.</summary>
public interface ILocalizableViewModel
{
    void RefreshLocalization();
}
