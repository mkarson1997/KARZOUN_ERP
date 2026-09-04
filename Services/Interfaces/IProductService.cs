using KarzounERP.Models;

namespace KarzounERP.Services.Interfaces;

public interface IProductService
{
    Task<List<Product>> GetProductsAsync(int companyId, string? search = null, bool? isActive = null);
    Task<Product?> GetProductAsync(int id);
    Task<Product> AddProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(int id);
    Task<Product?> CheckDuplicateAsync(int companyId, int excludeProductId, string name, string arName, string trName, string enName, decimal? weight = null, string? weightUnit = null, ProductType type = ProductType.Physical);
}
