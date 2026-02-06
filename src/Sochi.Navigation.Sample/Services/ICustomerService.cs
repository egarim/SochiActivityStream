using Sochi.Navigation.Sample.Models;

namespace Sochi.Navigation.Sample.Services;

/// <summary>
/// Service for managing customers.
/// </summary>
public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
}
