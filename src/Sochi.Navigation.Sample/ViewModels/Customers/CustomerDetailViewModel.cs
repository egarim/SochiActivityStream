using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Sample.Models;
using Sochi.Navigation.Sample.Services;

namespace Sochi.Navigation.Sample.ViewModels.Customers;

/// <summary>
/// ViewModel for the customer detail page.
/// </summary>
public sealed class CustomerDetailViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly ICustomerService _customerService;
    private Customer? _customer;

    public CustomerDetailViewModel(
        INavigationService navigationService,
        ICustomerService customerService)
    {
        _navigationService = navigationService;
        _customerService = customerService;

        Title = "Customer Details";

        GoBackCommand = new AsyncDelegateCommand(GoBackAsync);

        RegisterCommand(GoBackCommand);
    }

    #region Properties

    public Customer? Customer
    {
        get => _customer;
        set
        {
            if (SetProperty(ref _customer, value))
            {
                Title = _customer != null ? $"Customer: {_customer.FullName}" : "Customer Details";
            }
        }
    }

    #endregion

    #region Commands

    public IAsyncCommand GoBackCommand { get; }

    #endregion

    #region IInitialize

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (parameters.TryGetValue<int>("CustomerId", out var customerId))
        {
            IsBusy = true;
            try
            {
                Customer = await _customerService.GetByIdAsync(customerId);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    #endregion

    #region Command Implementations

    private async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
    }

    #endregion
}
