using Sochi.Navigation.Sample.Models;

namespace Sochi.Navigation.Sample.Services;

/// <summary>
/// Service for managing products.
/// </summary>
public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(int id);
}
