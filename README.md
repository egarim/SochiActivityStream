# Sochi Activity Stream

A comprehensive collection of storage-agnostic C# libraries for building activity stream systems with relationship management and Blazor MVVM navigation.

## Projects

This repository contains three main library families:

### 1. ActivityStream
A storage-agnostic activity stream library for publishing and querying activities across any domain.

**Projects:**
- [`ActivityStream.Abstractions`](./docs/ActivityStream.Abstractions.md) - Core DTOs, interfaces, and error types
- [`ActivityStream.Core`](./docs/ActivityStream.Core.md) - Service implementation with validation and idempotency
- [`ActivityStream.Store.InMemory`](./docs/ActivityStream.Store.InMemory.md) - Reference in-memory store implementation
- [`ActivityStream.Tests`](./tests/ActivityStream.Tests) - Comprehensive test suite

**Key Features:**
- Universal activity envelope - Single canonical DTO for all activity types
- Storage agnostic - Bring your own persistence via `IActivityStore`
- Idempotent publishing - Deduplication via source system + idempotency key
- Deterministic pagination - Cursor-based pagination with no duplicates or gaps
- Validation - Configurable validation with sensible defaults
- Zero framework dependencies - Pure .NET 8 library, no ASP.NET/UI ties

### 2. RelationshipService
A relationship graph library for managing edges between entities with visibility evaluation.

**Projects:**
- [`RelationshipService.Abstractions`](./docs/RelationshipService.Abstractions.md) - Relationship DTOs, interfaces, and decision types
- [`RelationshipService.Core`](./docs/RelationshipService.Core.md) - Service implementation with deterministic visibility rules
- [`RelationshipService.Store.InMemory`](./docs/RelationshipService.Store.InMemory.md) - Reference in-memory store
- [`RelationshipService.Tests`](./tests/RelationshipService.Tests) - Comprehensive test suite

**Key Features:**
- Relationship management - Follow, Subscribe, Block, Mute, Allow, Deny edges
- Deterministic visibility - Rule-based activity visibility evaluation
- Flexible scoping - Actor, Target, Owner, or Any scope matching
- Filter support - TypeKey, tags, and visibility filtering
- Integration - Works seamlessly with ActivityStream for feed building

### 3. Sochi.Navigation
A Prism-like MVVM navigation library for Blazor with full command pattern support.

**Projects:**
- [`Sochi.Navigation`](./docs/Sochi.Navigation.md) - Complete MVVM navigation library for Blazor
- [`Sochi.Navigation.Sample`](./src/Sochi.Navigation.Sample) - Comprehensive sample application
- [`Sochi.Navigation.Tests`](./tests/Sochi.Navigation.Tests) - Unit tests

**Key Features:**
- ICommand pattern - Sync/async commands with CanExecute support
- ViewModel-first navigation - Navigate with parameters and lifecycle hooks
- CommandButton component - Automatic loading states and button management
- Dialog service - Modal dialogs with parameters and results
- MVVM base classes - ViewModelBase with automatic property tracking
- Navigation lifecycle - INavigationAware, IInitialize, IConfirmNavigation

## Installation

### ActivityStream

```xml
<PackageReference Include="ActivityStream.Abstractions" />
<PackageReference Include="ActivityStream.Core" />
<PackageReference Include="ActivityStream.Store.InMemory" />
```

### RelationshipService

```xml
<PackageReference Include="RelationshipService.Abstractions" />
<PackageReference Include="RelationshipService.Core" />
<PackageReference Include="RelationshipService.Store.InMemory" />
```

### Sochi.Navigation

```bash
dotnet add package Sochi.Navigation
```

## Quick Start Examples

### ActivityStream - Publish and Query Activities

```csharp
using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

// Setup
var store = new InMemoryActivityStore();
var service = new ActivityStreamService(
    store: store,
    idGenerator: new UlidIdGenerator(),
    validator: new DefaultActivityValidator()
);

// Publish an activity
var activity = new ActivityDto
{
    TenantId = "acme",
    TypeKey = "invoice.paid",
    OccurredAt = DateTimeOffset.UtcNow,
    Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123", Display = "Jose" },
    Targets =
    {
        new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_332", Display = "Invoice #332" }
    },
    Summary = "Invoice #332 paid",
    Payload = new { invoiceNumber = 332, amount = 500m, currency = "USD" },
    Source = new ActivitySourceDto { System = "erp", IdempotencyKey = "inv_332_paid" }
};

var stored = await service.PublishAsync(activity);

// Query activities
var page = await service.QueryAsync(new ActivityQuery
{
    TenantId = "acme",
    Limit = 50
});
```

