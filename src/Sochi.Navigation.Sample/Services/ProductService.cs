using Sochi.Navigation.Sample.Models;

namespace Sochi.Navigation.Sample.Services;

/// <summary>
/// In-memory implementation of <see cref="IProductService"/>.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999.99m, Stock = 10, Description = "High-performance laptop" },
        new Product { Id = 2, Name = "Mouse", Category = "Electronics", Price = 29.99m, Stock = 50, Description = "Wireless mouse" },
        new Product { Id = 3, Name = "Keyboard", Category = "Electronics", Price = 79.99m, Stock = 30, Description = "Mechanical keyboard" },
        new Product { Id = 4, Name = "Monitor", Category = "Electronics", Price = 299.99m, Stock = 15, Description = "27-inch 4K monitor" },
        new Product { Id = 5, Name = "Headphones", Category = "Electronics", Price = 149.99m, Stock = 25, Description = "Noise-cancelling headphones" },
    };

    public async Task<List<Product>> GetAllAsync()
    {
        await Task.Delay(300); // Simulate API call
        return _products.ToList();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await Task.Delay(200);
        return _products.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await Task.Delay(200);
        product.Id = _products.Count > 0 ? _products.Max(p => p.Id) + 1 : 1;
        _products.Add(product);
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        await Task.Delay(200);
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing != null)
        {
            _products.Remove(existing);
            _products.Add(product);
        }
        return product;
    }

    public async Task DeleteAsync(int id)
    {
        await Task.Delay(200);
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product != null)
        {
            _products.Remove(product);
        }
    }
}
