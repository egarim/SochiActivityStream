using System.Collections.ObjectModel;
using System.Linq;
using ActivityStream.Abstractions;
using Inbox.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel powering the notifications page.
/// </summary>
public class NotificationsViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IInboxNotificationService _inboxService;
    private readonly ICurrentUserService _currentUser;

    private ObservableCollection<InboxItemDto> _notifications = new();
    private bool _isLoading;

    public NotificationsViewModel(
        INavigationService navigationService,
        IInboxNotificationService inboxService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _inboxService = inboxService;
        _currentUser = currentUser;

        Title = "Notifications";

        LoadNotificationsCommand = new AsyncDelegateCommand(LoadNotificationsAsync);
        MarkReadCommand = new AsyncDelegateCommand<InboxItemDto>(MarkReadAsync);
        OpenNotificationCommand = new AsyncDelegateCommand<InboxItemDto>(OpenNotificationAsync);

        RegisterCommand(LoadNotificationsCommand);
        RegisterCommand(MarkReadCommand);
        RegisterCommand(OpenNotificationCommand);
    }

    public ObservableCollection<InboxItemDto> Notifications
    {
        get => _notifications;
        set => SetProperty(ref _notifications, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public IAsyncCommand LoadNotificationsCommand { get; }
    public IAsyncCommand MarkReadCommand { get; }
    public IAsyncCommand OpenNotificationCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (!_currentUser.IsAuthenticated)
        {
            await _navigationService.NavigateAsync("/login");
            return;
        }

        await LoadNotificationsAsync();
    }

    private async Task LoadNotificationsAsync()
    {
        if (_currentUser.ProfileId == null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var query = new InboxQuery
            {
                TenantId = "blazorbook",
                Limit = 50,
                Recipients =
                {
                    new EntityRefDto
                    {
                        Kind = "Profile",
                        Type = "Profile",
                        Id = _currentUser.ProfileId,
                        Display = _currentUser.DisplayName
                    }
                }
            };

            var result = await _inboxService.QueryInboxAsync(query);
            Notifications = new ObservableCollection<InboxItemDto>(result.Items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task MarkReadAsync(InboxItemDto? item)
    {
        if (item?.Id == null)
        {
            return;
        }

        try
        {
            await _inboxService.MarkReadAsync("blazorbook", item.Id);
            item.Status = InboxItemStatus.Read;
            OnPropertyChanged(nameof(Notifications));
        }
        catch
        {
            // Silent failure for now; add toast/telemetry later
        }
    }

    private async Task OpenNotificationAsync(InboxItemDto? item)
    {
        if (item == null)
        {
            return;
        }

        await MarkReadAsync(item);

        var targetRoute = ResolveRoute(item);
        if (!string.IsNullOrWhiteSpace(targetRoute))
        {
            await _navigationService.NavigateAsync(targetRoute);
        }
    }

    private static string? ResolveRoute(InboxItemDto item)
    {
        var target = item.Targets.FirstOrDefault();
        if (target == null)
        {
            return null;
        }

        if (string.Equals(target.Type, "Profile", StringComparison.OrdinalIgnoreCase))
        {
            return $"/profile/{target.Id}";
        }

        if (string.Equals(target.Type, "Conversation", StringComparison.OrdinalIgnoreCase))
        {
            return $"/messages/{target.Id}";
        }

        if (string.Equals(target.Type, "Post", StringComparison.OrdinalIgnoreCase))
        {
            return $"/feed?anchorPostId={target.Id}";
        }

        return null;
    }
}