### RelationshipService - Manage Relationships and Visibility

```csharp
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

// Setup
var store = new InMemoryRelationshipStore();
var service = new RelationshipServiceImpl(store, new UlidIdGenerator());

// Create a follow relationship
await service.UpsertAsync(new RelationshipEdgeDto
{
    TenantId = "acme",
    From = new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" },
    To = new EntityRefDto { Kind = "user", Type = "User", Id = "u_2" },
    Kind = RelationshipKind.Follow,
    Scope = RelationshipScope.ActorOnly
});

// Evaluate visibility
var decision = await service.CanSeeAsync(
    tenantId: "acme",
    viewer: new EntityRefDto { Kind = "user", Type = "User", Id = "u_1" },
    activity: activityDto
);

if (decision.Allowed)
{
    // Show activity
}
```

### Sochi.Navigation - MVVM Navigation in Blazor

```csharp
// Program.cs
builder.Services.AddSochiNavigation();
builder.Services.AddViewModel<ProductListViewModel>();

// ViewModel
public class ProductListViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    
    public ProductListViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        NavigateCommand = new AsyncDelegateCommand<Product>(NavigateAsync);
        RegisterCommand(NavigateCommand);
    }
    
    public IAsyncCommand NavigateCommand { get; }
    
    private async Task NavigateAsync(Product product)
    {
        var parameters = new NavigationParameters { { "ProductId", product.Id } };
        await _navigationService.NavigateAsync("/product-detail", parameters);
    }
    
    public async Task InitializeAsync(INavigationParameters parameters)
    {
        // Load data
    }
}
```

```razor
@* Razor Component *@
@page "/products"
@inherits MvvmComponentBase<ProductListViewModel>

<CommandButton Command="@ViewModel.NavigateCommand" 
              CommandParameter="@product"
              CssClass="btn btn-primary">
    View Details
</CommandButton>
```

## Architecture

These libraries follow a consistent architecture pattern:

1. **Abstractions Layer** - DTOs, interfaces, and error types with zero dependencies
2. **Core Layer** - Service implementations with validation and business logic
3. **Store Layer** - Persistence implementations (InMemory reference, bring your own DB)
4. **Tests** - Comprehensive test coverage

All libraries are:
- **Storage agnostic** - Implement `IStore` interfaces for your database
- **Framework independent** - Pure .NET 8, no web/UI dependencies
- **Testable** - Dependency injection friendly with comprehensive test suites
- **Validated** - Built-in validation with extensibility
- **Well documented** - XML docs, READMEs, and sample code

## Use Cases

### Build an Activity Feed System
1. Use **ActivityStream** to publish and query activities
2. Use **RelationshipService** to manage follows/blocks and visibility
3. Combine them to build personalized, filtered activity feeds

### Build a Social Network
- Track user actions with ActivityStream (posts, likes, comments)
- Manage relationships with RelationshipService (follow, block, mute)
- Evaluate visibility rules (who can see what)
- Build home feeds based on relationships

### Build an Audit Log
- Use ActivityStream to record all system activities
- Query by tenant, type, date range, or related entities
- Implement custom storage for high-volume scenarios

### Build a Blazor MVVM Application
- Use Sochi.Navigation for ViewModel-first navigation
- Implement ICommand pattern for all user actions
- Handle navigation lifecycle with INavigationAware, IInitialize
- Show loading states automatically with CommandButton

## Library Documentation

### ActivityStream Family

#### ActivityStream.Abstractions
Core DTOs, interfaces, and error types with zero dependencies.

**Key Types:**
- `ActivityDto` - Universal activity envelope
- `EntityRefDto` - Entity reference (Kind, Type, Id, Display)
- `ActivityQuery` - Query parameters with filtering
- `ActivityVisibility` - Public/Internal/Private
- `IActivityStore` - Storage interface to implement
- `IActivityValidator` - Validation interface

**Location:** `src/ActivityStream.Abstractions/`

#### ActivityStream.Core  
Service implementation with validation, idempotency, and cursor pagination.

**Key Classes:**
- `ActivityStreamService` - Main service implementation
- `DefaultActivityValidator` - Built-in validation rules
- `UlidIdGenerator` - Time-ordered ID generation
- `ActivityCursorBuilder` - Pagination cursor handling

**Features:**
- Idempotent publishing via source system + key
- Deterministic ordering (OccurredAt DESC, Id DESC)
- Cursor pagination with no gaps/duplicates
- Extensible validation

**Location:** `src/ActivityStream.Core/`

