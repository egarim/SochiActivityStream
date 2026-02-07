using System.Collections.ObjectModel;
using Chat.Abstractions;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using SocialKit.Components.Abstractions;

namespace SocialKit.Components.ViewModels;

/// <summary>
/// ViewModel for a single conversation/chat page.
/// </summary>
public class ConversationViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IChatService _chatService;
    private readonly ICurrentUserService _currentUser;
    
    private ConversationDto? _conversation;
    private ObservableCollection<MessageDto> _messages = new();
    private string _newMessageText = string.Empty;

    public ConversationViewModel(
        INavigationService navigationService,
        IChatService chatService,
        ICurrentUserService currentUser)
    {
        _navigationService = navigationService;
        _chatService = chatService;
        _currentUser = currentUser;
        
        Title = "Conversation";
        
        LoadMessagesCommand = new AsyncDelegateCommand(LoadMessagesAsync);
        SendMessageCommand = new AsyncDelegateCommand(SendMessageAsync, CanSendMessage);
        GoBackCommand = new AsyncDelegateCommand(() => _navigationService.NavigateAsync("/messages"));
        
        RegisterCommand(SendMessageCommand);
    }

    public ConversationDto? Conversation
    {
        get => _conversation;
        set
        {
            if (SetProperty(ref _conversation, value))
            {
                Title = GetConversationTitle();
            }
        }
    }

    public ObservableCollection<MessageDto> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    public string NewMessageText
    {
        get => _newMessageText;
        set
        {
            if (SetProperty(ref _newMessageText, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public string? CurrentUserId => _currentUser.ProfileId;

    public IAsyncCommand LoadMessagesCommand { get; }
    public IAsyncCommand SendMessageCommand { get; }
    public IAsyncCommand GoBackCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (!_currentUser.IsAuthenticated)
        {
            await _navigationService.NavigateAsync("/login");
            return;
        }

        if (parameters.TryGetValue<string>("conversationId", out var conversationId))
        {
            await LoadConversationAsync(conversationId);
        }
    }

    private string GetConversationTitle()
    {
        if (Conversation == null) return "Conversation";
        
        if (!string.IsNullOrEmpty(Conversation.Title))
            return Conversation.Title;
        
        var otherParticipant = Conversation.Participants
            .FirstOrDefault(p => p.Profile.Id != _currentUser.ProfileId);
        
        return otherParticipant?.Profile.DisplayName ?? "Chat";
    }

    private async Task LoadConversationAsync(string conversationId)
    {
        IsBusy = true;
        try
        {
            var viewer = Chat.Abstractions.EntityRefDto.Profile(
                _currentUser.ProfileId!,
                _currentUser.DisplayName ?? "User");
            Conversation = await _chatService.GetConversationAsync("blazorbook", conversationId, viewer);
            await LoadMessagesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadMessagesAsync()
    {
        if (Conversation?.Id == null || _currentUser.ProfileId == null) return;
        
        var result = await _chatService.GetMessagesAsync(new MessageQuery
        {
            TenantId = "blazorbook",
            ConversationId = Conversation.Id,
            Viewer = Chat.Abstractions.EntityRefDto.Profile(
                _currentUser.ProfileId,
                _currentUser.DisplayName ?? "User"),
            Limit = 100
        });
        
        var messages = result.Items.AsEnumerable().Reverse().ToList();
        Messages = new ObservableCollection<MessageDto>(messages);
    }

    private bool CanSendMessage() => 
        !IsBusy && 
        !string.IsNullOrWhiteSpace(NewMessageText) &&
        Conversation != null &&
        _currentUser.IsAuthenticated;

    private async Task SendMessageAsync()
    {
        if (Conversation?.Id == null || _currentUser.ProfileId == null) return;
        
        IsBusy = true;
        try
        {
            var message = await _chatService.SendMessageAsync(new SendMessageRequest
            {
                TenantId = "blazorbook",
                ConversationId = Conversation.Id,
                Sender = Chat.Abstractions.EntityRefDto.Profile(
                    _currentUser.ProfileId,
                    _currentUser.DisplayName ?? "User"),
                Body = NewMessageText
            });
            
            Messages.Add(message);
            NewMessageText = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
