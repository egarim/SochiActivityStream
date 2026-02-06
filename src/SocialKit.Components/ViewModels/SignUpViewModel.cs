using Identity.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the sign-up page.
/// </summary>
public class SignUpViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;
    
    private string _displayName = string.Empty;
    private string _handle = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;

    public SignUpViewModel(
        INavigationService navigationService,
        IAuthService authService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _authService = authService;
        _currentUser = currentUser;
        
        Title = "Create Account";
        
        SignUpCommand = new AsyncDelegateCommand(SignUpAsync, CanSignUp);
        NavigateToLoginCommand = new AsyncDelegateCommand(() => _navigationService.NavigateAsync("/login"));
        
        RegisterCommand(SignUpCommand);
    }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (SetProperty(ref _displayName, value))
            {
                ErrorMessage = null;
                if (string.IsNullOrEmpty(Handle))
                {
                    Handle = value.ToLowerInvariant().Replace(" ", "");
                }
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public string Handle
    {
        get => _handle;
        set
        {
            if (SetProperty(ref _handle, value))
            {
                ErrorMessage = null;
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ErrorMessage = null;
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ErrorMessage = null;
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public IAsyncCommand SignUpCommand { get; }
    public IAsyncCommand NavigateToLoginCommand { get; }

    private bool CanSignUp() => 
        !IsBusy && 
        !string.IsNullOrWhiteSpace(DisplayName) &&
        !string.IsNullOrWhiteSpace(Handle) &&
        !string.IsNullOrWhiteSpace(Email) && 
        !string.IsNullOrWhiteSpace(Password) &&
        Password.Length >= 6;

    private async Task SignUpAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        
        try
        {
            var result = await _authService.SignUpAsync("blazorbook", new SignUpRequest
            {
                Email = Email,
                Password = Password,
                Username = Handle,
                DisplayName = DisplayName
            });
            
            await _currentUser.SignInAsync(result.Profile, result.User.Id!);
            await _navigationService.NavigateAsync("/");
        }
        catch (IdentityValidationException ex)
        {
            ErrorMessage = ex.Errors.FirstOrDefault()?.Message ?? "Sign up failed";
        }
        catch (Exception)
        {
            ErrorMessage = "An error occurred. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