#### ActivityStream.Store.InMemory
Reference in-memory storage implementation for testing and development.

**Key Classes:**
- `InMemoryActivityStore` - Thread-safe in-memory store
- Supports all query operations
- Perfect for unit tests

**Location:** `src/ActivityStream.Store.InMemory/`

### RelationshipService Family

#### RelationshipService.Abstractions
Relationship DTOs, interfaces, and decision types.

**Key Types:**
- `RelationshipEdgeDto` - Edge between entities (From → To)
- `RelationshipKind` - Follow, Subscribe, Block, Mute, Allow, Deny
- `RelationshipScope` - ActorOnly, TargetOnly, OwnerOnly, Any
- `RelationshipFilterDto` - TypeKey, tags, visibility filters
- `RelationshipDecision` - Allowed/Denied/Hidden with reason
- `IRelationshipStore` - Storage interface to implement

**Location:** `src/RelationshipService.Abstractions/`

#### RelationshipService.Core
Service implementation with deterministic visibility rules.

**Key Classes:**
- `RelationshipServiceImpl` - Main service implementation
- `RelationshipEdgeValidator` - Edge validation
- `RelationshipEdgeNormalizer` - Normalization logic
- `VisibilityEvaluator` - Deterministic decision engine

**Decision Priority (Highest to Lowest):**
1. SelfAuthored - User always sees own activities
2. Block - Hard deny
3. Deny - Rule-based deny
4. Visibility - Private/Internal/Public checks
5. Mute - Soft hide
6. Allow - Explicit allow
7. Default - Allow

**Location:** `src/RelationshipService.Core/`

#### RelationshipService.Store.InMemory
Reference in-memory storage implementation.

**Key Classes:**
- `InMemoryRelationshipStore` - Thread-safe in-memory store
- Supports edge upsert with uniqueness
- Query by From/To/Kind/Scope

**Location:** `src/RelationshipService.Store.InMemory/`

### Sochi.Navigation

Complete MVVM navigation library for Blazor.

**Namespaces:**
- `Sochi.Navigation.Commands` - ICommand pattern implementations
- `Sochi.Navigation.Navigation` - Navigation service and lifecycle
- `Sochi.Navigation.Mvvm` - Base classes for ViewModels
- `Sochi.Navigation.Components` - Blazor components
- `Sochi.Navigation.Dialogs` - Dialog service
- `Sochi.Navigation.Extensions` - Service registration

**Key Features:**
- `DelegateCommand` / `AsyncDelegateCommand` - Full ICommand support
- `INavigationService` - ViewModel-first navigation
- `INavigationAware` - Navigation lifecycle hooks
- `IInitialize` - Async initialization
- `IConfirmNavigation` - Navigation guards
- `MvvmComponentBase<T>` - Base Razor component
- `CommandButton` - Button with automatic loading states

**Location:** `src/Sochi.Navigation/`

**Sample:** `src/Sochi.Navigation.Sample/` - Complete working example

## Building and Testing

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Run specific project tests
dotnet test tests/ActivityStream.Tests
dotnet test tests/RelationshipService.Tests
dotnet test tests/Sochi.Navigation.Tests

# Run sample application
dotnet run --project src/Sochi.Navigation.Sample
```

## Contributing

Contributions are welcome! Please ensure:
- All tests pass
- New features include tests
- XML documentation is complete
- Follow existing code patterns

## License

MIT

## Installation

Reference the projects in your solution:

```xml
<PackageReference Include="ActivityStream.Abstractions" />
<PackageReference Include="ActivityStream.Core" />
<PackageReference Include="ActivityStream.Store.InMemory" /> <!-- or your own store -->
```

## Quick Start

```csharp
using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;

// Setup
var store = new InMemoryActivityStore();
var service = new ActivityStreamService(
    store: store,
    idGenerator: new UlidIdGenerator(),
    validator: new DefaultActivityValidator()
);

// Publish an activity
var activity = new ActivityDto
{
    TenantId = "acme",
    TypeKey = "invoice.paid",
    OccurredAt = DateTimeOffset.UtcNow,
    Actor = new EntityRefDto { Kind = "user", Type = "User", Id = "u_123", Display = "Jose" },
    Targets =
    {
        new EntityRefDto { Kind = "object", Type = "Invoice", Id = "inv_332", Display = "Invoice #332" }
    },
    Summary = "Invoice #332 paid",
    Payload = new { invoiceNumber = 332, amount = 500m, currency = "USD" },
    Source = new ActivitySourceDto { System = "erp", IdempotencyKey = "inv_332_paid" }
};

