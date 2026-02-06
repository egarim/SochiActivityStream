using Sochi.Navigation.Sample.Models;

namespace Sochi.Navigation.Sample.Services;

/// <summary>
/// In-memory implementation of <see cref="ICustomerService"/>.
/// </summary>
public sealed class CustomerService : ICustomerService
{
    private readonly List<Customer> _customers = new()
    {
        new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "555-0001", Address = "123 Main St", City = "Phoenix" },
        new Customer { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", Phone = "555-0002", Address = "456 Oak Ave", City = "Tucson" },
        new Customer { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob@example.com", Phone = "555-0003", Address = "789 Pine Rd", City = "Mesa" },
    };

    public async Task<List<Customer>> GetAllAsync()
    {
        await Task.Delay(300);
        return _customers.ToList();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        await Task.Delay(200);
        return _customers.FirstOrDefault(c => c.Id == id);
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        await Task.Delay(200);
        customer.Id = _customers.Count > 0 ? _customers.Max(c => c.Id) + 1 : 1;
        _customers.Add(customer);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        await Task.Delay(200);
        var existing = _customers.FirstOrDefault(c => c.Id == customer.Id);
        if (existing != null)
        {
            _customers.Remove(existing);
            _customers.Add(customer);
        }
        return customer;
    }

    public async Task DeleteAsync(int id)
    {
        await Task.Delay(200);
        var customer = _customers.FirstOrDefault(c => c.Id == id);
        if (customer != null)
        {
            _customers.Remove(customer);
        }
    }
}
