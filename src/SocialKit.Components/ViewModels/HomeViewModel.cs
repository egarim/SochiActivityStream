using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the home/landing page (unauthenticated users).
/// </summary>
public class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public HomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "Welcome to BlazorBook";
        
        NavigateToLoginCommand = new AsyncDelegateCommand(NavigateToLoginAsync);
        NavigateToSignUpCommand = new AsyncDelegateCommand(NavigateToSignUpAsync);
        
        RegisterCommand(NavigateToLoginCommand);
        RegisterCommand(NavigateToSignUpCommand);
    }

    public IAsyncCommand NavigateToLoginCommand { get; }
    public IAsyncCommand NavigateToSignUpCommand { get; }

    private Task NavigateToLoginAsync() => _navigationService.NavigateAsync("/login");
    private Task NavigateToSignUpAsync() => _navigationService.NavigateAsync("/signup");
}