var stored = await service.PublishAsync(activity);
Console.WriteLine($"Published: {stored.Id}");

// Query activities
var page = await service.QueryAsync(new ActivityQuery
{
    TenantId = "acme",
    Limit = 50
});

foreach (var item in page.Items)
{
    Console.WriteLine($"{item.OccurredAt:u} {item.TypeKey} {item.Summary}");
}

// Paginate
if (page.NextCursor != null)
{
    var nextPage = await service.QueryAsync(new ActivityQuery
    {
        TenantId = "acme",
        Cursor = page.NextCursor
    });
}
```

## Behavioral Guarantees

### Ordering

All queries return activities ordered by:
1. `OccurredAt` descending (newest first)
2. `Id` descending (tie-breaker for same timestamp)

### Cursor Pagination

- Cursors are opaque strings encoding the position of the last item
- Using a cursor guarantees no duplicates or gaps between pages
- Works correctly even when many activities share the same timestamp
- `NextCursor` is `null` when no more items exist

### Idempotency

When both `Source.System` and `Source.IdempotencyKey` are provided:
- Duplicate publishes return the existing activity (same Id)
- Idempotency is scoped per tenant + system

```csharp
// First publish
var result1 = await service.PublishAsync(activity);

// Second publish with same system + key returns same activity
var result2 = await service.PublishAsync(activity);

Assert.Equal(result1.Id, result2.Id); // Same activity
```

### Validation

Default validation enforces:
- `TenantId` required, non-empty
- `TypeKey` required, max 200 chars
- `OccurredAt` must not be default
- `Actor` required with valid Kind/Type/Id
- `Payload` required (not null)
- `Targets` each must have valid Kind/Type/Id
- `Summary` max 500 chars
- `Tags` max 50 items

Validation errors throw `ActivityValidationException` with detailed error information.

## Project Structure

```
ActivityStream.sln
├── src/
│   ├── ActivityStream.Abstractions/   # DTOs, interfaces, error types
│   ├── ActivityStream.Core/           # Service implementation
│   └── ActivityStream.Store.InMemory/ # Reference in-memory store
└── tests/
    └── ActivityStream.Tests/          # All tests
```

## Extending with Custom Storage

Implement `IActivityStore` for your database:

```csharp
public class PostgresActivityStore : IActivityStore
{
    public Task<ActivityDto?> GetByIdAsync(string tenantId, string id, CancellationToken ct = default)
    {
        // SELECT * FROM activities WHERE tenant_id = @tenantId AND id = @id
    }

    public Task<ActivityDto?> FindByIdempotencyAsync(
        string tenantId, string sourceSystem, string idempotencyKey, CancellationToken ct = default)
    {
        // SELECT * FROM activities WHERE tenant_id = @tenantId 
        //   AND source_system = @sourceSystem AND idempotency_key = @idempotencyKey
    }

    public Task AppendAsync(ActivityDto activity, CancellationToken ct = default)
    {
        // INSERT INTO activities (...)
    }

    public Task<IReadOnlyList<ActivityDto>> QueryAsync(ActivityQuery query, CancellationToken ct = default)
    {
        // SELECT * FROM activities WHERE ... ORDER BY occurred_at DESC, id DESC
    }
}
```

### Postgres Schema Hints

```sql
CREATE TABLE activities (
    id TEXT PRIMARY KEY,
    tenant_id TEXT NOT NULL,
    type_key TEXT NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    actor JSONB NOT NULL,
    owner JSONB,
    targets JSONB NOT NULL DEFAULT '[]',
    visibility INT NOT NULL DEFAULT 1,
    summary TEXT,
    payload JSONB NOT NULL,
    source_system TEXT,
    source_correlation_id TEXT,
    source_idempotency_key TEXT,
    tags TEXT[] NOT NULL DEFAULT '{}'
);

CREATE INDEX idx_activities_tenant_occurred ON activities (tenant_id, occurred_at DESC, id DESC);
CREATE UNIQUE INDEX idx_activities_idempotency ON activities (tenant_id, source_system, source_idempotency_key) 
    WHERE source_system IS NOT NULL AND source_idempotency_key IS NOT NULL;
```

For fast object timelines, normalize targets:
```sql
CREATE TABLE activity_targets (
    activity_id TEXT REFERENCES activities(id),
    tenant_id TEXT NOT NULL,
    kind TEXT NOT NULL,
    type TEXT NOT NULL,
    target_id TEXT NOT NULL
);

CREATE INDEX idx_activity_targets_lookup ON activity_targets (tenant_id, kind, type, target_id);
```

## Running Tests

```bash
dotnet test
```

## License

MIT
