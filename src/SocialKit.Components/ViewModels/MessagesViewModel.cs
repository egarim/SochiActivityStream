using System.Collections.ObjectModel;
using Chat.Abstractions;
using ActivityStream.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for the messages/conversations list page.
/// </summary>
public class MessagesViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IChatService _chatService;
    private readonly ICurrentUserService _currentUser;
    
    private ObservableCollection<ConversationDto> _conversations = new();
    private ConversationDto? _selectedConversation;

    public MessagesViewModel(
        INavigationService navigationService,
        IChatService chatService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _chatService = chatService;
        _currentUser = currentUser;
        
        Title = "Messages";
        
        LoadConversationsCommand = new AsyncDelegateCommand(LoadConversationsAsync);
        OpenConversationCommand = new AsyncDelegateCommand<ConversationDto>(OpenConversationAsync);
        RefreshCommand = new AsyncDelegateCommand(LoadConversationsAsync, () => !IsBusy);
        
        RegisterCommand(RefreshCommand);
    }

    public ObservableCollection<ConversationDto> Conversations
    {
        get => _conversations;
        set => SetProperty(ref _conversations, value);
    }

    public ConversationDto? SelectedConversation
    {
        get => _selectedConversation;
        set => SetProperty(ref _selectedConversation, value);
    }

    public string? CurrentUserId => _currentUser.ProfileId;

    public IAsyncCommand LoadConversationsCommand { get; }
    public IAsyncCommand OpenConversationCommand { get; }
    public IAsyncCommand RefreshCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (!_currentUser.IsAuthenticated)
        {
            await _navigationService.NavigateAsync("/login");
            return;
        }

        await LoadConversationsAsync();
    }

    private async Task LoadConversationsAsync()
    {
        if (_currentUser.ProfileId == null) return;
        
        IsBusy = true;
        try
        {
            var result = await _chatService.GetConversationsAsync(new ConversationQuery
            {
                TenantId = "blazorbook",
                Participant = ActivityStream.Abstractions.EntityRefDto.Profile(
                    _currentUser.ProfileId,
                    _currentUser.DisplayName ?? "User"),
                Limit = 50
            });
            
            Conversations = new ObservableCollection<ConversationDto>(result.Items);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenConversationAsync(ConversationDto? conversation)
    {
        if (conversation?.Id == null) return;
        
        SelectedConversation = conversation;
        await _navigationService.NavigateAsync($"/messages/{conversation.Id}");
    }
}
