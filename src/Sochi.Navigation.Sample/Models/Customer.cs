namespace Sochi.Navigation.Sample.Models;

/// <summary>
/// Represents a customer entity.
/// </summary>
public sealed class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public string FullName => $"{FirstName} {LastName}";
}
