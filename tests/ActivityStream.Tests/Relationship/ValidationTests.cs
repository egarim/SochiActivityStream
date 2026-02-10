using ActivityStream.Abstractions;
using ActivityStream.Core;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;

namespace ActivityStream.Tests.Relationship;

/// <summary>
/// Tests for edge validation per Section 7.2 of the plan.
/// </summary>
public class ValidationTests
{
    private readonly RelationshipServiceImpl _service;
    private readonly InMemoryRelationshipStore _store;

    public ValidationTests()
    {
        _store = new InMemoryRelationshipStore();
        _service = new RelationshipServiceImpl(_store, new UlidIdGenerator());
    }

    private static EntityRefDto CreateUser(string id) => new()
    {
        Kind = "user",
        Type = "User",
        Id = id
    };

    private static RelationshipEdgeDto CreateValidEdge() => new()
    {
        TenantId = "acme",
        From = CreateUser("u_1"),
        To = CreateUser("u_2"),
        Kind = RelationshipKind.Follow,
        Scope = RelationshipScope.ActorOnly
    };

    [Fact]
    public async Task Valid_edge_upserts_successfully()
    {
        var edge = CreateValidEdge();

        var result = await _service.UpsertAsync(edge);

        Assert.NotNull(result.Id);
    }

    [Fact]
    public async Task Missing_TenantId_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.TenantId = "";

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "TenantIdRequired");
    }

    [Fact]
    public async Task Whitespace_only_TenantId_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.TenantId = "   ";

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "TenantIdRequired");
    }

    [Fact]
    public async Task Missing_From_Kind_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.From = new EntityRefDto { Kind = "", Type = "User", Id = "u_1" };

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "EntityKindRequired" && e.Path == "From.Kind");
    }

    [Fact]
    public async Task Missing_From_Type_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.From = new EntityRefDto { Kind = "user", Type = "", Id = "u_1" };

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "EntityTypeRequired" && e.Path == "From.Type");
    }

    [Fact]
    public async Task Missing_From_Id_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.From = new EntityRefDto { Kind = "user", Type = "User", Id = "" };

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "EntityIdRequired" && e.Path == "From.Id");
    }

    [Fact]
    public async Task Missing_To_Kind_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.To = new EntityRefDto { Kind = "", Type = "User", Id = "u_2" };

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "EntityKindRequired" && e.Path == "To.Kind");
    }

    [Fact]
    public async Task Missing_To_Type_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.To = new EntityRefDto { Kind = "user", Type = "", Id = "u_2" };

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "EntityTypeRequired" && e.Path == "To.Type");
    }

    [Fact]
    public async Task Missing_To_Id_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.To = new EntityRefDto { Kind = "user", Type = "User", Id = "" };

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "EntityIdRequired" && e.Path == "To.Id");
    }

    [Fact]
    public async Task Filter_trims_and_removes_empty_entries()
    {
        var edge = CreateValidEdge();
        edge.Filter = new RelationshipFilterDto
        {
            TypeKeys = new List<string> { "  invoice.paid  ", "", "   ", "build.completed" },
            RequiredTagsAny = new List<string> { "  tag1  ", "", "tag2" }
        };

        var result = await _service.UpsertAsync(edge);

        Assert.Equal(2, result.Filter!.TypeKeys!.Count);
        Assert.Contains("invoice.paid", result.Filter.TypeKeys);
        Assert.Contains("build.completed", result.Filter.TypeKeys);
        Assert.Equal(2, result.Filter.RequiredTagsAny!.Count);
    }

    [Fact]
    public async Task Filter_dedupes_case_insensitively()
    {
        var edge = CreateValidEdge();
        edge.Filter = new RelationshipFilterDto
        {
            TypeKeys = new List<string> { "Invoice.Paid", "INVOICE.PAID", "invoice.paid" }
        };

        var result = await _service.UpsertAsync(edge);

        Assert.Single(result.Filter!.TypeKeys!);
    }

    [Fact]
    public async Task TypeKey_exceeding_max_length_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.Filter = new RelationshipFilterDto
        {
            TypeKeys = new List<string> { new string('a', 201) }
        };

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "TypeKeyTooLong");
    }

    [Fact]
    public async Task Tag_exceeding_max_length_fails_validation()
    {
        var edge = CreateValidEdge();
        edge.Filter = new RelationshipFilterDto
        {
            RequiredTagsAny = new List<string> { new string('a', 101) }
        };

        var ex = await Assert.ThrowsAsync<RelationshipValidationException>(
            () => _service.UpsertAsync(edge));

        Assert.Contains(ex.Errors, e => e.Code == "RequiredTagTooLong");
    }
}
