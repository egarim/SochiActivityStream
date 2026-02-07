using Identity.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the login page.
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IProfileService _profileService;
    private readonly ICurrentUserService _currentUser;
    
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;

    public LoginViewModel(
        INavigationService navigationService,
        IAuthService authService,
        IProfileService profileService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _authService = authService;
        _profileService = profileService;
        _currentUser = currentUser;
        
        Title = "Log In";
        
        LoginCommand = new AsyncDelegateCommand(LoginAsync, CanLogin);
        NavigateToSignUpCommand = new AsyncDelegateCommand(() => _navigationService.NavigateAsync("/signup"));
        
        RegisterCommand(LoginCommand);
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

    public IAsyncCommand LoginCommand { get; }
    public IAsyncCommand NavigateToSignUpCommand { get; }

    private bool CanLogin() => 
        !IsBusy && 
        !string.IsNullOrWhiteSpace(Email) && 
        !string.IsNullOrWhiteSpace(Password);

    private async Task LoginAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        
        try
        {
            var session = await _authService.SignInAsync("blazorbook", new SignInRequest
            {
                Login = Email,
                Password = Password
            });
            
            // Get profile for the session
            var profiles = await GetProfileForSession(session);
            if (profiles != null)
            {
                await _currentUser.SignInAsync(profiles, session.UserId);
                await _navigationService.NavigateAsync("/feed");
            }
        }
        catch (IdentityValidationException ex)
        {
            ErrorMessage = ex.Errors.FirstOrDefault()?.Message ?? "Invalid credentials";
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

    private async Task<ProfileDto?> GetProfileForSession(SessionDto session)
    {
        var profiles = await _profileService.GetProfilesForUserAsync("blazorbook", session.UserId);
        return profiles.FirstOrDefault();
    }
}
