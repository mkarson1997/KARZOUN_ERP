using KarzounERP.Data;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KarzounERP.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context) => _context = context;

    public async Task<List<Product>> GetProductsAsync(int companyId, string? search = null, bool? isActive = null)
    {
        var query = _context.Products.Include(p => p.LocalizedTexts).Where(p => p.CompanyId == companyId);
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);

        var list = await query.ToListAsync();
        if (!string.IsNullOrWhiteSpace(search))
        {
            list = ProductSearchHelper.SearchProducts(list, search);
        }
        else
        {
            list = list.OrderBy(p => p.Name).ToList();
        }
        return list;
    }

    public async Task<Product?> GetProductAsync(int id) => 
        await _context.Products.Include(p => p.LocalizedTexts).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Product> AddProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateProductAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(int id)
    {
        var p = await _context.Products.FindAsync(id);
        if (p != null) { _context.Products.Remove(p); await _context.SaveChangesAsync(); }
    }

    public async Task<Product?> CheckDuplicateAsync(int companyId, int excludeProductId, string name, string arName, string trName, string enName, decimal? weight = null, string? weightUnit = null, ProductType type = ProductType.Physical)
    {
        var products = await _context.Products
            .Include(p => p.LocalizedTexts)
            .Where(p => p.CompanyId == companyId && p.Id != excludeProductId)
            .ToListAsync();

        var enteredNames = new[] { name, arName, trName, enName }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToList();
        var enteredIdentities = ProductDuplicateHelper.GetEnteredIdentities(name, arName, trName, enName, weight, weightUnit, type);
        var match = ProductDuplicateHelper.FindBestRichMatch(enteredIdentities, enteredNames, weight, weightUnit, type, products, excludeProductId);
        return match.ShouldWarn ? match.ClosestProduct : null;
    }
}
