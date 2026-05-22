using FornixxCRM.Models;

namespace FornixxCRM.Services.Interfaces;

public interface IProductService
{
    Task<List<Product>> GetProductsAsync(int companyId, string? search = null, bool? isActive = null);
    Task<Product?> GetProductAsync(int id);
    Task<Product> AddProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(int id);
}
