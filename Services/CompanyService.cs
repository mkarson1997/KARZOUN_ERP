using FornixxCRM.Data;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FornixxCRM.Services;

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _context;

    public CompanyService(AppDbContext context) => _context = context;

    public async Task<List<Company>> GetAllCompaniesAsync()
        => await _context.Companies.OrderBy(c => c.Name).ToListAsync();

    public async Task<Company?> GetCompanyAsync(int id)
        => await _context.Companies.FindAsync(id);

    public async Task<Company> AddCompanyAsync(Company company)
    {
        company.CreatedAt = DateTime.UtcNow;
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task UpdateCompanyAsync(Company company)
    {
        _context.Companies.Update(company);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteCompanyAsync(int id)
    {
        if (await CompanyHasDataAsync(id)) return false;
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return false;
        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompanyHasDataAsync(int id)
        => await _context.Customers.AnyAsync(c => c.CompanyId == id)
        || await _context.Documents.AnyAsync(d => d.CompanyId == id)
        || await _context.Products.AnyAsync(p => p.CompanyId == id);
}
