# BlazorBook — Facebook-Like Experience Blueprint

_Last updated: 2026-02-06_

## 1. Goals
- Deliver a Facebook-style social experience while reusing SocialKit components whenever parity gaps do not exist.
- Keep routing, dialogs, and viewmodel orchestration aligned with Sochi.Navigation patterns.
- Provide a blueprint (navigation map, component hierarchy, wireframes) so engineering workstreams can proceed in parallel.

## 2. Scope & Non-Goals
**In-scope**
- Feed, profile, messaging, notifications, and search entry points.
- Layout shell refinements (top nav, dual sidebars, responsive breakpoints).
- Dialog and inline composer flows powered by Sochi.Navigation services.
- Component gap analysis + backlog items for missing building blocks.

**Out of scope** (v1)
- Backend persistence (in-memory stores remain acceptable for demos).
- Mobile-specific layouts (document responsive breakpoints, no full redesign).
- Monetization surfaces (ads, marketplace) beyond placeholder slots.

## 3. Personas & Journeys
- **Daily User**: Scrolls feed, reacts/comments, checks notifications, posts updates.
- **Content Creator**: Publishes longer posts, manages media, reviews engagement.
- **Connector**: Uses messaging/search heavily to maintain relationships.

Document detailed user stories, success criteria, and exclusions in `/docs/blueprints/blazorbook/facebook-ui/user-stories.md` (to be produced).

## 4. Navigation Blueprint (Sochi.Navigation)
- Author routes in `Components/Routes.razor` using `Sochi.Navigation` route descriptors.
- Each page `MvvmComponentBase<TViewModel>` hosts a SocialKit content component.
- Dialog flows invoke `IDialogService` (`DialogService.ShowAsync`) with viewmodels registered via `RegisterDialog<TDialog, TViewModel>` in `Program.cs`.
- Primary routes:
  1. `/` → Feed page (default route)
  2. `/profile/{id}` → Profile page (supports current user + others)
  3. `/messages` → Messaging hub with thread list + conversation dialog
  4. `/notifications` → Notifications digest with inline actions
  5. `/search` → Search results surface, deep links into entities via navigation service
- Create navigation map asset (`/docs/wireframes/blazorbook/navigation-map.drawio`) showing route transitions, dialogs, and command triggers (include command IDs, dialog tokens, and navigation service usage).

## 5. Layout Shell Strategy
- Source: `Components/Layout/MainLayout.razor`, `TopNav.razor`, `LeftSidebar.razor`, `RightSidebar.razor`, `wwwroot/app.css`.
- Tasks:
  - Align top nav with Facebook-style global search, quick links, user menu.
  - Left sidebar: dynamic shortcuts (NavigationService commands) + groups/events placeholders.
  - Right sidebar: contacts (presence list), sponsored slot placeholder.
  - Responsive behavior: collapse sidebars < 1024px, transform top nav menu into overflow menu.

## 6. Page Blueprints
For each surface create a wireframe (low → mid fidelity) stored under `/docs/wireframes/blazorbook/{page}-{fidelity}.png`.

1. **Feed**
   - SocialKit `FeedContent` as core; extend with stories rail (new component if gap identified).
   - Composer: reuse `PostComposer` with inline media attachments.
   - Engagement bar leveraging `ReactionSummary`, `CommentList` components.

2. **Profile**
   - SocialKit `ProfileContent`; add cover photo band + action buttons.
   - Tabs for Posts, About, Friends, Photos using `NavigationService.NavigateAsync` to subroutes (`/profile/{id}/posts`, etc.).

3. **Messaging**
   - Main view uses `MessagesContent`; left rail thread list, right pane active conversation.
   - Message dialog for quick compose via `DialogService` when invoked from top nav.

4. **Notifications**
   - Use `NotificationList` with grouping by day; quick action buttons (confirm friend request, mark as read).
   - Settings dialog accessible via overflow menu.

5. **Search**
   - `SearchResultsContent` with faceting bar (entities: people, posts, groups).
   - Integrate navigation commands to jump into feed filters or profile pages.

## 7. Component Inventory & Gap Analysis
Produce `/docs/blueprints/blazorbook/component-inventory.md` enumerating SocialKit assets:
- Map existing components (FeedContent, ProfileContent, MessagesContent, NotificationList, SearchResultsContent, PostComposer, ReactionSummary, CommentList, CommandButton, Avatar, Skeleton loaders).
- Record missing items (StoriesRail, CoverPhotoHeader, PresenceList, FacetFilterBar, AdsPlaceholder).
- For each gap add backlog entry (link to issue) detailing:
  - Proposed component name + namespace under `SocialKit.Components`.
  - Required viewmodel updates.
  - Styling or theming needs.

## 8. Workstreams & Hand-off
1. **Blueprint Completion**
   - Populate navigation map, wireframes, inventory docs.
   - Review with stakeholders and Sochi.Navigation maintainers.
2. **Implementation Prep**
   - Create issues per component gap, layout refinement, routing updates.
   - Define definition-of-done (tests, responsive breakpoints, dialog coverage).
3. **Execution**
   - Implement layout shell updates first.
   - Deliver page updates iteratively leveraging SocialKit components.
   - Integrate new components into SocialKit RCL when required.

## 9. Open Questions
- Do we require dynamic theming (dark mode) for parity?
- How will real-time presence integrate with Realtime services beyond in-memory transport?
- Any accessibility baselines (WCAG level) mandated for launch?

## 10. Approvals & Next Steps
- Schedule design review meeting once wireframes + navigation map are ready.
- Capture decisions + action items in `/docs/blueprints/blazorbook/facebook-ui/review-notes.md`.
- After approval, kick off implementation per workstreams.
