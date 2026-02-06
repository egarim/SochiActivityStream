namespace Sochi.Navigation.Navigation;

/// <summary>
/// Interface for ViewModels that require asynchronous initialization with navigation parameters.
/// </summary>
public interface IInitialize
{
    /// <summary>
    /// Initializes the ViewModel asynchronously with navigation parameters.
    /// </summary>
    /// <param name="parameters">The navigation parameters.</param>
    Task InitializeAsync(INavigationParameters parameters);
}
