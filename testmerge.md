# Test Merge Plan

## Goal

Consolidate all **unit test** projects into a single `tests/ActivityStream.Tests/` project.  
Keep `tests/BlazorBook.E2E/` as a separate standalone E2E project (unchanged).

---

## Current State

| Project | Test Framework | .cs Files | Helpers | Notes |
|---|---|---|---|---|
| ActivityStream.Tests | xUnit 2.5.3 | 6 | — | **Target project** |
| BlazorBook.Tests | **NUnit 4.1.0** | 11 | TestFixture, 2 mocks | Mixed framework — must convert to xUnit |
| Chat.Tests | xUnit 2.6.2 | 6 | — | |
| Content.Tests | xUnit 2.5.3 | 3 | — | |
| Identity.Tests | xUnit 2.5.3 | 5 | — | |
| Inbox.Tests | xUnit 2.5.3 | 8 | TestGovernancePolicy | |
| Media.Tests | xUnit 2.5.3 | 2 | — | |
| Realtime.Tests | xUnit 2.5.3 | 5 | — | |
| RelationshipService.Tests | xUnit 2.5.3 | 6 | — | |
| Search.Tests | xUnit 2.5.3 | 2 | — | |
| Sochi.Navigation.Tests | xUnit 2.5.3 | 3 | — | |
| **BlazorBook.E2E** | **NUnit + Playwright** | **15** | **4 infra helpers** | **Keep separate** |
| **Total to merge** | | **57 files** | **4 helpers** | |

---

## Target Structure

```
tests/
  ActivityStream.Tests/                  ← Single consolidated unit test project
    ActivityStream.Tests.csproj
    Helpers/
      TestGovernancePolicy.cs            ← from Inbox.Tests
    Mocks/
      MockNavigationService.cs           ← from BlazorBook.Tests
      MockCurrentUserService.cs          ← from BlazorBook.Tests
      TestFixture.cs                     ← from BlazorBook.Tests (xUnit-converted)
    ActivityStream/
      CursorTests.cs                     ← existing
      FilterTests.cs                     ← existing
      IdempotencyTests.cs                ← existing
      IdGenerationTests.cs               ← existing
      PaginationTests.cs                 ← existing
      ValidationTests.cs → ActivityStreamValidationTests.cs  ← rename to avoid clash
    Chat/
      ConversationTests.cs
      DirectConversationTests.cs
      GroupConversationTests.cs
      MessageTests.cs
      ReadReceiptTests.cs
      ValidationTests.cs → ChatValidationTests.cs
    Content/
      CommentTests.cs
      PostTests.cs
      ReactionTests.cs
    Identity/
      AuthServiceTests.cs
      MultiTenantTests.cs
      PasswordHasherTests.cs
      ProfileServiceTests.cs
      ValidationTests.cs → IdentityValidationTests.cs
    Inbox/
      GroupingDedupTests.cs
      NotificationPipelineTests.cs
      PaginationTests.cs → InboxPaginationTests.cs
      PermissionTests.cs
      PolicyViolationTests.cs
      RequestWorkflowTests.cs
      StatusTests.cs
    Media/
      InMemoryMediaStoreTests.cs
      MediaServiceTests.cs
    Realtime/
      ConnectionManagerTests.cs
      PresenceTrackerTests.cs
      PublisherTests.cs
      TargetResolutionTests.cs
      ValidationTests.cs → RealtimeValidationTests.cs
    Relationship/
      DecisionPriorityTests.cs
      EdgeUpsertRemoveTests.cs
      FilterMatchingTests.cs
      MutualRelationshipTests.cs
      ScopeTests.cs
      ValidationTests.cs → RelationshipValidationTests.cs
    Search/
      InMemorySearchIndexTests.cs
      TextAnalyzerTests.cs
    Navigation/
      AsyncDelegateCommandTests.cs
      DelegateCommandTests.cs
      NavigationParametersTests.cs
    BlazorBook/
      ActivityStreamTests.cs → BlazorBookActivityStreamTests.cs
      AuthFlowTests.cs
      CommentsTests.cs
      FeedViewModelTests.cs
      MessagingTests.cs
      ProfileViewModelTests.cs
      SearchTests.cs → BlazorBookSearchTests.cs
      SocialGraphTests.cs

  BlazorBook.E2E/                        ← Untouched
    (stays as-is)
```

---

## Steps

### Phase 1: Prepare the Consolidated .csproj

1. **Upgrade `ActivityStream.Tests.csproj`** to include all needed references:
   - Keep both **xUnit 2.6.2** and **NUnit 4.3.2** (highest version in use)
   - Add all **ProjectReferences** from every source project:
     - `ActivityStream.Abstractions`, `ActivityStream.Core`, `ActivityStream.Store.InMemory`
     - `Identity.Abstractions`, `Identity.Core`, `Identity.Store.InMemory`
     - `Content.Core`, `Content.Store.InMemory`
     - `Chat.Abstractions`, `Chat.Core`, `Chat.Store.InMemory`
     - `RelationshipService.Abstractions`, `RelationshipService.Core`, `RelationshipService.Store.InMemory`
     - `Inbox.Abstractions`, `Inbox.Core`, `Inbox.Store.InMemory`
     - `Media.Abstractions`, `Media.Core`, `Media.Store.InMemory`
     - `Search.Abstractions`, `Search.Core`, `Search.Index.InMemory`
     - `Realtime.Abstractions`, `Realtime.Core`, `Realtime.Transport.InMemory`
     - `SocialKit.Components`, `Sochi.Navigation`
   - Add `Microsoft.Extensions.DependencyInjection` (needed by BlazorBook.Tests fixtures)
   - Bump `Microsoft.NET.Test.Sdk` to **17.10.0**

