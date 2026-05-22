using FornixxCRM.Models;

namespace FornixxCRM.Services.Interfaces;

public interface ICompanyService
{
    Task<List<Company>> GetAllCompaniesAsync();
    Task<Company?> GetCompanyAsync(int id);
    Task<Company> AddCompanyAsync(Company company);
    Task UpdateCompanyAsync(Company company);
    Task<bool> DeleteCompanyAsync(int id);
    Task<bool> CompanyHasDataAsync(int id);
}
