namespace Sochi.Navigation.Navigation;

/// <summary>
/// Interface for ViewModels that need to be notified of navigation events.
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// Called when navigating TO this ViewModel.
    /// </summary>
    /// <param name="parameters">The navigation parameters.</param>
    void OnNavigatedTo(INavigationParameters parameters);

    /// <summary>
    /// Called when navigating AWAY from this ViewModel.
    /// </summary>
    /// <param name="parameters">The navigation parameters.</param>
    void OnNavigatedFrom(INavigationParameters parameters);
}
