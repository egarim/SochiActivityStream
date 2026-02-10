Merge plan - detailed move checklist

Goal: Keep only the following projects under src/:
- src/ActivityStream.Abstractions/ActivityStream.Abstractions.csproj
- src/ActivityStream.Core/ActivityStream.Core.csproj
- src/ActivityStream.Store.InMemory/ActivityStream.Store.InMemory.csproj
- src/SocialKit.Components/SocialKit.Components.csproj

Tests:
- Merge all test sources into tests/ActivityStream.Tests/ActivityStream.Tests.csproj

Per-project source → target mapping

Abstractions (move all public contracts into ActivityStream.Abstractions):
- src/Chat.Abstractions/* -> src/ActivityStream.Abstractions/Chat/*
- src/Content.Abstractions/* -> src/ActivityStream.Abstractions/Content/*
- src/Identity.Abstractions/* -> src/ActivityStream.Abstractions/Identity/*
- src/Inbox.Abstractions/* -> src/ActivityStream.Abstractions/Inbox/*
- src/Media.Abstractions/* -> src/ActivityStream.Abstractions/Media/*
- src/Realtime.Abstractions/* -> src/ActivityStream.Abstractions/Realtime/*
- src/RelationshipService.Abstractions/* -> src/ActivityStream.Abstractions/RelationshipService/*
- src/Search.Abstractions/* -> src/ActivityStream.Abstractions/Search/*
- (Keep ActivityStream.Abstractions content in place)

Core implementations (move into ActivityStream.Core):
- src/Chat.Core/* -> src/ActivityStream.Core/Chat/*
- src/Content.Core/* -> src/ActivityStream.Core/Content/*
- src/Identity.Core/* -> src/ActivityStream.Core/Identity/*
- src/Inbox.Core/* -> src/ActivityStream.Core/Inbox/*
- src/Media.Core/* -> src/ActivityStream.Core/Media/*
- src/Realtime.Core/* -> src/ActivityStream.Core/Realtime/*
- src/RelationshipService.Core/* -> src/ActivityStream.Core/RelationshipService/*
- src/Search.Core/* -> src/ActivityStream.Core/Search/*
- src/ActivityStream.Core/* -> src/ActivityStream.Core/ActivityStream/* (retain existing)

Store.InMemory implementations (group under ActivityStream.Store.InMemory):
- src/Chat.Store.InMemory/* -> src/ActivityStream.Store.InMemory/Chat/*
- src/Content.Store.InMemory/* -> src/ActivityStream.Store.InMemory/Content/*
- src/Identity.Store.InMemory/* -> src/ActivityStream.Store.InMemory/Identity/*
- src/Inbox.Store.InMemory/* -> src/ActivityStream.Store.InMemory/Inbox/*
- src/Media.Store.InMemory/* -> src/ActivityStream.Store.InMemory/Media/*
- src/Realtime.Transport.InMemory/* -> src/ActivityStream.Store.InMemory/Realtime/*
- src/RelationshipService.Store.InMemory/* -> src/ActivityStream.Store.InMemory/RelationshipService/*
- src/Search.Index.InMemory/* -> src/ActivityStream.Store.InMemory/Search/*
- src/ActivityStream.Store.InMemory/* -> src/ActivityStream.Store.InMemory/ActivityStream/* (retain existing)

SocialKit.Components:
- Keep project at src/SocialKit.Components as-is.
- After moving abstractions, update `SocialKit.Components.csproj` ProjectReferences to point to ActivityStream.Abstractions, ActivityStream.Core, ActivityStream.Store.InMemory as needed.

Tests:
- For each tests/* project, copy source files into tests/ActivityStream.Tests preserving folder structure (e.g., tests/Media.Tests/* -> tests/ActivityStream.Tests/Media/*)
- Update test project references to use ActivityStream.* and SocialKit.Components where required.

Namespace strategy:
- Consolidate root to `Sochi.<domain>` for moved code (e.g., `Sochi.Chat`, `Sochi.Media`) or keep `ActivityStream.*` for types originally under that domain; unify during move.
- All common/shared interfaces (e.g., IIdGenerator) will live under `ActivityStream.Abstractions` and be canonicalized.

CSProj edits:
- Add moved files to the destination csproj (wildcard includes preferred) and remove from source csproj.
- Remove empty project folders and their csproj entries from the solution (already updated ActivityStream.slnx).

Verification:
- After each domain move: `dotnet build` then `dotnet test` for the merged test project.

Next actions I will take (pending confirmation):
1. Create a backup commit (or copy) of current `src/` and `tests/`.
2. Start with `Abstractions` consolidation: physically move files for one domain (e.g., `Media.Abstractions`) as a pilot, update csproj and namespaces, build and run tests.

If this plan is good I will proceed with step 1 and then run the pilot domain move.