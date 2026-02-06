using System.Collections.ObjectModel;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Sample.Models;
using Sochi.Navigation.Sample.Services;

namespace Sochi.Navigation.Sample.ViewModels.Customers;

/// <summary>
/// ViewModel for the customer list page.
/// </summary>
public sealed class CustomerListViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly ICustomerService _customerService;
    private ObservableCollection<Customer> _customers = new();
    private Customer? _selectedCustomer;

    public CustomerListViewModel(
        INavigationService navigationService,
        ICustomerService customerService)
    {
        _navigationService = navigationService;
        _customerService = customerService;

        Title = "Customers";

        LoadCustomersCommand = new AsyncDelegateCommand(LoadCustomersAsync);
        RefreshCommand = new AsyncDelegateCommand(LoadCustomersAsync, () => !IsBusy);
        NavigateToDetailCommand = new AsyncDelegateCommand<Customer>(NavigateToDetailAsync, c => !IsBusy && c != null);

        RegisterCommand(RefreshCommand);
        RegisterCommand(NavigateToDetailCommand);
    }

    #region Properties

    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    #endregion

    #region Commands

    public IAsyncCommand LoadCustomersCommand { get; }
    public IAsyncCommand RefreshCommand { get; }
    public IAsyncCommand NavigateToDetailCommand { get; }

    #endregion

    #region IInitialize

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        await LoadCustomersAsync();
    }

    #endregion

    #region Command Implementations

    private async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            var customers = await _customerService.GetAllAsync();
            Customers = new ObservableCollection<Customer>(customers);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NavigateToDetailAsync(Customer? customer)
    {
        if (customer == null) return;

        var parameters = new NavigationParameters();
        parameters.Add("CustomerId", customer.Id);

        await _navigationService.NavigateAsync("/customer-detail", parameters);
    }

    #endregion
}
