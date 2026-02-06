namespace Sochi.Navigation.Navigation;

/// <summary>
/// Interface for ViewModels that need to confirm before navigating away.
/// </summary>
public interface IConfirmNavigation
{
    /// <summary>
    /// Determines if navigation can proceed.
    /// </summary>
    /// <param name="parameters">The navigation parameters.</param>
    /// <returns>true if navigation can proceed; otherwise, false.</returns>
    Task<bool> CanNavigateAsync(INavigationParameters parameters);
}
