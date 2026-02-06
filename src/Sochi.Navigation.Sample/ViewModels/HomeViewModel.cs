using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;

namespace Sochi.Navigation.Sample.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public HomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        
        Title = "Home";
        
        NavigateToProductsCommand = new AsyncDelegateCommand(NavigateToProductsAsync, CanNavigate);
        NavigateToCustomersCommand = new AsyncDelegateCommand(NavigateToCustomersAsync, CanNavigate);
        
        RegisterCommand(NavigateToProductsCommand);
        RegisterCommand(NavigateToCustomersCommand);
    }

    public IAsyncCommand NavigateToProductsCommand { get; }
    public IAsyncCommand NavigateToCustomersCommand { get; }

    private async Task NavigateToProductsAsync()
    {
        await _navigationService.NavigateAsync("/products");
    }

    private async Task NavigateToCustomersAsync()
    {
        await _navigationService.NavigateAsync("/customers");
    }

    private bool CanNavigate() => !IsBusy;
}