### Phase 2: Move Test Files into Subdirectories

2. **Create subdirectory structure** inside `tests/ActivityStream.Tests/`:
   - `ActivityStream/`, `Chat/`, `Content/`, `Identity/`, `Inbox/`, `Media/`, `Realtime/`, `Relationship/`, `Search/`, `Navigation/`, `BlazorBook/`, `Helpers/`, `Mocks/`

3. **Move existing ActivityStream.Tests files** into `ActivityStream/` subfolder.

4. **Copy .cs files** from each source test project into the corresponding subfolder:
   - `Chat.Tests/*.cs` → `Chat/`
   - `Content.Tests/*.cs` → `Content/`
   - `Identity.Tests/*.cs` → `Identity/`
   - `Inbox.Tests/*.cs` → `Inbox/` (except `TestGovernancePolicy.cs` → `Helpers/`)
   - `Media.Tests/*.cs` → `Media/`
   - `Realtime.Tests/*.cs` → `Realtime/`
   - `RelationshipService.Tests/*.cs` → `Relationship/`
   - `Search.Tests/*.cs` → `Search/`
   - `Sochi.Navigation.Tests/Commands/*.cs` + `Navigation/*.cs` → `Navigation/`
   - `BlazorBook.Tests/*.cs` → `BlazorBook/` (tests) + `Mocks/` (mock helpers)

### Phase 3: Rename Clashing Filenames

5. **Rename files** that would collide across subdirectories:
   - `ActivityStream/ValidationTests.cs` → `ActivityStreamValidationTests.cs`
   - `Chat/ValidationTests.cs` → `ChatValidationTests.cs`
   - `Identity/ValidationTests.cs` → `IdentityValidationTests.cs`
   - `Realtime/ValidationTests.cs` → `RealtimeValidationTests.cs`
   - `Relationship/ValidationTests.cs` → `RelationshipValidationTests.cs`
   - `Inbox/PaginationTests.cs` → `InboxPaginationTests.cs`
   - `BlazorBook/ActivityStreamTests.cs` → `BlazorBookActivityStreamTests.cs`
   - `BlazorBook/SearchTests.cs` → `BlazorBookSearchTests.cs`

### Phase 4: Update Namespaces

6. **Unify all namespaces** under `ActivityStream.Tests.*`:
   - `Chat.Tests` → `ActivityStream.Tests.Chat`
   - `Content.Tests` → `ActivityStream.Tests.Content`
   - `Identity.Tests` → `ActivityStream.Tests.Identity`
   - `Inbox.Tests` → `ActivityStream.Tests.Inbox`
   - `Media.Tests` → `ActivityStream.Tests.Media`
   - `Realtime.Tests` → `ActivityStream.Tests.Realtime`
   - `RelationshipService.Tests` → `ActivityStream.Tests.Relationship`
   - `Search.Tests` → `ActivityStream.Tests.Search`
   - `Sochi.Navigation.Tests.*` → `ActivityStream.Tests.Navigation`
   - `BlazorBook.Tests` → `ActivityStream.Tests.BlazorBook`
   - `BlazorBook.Tests.ViewModels` → `ActivityStream.Tests.BlazorBook`
   - `BlazorBook.Tests.Mocks` → `ActivityStream.Tests.Mocks`
   - Keep `ActivityStream.Tests` for existing files in root/ActivityStream subfolder.

### Phase 5: Update using Directives

7. **Fix `using` directives** in each moved file to match new namespace locations and project references.  
   Add `using ActivityStream.Abstractions;` where needed since types were consolidated there.

### Phase 6: Remove Old Test Projects

8. **Delete the old test project directories** (after verifying all tests pass):
   - `tests/BlazorBook.Tests/`
   - `tests/Chat.Tests/`
   - `tests/Content.Tests/`
   - `tests/Identity.Tests/`
   - `tests/Inbox.Tests/`
   - `tests/Media.Tests/`
   - `tests/Realtime.Tests/`
   - `tests/RelationshipService.Tests/`
   - `tests/Search.Tests/`
   - `tests/Sochi.Navigation.Tests/`

10. **Update `ActivityStream.slnx`** to remove references to deleted test projects.

### Phase 7: Build & Test

11. **Build the consolidated project**: `dotnet build tests/ActivityStream.Tests/`
12. **Run all tests**: `dotnet test tests/ActivityStream.Tests/`
13. **Verify same pass count** as running all old projects individually (~300+ tests).
14. **Run E2E separately**: `dotnet test tests/BlazorBook.E2E/` (must still work independently).

### Phase 8: Commit & Push

15. `git add -A && git commit -m "Merge all unit tests into ActivityStream.Tests"`
16. `git push origin master`

---

## Risk Mitigation

| Risk | Mitigation |
|---|---|
| Filename collisions (6× `ValidationTests.cs`) | Rename with domain prefix before moving |
| Mixed test frameworks (xUnit + NUnit) | Both frameworks in consolidated csproj; no conversion needed |
| Namespace collisions | Use `ActivityStream.Tests.<Domain>` subfolder namespaces |
| Missing ProjectReferences | Consolidated csproj references every Core + Store + Abstractions project |
| Test count regression | Compare total test count before/after (~300+ across all projects) |

---

## Metrics

- **Projects before**: 11 unit test + 1 E2E = 12
- **Projects after**: 1 unit test + 1 E2E = 2
- **Files to move**: ~57 .cs files + 4 helper/mock files
- **Files requiring rename**: 8
- **Test frameworks**: xUnit (9 projects) + NUnit (2 projects) mixed in single csproj
