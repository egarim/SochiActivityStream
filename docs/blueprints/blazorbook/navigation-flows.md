# BlazorBook Navigation Flows (Sochi.Navigation)

_Last updated: 2026-02-06_

This document captures current and target navigation flows for the Facebook-style BlazorBook experience. All routes and transitions are expressed with Sochi.Navigation concepts (routes, navigation parameters, dialog registrations, command bindings).

## Core Principles
- **Routes in one place**: Author top-level routes inside [src/BlazorBook.Web/Components/Routes.razor](src/BlazorBook.Web/Components/Routes.razor) using `@page` directives on individual page components for static routing.
- **MVVM pages**: Each page inherits `MvvmComponentBase<TViewModel>` and renders the matching SocialKit content component (for example [Feed.razor](src/BlazorBook.Web/Components/Pages/Feed.razor) → `FeedViewModel` + `FeedContent`).
- **Navigation service**: Viewmodels request transitions with `INavigationService.NavigateAsync` and pass strongly-typed parameters via `NavigationParameters`.
- **Dialogs**: Dialog flows use `IDialogService` from Sochi.Navigation; viewmodels register their dialogs during DI setup (to be detailed in the blueprint implementation tasks).

## Route Inventory
| Route | Page component | ViewModel | Primary content | Key navigation interactions |
|-------|----------------|-----------|-----------------|-----------------------------|
| `/` | [Home.razor](src/BlazorBook.Web/Components/Pages/Home.razor) *(redirect to feed planned)* | `HomeViewModel` | `HomeContent` | Should forward authenticated users to `/feed` via `INavigationService.NavigateAsync("/feed")` (implementation item). |
| `/feed` *(target)* | Feed page (currently rendered via `/`) | [FeedViewModel](src/SocialKit.Components/ViewModels/FeedViewModel.cs) | `FeedContent` | `LoadPostsCommand`, `CreatePostCommand`, `LikePostCommand`; future navigation: open profile (`NavigateAsync($"/profile/{post.Author.Id}")`), open post detail dialog. |
| `/profile/{profileId?}` | [Profile.razor](src/BlazorBook.Web/Components/Pages/Profile.razor) | [ProfileViewModel](src/SocialKit.Components/ViewModels/ProfileViewModel.cs) | `ProfileContent` | Uses navigation parameters (`profileId`) to resolve profile; future sub-tabs handled via `/profile/{profileId}/{tab}` with navigation service updates. |
| `/messages` | [Messages.razor](src/BlazorBook.Web/Components/Pages/Messages.razor) | [MessagesViewModel](src/SocialKit.Components/ViewModels/MessagesViewModel.cs) | `MessagesContent` | `OpenConversationCommand` calls `NavigateAsync($"/messages/{conversation.Id}")`; quick compose dialog planned via `DialogService.ShowAsync("ComposeMessage")`. |
| `/messages/{conversationId}` | [Conversation.razor](src/BlazorBook.Web/Components/Pages/Conversation.razor) | [ConversationViewModel](src/SocialKit.Components/ViewModels/ConversationViewModel.cs) | `ConversationContent` | `GoBackCommand` → `/messages`; `SendMessageCommand` stays on route. |
| `/friends` | [Friends.razor](src/BlazorBook.Web/Components/Pages/Friends.razor) | `FriendsViewModel` | `FriendsContent` | Should trigger navigation to `/profile/{profileId}` when selecting a friend entry. |
| `/notifications` *(target)* | new page | new viewmodel | `NotificationList` (existing SocialKit component) | Navigation to content source (post/profile) using `NavigateAsync` with entity-specific paths. |
| `/search` *(target)* | new page | new viewmodel | `SearchResultsContent` | Tab filters update query string via `NavigateAsync("/search", parameters)`; selecting a result jumps to `NavigateAsync(result.Route)`. |

> **Note:** `@page` directives currently point `/` at the feed experience via `Home.razor`. As part of the blueprint execution, we will introduce an explicit `/feed` route and update navigation commands accordingly.

## Dialog & Overlay Flows
| Dialog token | Trigger command | Viewmodel | Notes |
|--------------|-----------------|-----------|-------|
| `PostComposerDialog` *(planned)* | `FeedViewModel.CreatePostCommand` when expanded composer requested | Composer viewmodel (new) | Should reuse SocialKit `PostComposer` content inside dialog host rendered by [DialogHost.razor](src/BlazorBook.Web/Components/Dialogs/DialogHost.razor). |
| `ComposeMessage` *(planned)* | Top nav quick message or `MessagesViewModel` | `ComposeMessageViewModel` (new) | Upon submit, navigate to `/messages/{conversationId}`. |
| `NotificationSettings` *(planned)* | Notifications overflow command | `NotificationSettingsViewModel` (new) | Updates notification preferences; no route change. |

Register dialogs within `Program.cs` when implementing: `services.AddNavigationDialog<PostComposerDialog, PostComposerViewModel>("PostComposerDialog");` etc.

## Command Linking
- Commands in viewmodels should be registered via `RegisterCommand` so Sochi.Navigation propagates `CanExecute` state to the UI controls (`CommandButton`, `CommandLink`).
- Use navigation-aware commands for sidebar links: e.g. in `LeftSidebar.razor`, bind `CommandLink` to `NavigationService.NavigateAsync("/notifications")` to ensure consistent history handling.
- When adding new commands, prefer `AsyncDelegateCommand` from Sochi.Navigation to ensure busy state integration.

## Navigation Parameters
| Parameter | Source | Consumed by | Usage |
|-----------|--------|-------------|-------|
| `profileId` | `NavigateAsync("/profile/{profileId}")` from feed posts, friends list | `ProfileViewModel` | Determines profile to load; default falls back to current user. |
| `conversationId` | Messages list or notifications deep links | `ConversationViewModel` | Loads conversation/messages. |
| `anchorPostId` *(planned)* | Notifications or search results linking into feed | Feed route (new) | Feed viewmodel scrolls to highlight targeted post. |
| `searchQuery` *(planned)* | Global search box (TopNav) | Search results viewmodel | Query string value informs initial load.

## Navigation Map Asset
- Create `/docs/wireframes/blazorbook/navigation-map.drawio` capturing:
  - Primary routes and modal dialogs.
  - Entry points triggered from top nav, feed cards, sidebar links.
  - Command IDs and resulting navigation service calls.
  - Authentication guards (e.g., redirect unauthenticated users to `/login`).

## Redirect & Guard Rules (To Implement)
1. If `ICurrentUserService.IsAuthenticated` is false, `/feed`, `/profile`, `/messages`, `/friends`, `/notifications` should redirect to `/login` using `NavigationService.NavigateAsync("/login")` inside respective `InitializeAsync` methods.
2. After successful login (`LoginViewModel`), navigate to `/feed`.
3. Logout route should call `NavigationService.NavigateAsync("/login")` once the session clears.

## Next Steps
- Formalize missing routes (`/feed`, `/notifications`, `/search`) and update `Routes.razor` + page shells.
- Update viewmodels to emit navigation commands for new flows (e.g., feed post → profile, notification item → post).
- Generate Draw.io navigation map once routes are finalized.
