using FornixxCRM.Data;
using FornixxCRM.Models;
using FornixxCRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FornixxCRM.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context) => _context = context;

    public async Task<List<Product>> GetProductsAsync(int companyId, string? search = null, bool? isActive = null)
    {
        var query = _context.Products.Where(p => p.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s));
        }
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Product?> GetProductAsync(int id) => await _context.Products.FindAsync(id);

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
}
