using Identity.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;
using Search.Abstractions;
using Microsoft.Extensions.Logging;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the sign-up page.
/// </summary>
public class SignUpViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISearchIndexer _searchIndexer;
    private readonly ILogger<SignUpViewModel> _logger;
    
    private string _displayName = string.Empty;
    private string _handle = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;

    public SignUpViewModel(
        INavigationService navigationService,
        IAuthService authService,
        ICurrentUserService currentUser,
        ISearchIndexer searchIndexer,
        ILogger<SignUpViewModel> logger)
    {
        _navigationService = navigationService;
        _authService = authService;
        _currentUser = currentUser;
        _searchIndexer = searchIndexer;
        _logger = logger;
        
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
            _logger.LogInformation("Starting signup for user: {Username} ({Email})", Handle, Email);
            
            var result = await _authService.SignUpAsync("blazorbook", new SignUpRequest
            {
                Email = Email,
                Password = Password,
                Username = Handle,
                DisplayName = DisplayName
            });
            
            _logger.LogInformation("User created successfully. ProfileId: {ProfileId}, UserId: {UserId}", 
                result.Profile.Id, result.User.Id);
            
            // Index the new profile for search
            try
            {
                _logger.LogInformation("Starting search indexing for profile: {ProfileId} ({DisplayName})", 
                    result.Profile.Id, result.Profile.DisplayName);
                
                var doc = new SearchDocument
                {
                    Id = result.Profile.Id!,
                    TenantId = "blazorbook",
                    DocumentType = "Profile",
                    TextFields = new Dictionary<string, string>
                    {
                        ["displayName"] = result.Profile.DisplayName ?? "",
                        ["handle"] = result.Profile.Handle ?? ""
                    },
                    KeywordFields = new Dictionary<string, List<string>>
                    {
                        ["profileId"] = new() { result.Profile.Id! }
                    },
                    DateFields = new Dictionary<string, DateTimeOffset>
                    {
                        ["createdAt"] = result.Profile.CreatedAt
                    },
                    Boost = 1.0,
                    SourceEntity = result.Profile
                };

                _logger.LogDebug("Search document created: {@Document}", new { 
                    doc.Id, doc.DocumentType, doc.TenantId, 
                    TextFields = doc.TextFields, 
                    KeywordFields = doc.KeywordFields 
                });

                await _searchIndexer.IndexAsync(doc);
                
                _logger.LogInformation("Profile indexed successfully in search: {ProfileId}", result.Profile.Id);
            }
            catch (Exception indexEx)
            {
                _logger.LogError(indexEx, "FAILED to index profile {ProfileId} for search. Search will not find this user!", 
                    result.Profile.Id);
                // Don't fail signup if indexing fails
            }
            
            _logger.LogInformation("Signing in user: {ProfileId}", result.Profile.Id);
            await _currentUser.SignInAsync(result.Profile, result.User.Id!);
            
            _logger.LogInformation("Navigating to /feed");
            await _navigationService.NavigateAsync("/feed");
        }
        catch (IdentityValidationException ex)
        {
            _logger.LogWarning(ex, "Signup validation failed for {Username}", Handle);
            ErrorMessage = ex.Errors.FirstOrDefault()?.Message ?? "Sign up failed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during signup for {Username}", Handle);
            ErrorMessage = "An error occurred. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
