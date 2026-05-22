using FornixxCRM.Models;

namespace FornixxCRM.Helpers;

public class AppSession
{
    private Company? _activeCompany;

    public event EventHandler<Company?>? ActiveCompanyChanged;

    // Fired whenever the company list changes (add/edit/delete) so MainViewModel
    // can refresh the sidebar dropdown without needing a direct reference.
    public event EventHandler? CompaniesChanged;

    public void NotifyCompaniesChanged() => CompaniesChanged?.Invoke(this, EventArgs.Empty);

    public Company? ActiveCompany
    {
        get => _activeCompany;
        set
        {
            _activeCompany = value;
            ActiveCompanyChanged?.Invoke(this, value);
        }
    }

    public int ActiveCompanyId => _activeCompany?.Id ?? 0;
    public bool HasActiveCompany => _activeCompany != null;
    public string ActiveCompanyName => _activeCompany?.Name ?? LocalizationManager.Get("Msg_NoCompany");
    public string ActiveCompanyCurrency => _activeCompany?.Currency ?? "USD";
}
