using KarzounERP.Models;

namespace KarzounERP.Services.Interfaces;

public interface ICompanyService
{
    Task<List<Company>> GetAllCompaniesAsync();
    Task<Company?> GetCompanyAsync(int id);
    Task<Company> AddCompanyAsync(Company company);
    Task UpdateCompanyAsync(Company company);
    Task<bool> DeleteCompanyAsync(int id);
    Task<bool> CompanyHasDataAsync(int id);
    Task<CompanyLocalizedSetting?> GetLocalizedSettingAsync(int companyId, string languageCode);
    Task SaveLocalizedSettingAsync(CompanyLocalizedSetting setting);
}
