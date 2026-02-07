# BlazorBook Component Inventory

_Last updated: 2026-02-06_

This catalog tracks reusable assets from SocialKit and BlazorBook.Web that fuel the Facebook-like experience. It highlights what exists, how it maps to planned wireframes, and identifies parity gaps to address.

## Existing SocialKit Components
| Area | Artifact | Location | Notes |
|------|----------|----------|-------|
| Feed | FeedContent | [src/SocialKit.Components/Components/Pages/FeedContent.razor](src/SocialKit.Components/Components/Pages/FeedContent.razor) | Renders feed stream, binds to `FeedViewModel` commands (`LoadPostsCommand`, `CreatePostCommand`, etc.). |
| Feed | CreatePost | [src/SocialKit.Components/Components/Posts/CreatePost.razor](src/SocialKit.Components/Components/Posts/CreatePost.razor) | Inline composer surface used inside feed; candidate for dialog reuse. |
| Feed | PostCard | [src/SocialKit.Components/Components/Posts/PostCard.razor](src/SocialKit.Components/Components/Posts/PostCard.razor) | Displays individual post with reactions; needs enhancement for story context, media carousel. |
| Profile | ProfileContent | [src/SocialKit.Components/Components/Pages/ProfileContent.razor](src/SocialKit.Components/Components/Pages/ProfileContent.razor) | Leverages `ProfileViewModel`; tabs and cover header extensions required for parity. |
| Friends | FriendsContent | [src/SocialKit.Components/Components/Pages/FriendsContent.razor](src/SocialKit.Components/Components/Pages/FriendsContent.razor) | Uses `FriendsViewModel`; integrates `FriendCard` list. |
| Messaging | MessagesContent | [src/SocialKit.Components/Components/Pages/MessagesContent.razor](src/SocialKit.Components/Components/Pages/MessagesContent.razor) | Conversation list; triggers `OpenConversationCommand`. |
| Messaging | ConversationContent | [src/SocialKit.Components/Components/Pages/ConversationContent.razor](src/SocialKit.Components/Components/Pages/ConversationContent.razor) | Conversation detail; consumes `ConversationViewModel` commands (`SendMessageCommand`, `GoBackCommand`). |
| Messaging | ConversationItem | [src/SocialKit.Components/Components/Chat/ConversationItem.razor](src/SocialKit.Components/Components/Chat/ConversationItem.razor) | List item UI for a conversation. |
| Messaging | MessageBubble | [src/SocialKit.Components/Components/Chat/MessageBubble.razor](src/SocialKit.Components/Components/Chat/MessageBubble.razor) | Individual message bubble with sender alignment. |
| Auth | LoginContent | [src/SocialKit.Components/Components/Pages/LoginContent.razor](src/SocialKit.Components/Components/Pages/LoginContent.razor) | Backed by `LoginViewModel`; navigates to `/signup` / `/feed`. |
| Auth | SignUpContent | [src/SocialKit.Components/Components/Pages/SignUpContent.razor](src/SocialKit.Components/Components/Pages/SignUpContent.razor) | Registration flow. |
| Common | Avatar | [src/SocialKit.Components/Components/Common/Avatar.razor](src/SocialKit.Components/Components/Common/Avatar.razor) | Configurable avatar rendering. |
| Common | FriendCard | [src/SocialKit.Components/Components/Common/FriendCard.razor](src/SocialKit.Components/Components/Common/FriendCard.razor) | Displays friend summary; lacks presence indicator. |
| Common | Skeleton | [src/SocialKit.Components/Components/Common/Skeleton.razor](src/SocialKit.Components/Components/Common/Skeleton.razor) | Loading placeholder; types defined in `SkeletonType`. |
| Dialogs | ConfirmDialog | [src/SocialKit.Components/Components/Dialogs/ConfirmDialog.razor](src/SocialKit.Components/Components/Dialogs/ConfirmDialog.razor) | Generic confirm modal bridged through Sochi.Navigation dialogs. |

## Existing ViewModels
| ViewModel | Location | Responsibilities |
|-----------|----------|------------------|
| FeedViewModel | [src/SocialKit.Components/ViewModels/FeedViewModel.cs](src/SocialKit.Components/ViewModels/FeedViewModel.cs) | Drive feed posts lifecycle and composer actions; uses `IFeedService`. |
| ProfileViewModel | [src/SocialKit.Components/ViewModels/ProfileViewModel.cs](src/SocialKit.Components/ViewModels/ProfileViewModel.cs) | Load profile details, follow/unfollow logic, posts query. |
| MessagesViewModel | [src/SocialKit.Components/ViewModels/MessagesViewModel.cs](src/SocialKit.Components/ViewModels/MessagesViewModel.cs) | Fetch conversation list, navigate to conversation route. |
| ConversationViewModel | [src/SocialKit.Components/ViewModels/ConversationViewModel.cs](src/SocialKit.Components/ViewModels/ConversationViewModel.cs) | Manage message stream, send messages, navigation back. |
| FriendsViewModel | [src/SocialKit.Components/ViewModels/FriendsViewModel.cs](src/SocialKit.Components/ViewModels/FriendsViewModel.cs) | Friend recommendations & list actions. |
| HomeViewModel | [src/SocialKit.Components/ViewModels/HomeViewModel.cs](src/SocialKit.Components/ViewModels/HomeViewModel.cs) | Landing experience; currently simple. |
| LoginViewModel | [src/SocialKit.Components/ViewModels/LoginViewModel.cs](src/SocialKit.Components/ViewModels/LoginViewModel.cs) | Auth command set; redirects post-login. |
| SignUpViewModel | [src/SocialKit.Components/ViewModels/SignUpViewModel.cs](src/SocialKit.Components/ViewModels/SignUpViewModel.cs) | Registration flow and navigation to login. |

## Services & Abstractions
| Service | Location | Purpose |
|---------|----------|---------|
| FeedService | [src/SocialKit.Components/Services/FeedService.cs](src/SocialKit.Components/Services/FeedService.cs) | Adapter to Content/Activity services for feed queries. |
| ICurrentUserService | [src/BlazorBook.Web/Services/CurrentUserService.cs](src/BlazorBook.Web/Services/CurrentUserService.cs) | Tracks session data used by viewmodels (avatar, profile ID). |

## Identified Gaps (Parity Backlog)
| Gap | Description | Proposed asset |
|-----|-------------|----------------|
| Stories rail | Horizontal stories carousel at top of feed. | `StoriesRail.razor` + `StoriesRailViewModel` (extend SocialKit). |
| Cover photo header | Profile banner with action buttons. | Enhance `ProfileContent` or introduce `ProfileHeader` component. |
| Presence list | Right-sidebar online contacts with quick chat entry. | `PresenceList.razor` under `Components/Common`, powered by Realtime service. |
| Notification list | Grouped notifications experience. | `NotificationsContent.razor` + `NotificationsViewModel`. |
| Search facets | Filter chips for people/posts/groups. | `SearchFilterBar.razor`; new viewmodel to manage query parameters. |
| Sponsored slot | Ad placeholder in right sidebar. | `SponsoredCard.razor` with configuration hooks. |
| Quick actions tray | Top nav shortcuts (videos, marketplace). | Extend `TopNav.razor` with `NavQuickActions` component. |

Create issues referencing this table once priorities are agreed so each gap evolves into an implementation work item.
