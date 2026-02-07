using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Search.Abstractions;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? "default";

    /// <summary>
    /// Search across all content types
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? types = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var tenantId = GetTenantId();

            var request = new SearchRequest
            {
                TenantId = tenantId,
                Query = q,
                Cursor = cursor,
                Limit = limit,
                IncludeSource = true,
                Highlight = true
            };

            // Parse document types if provided
            if (!string.IsNullOrEmpty(types))
            {
                request.DocumentTypes = types.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var result = await _searchService.SearchAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for '{Query}'", q);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Search for users/profiles
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string q,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var tenantId = GetTenantId();

            var request = new SearchRequest
            {
                TenantId = tenantId,
                Query = q,
                DocumentTypes = ["profile", "user"],
                Cursor = cursor,
                Limit = limit,
                IncludeSource = true
            };

            var result = await _searchService.SearchAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users for '{Query}'", q);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Search for posts
    /// </summary>
    [HttpGet("posts")]
    public async Task<IActionResult> SearchPosts(
        [FromQuery] string q,
        [FromQuery] string? cursor = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var tenantId = GetTenantId();

            var request = new SearchRequest
            {
                TenantId = tenantId,
                Query = q,
                DocumentTypes = ["post"],
                Cursor = cursor,
                Limit = limit,
                IncludeSource = true,
                Highlight = true
            };

            var result = await _searchService.SearchAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts for '{Query}'", q);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get autocomplete suggestions
    /// </summary>
    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete(
        [FromQuery] string q,
        [FromQuery] string? types = null,
        [FromQuery] int limit = 10)
    {
        try
        {
            var tenantId = GetTenantId();

            var request = new AutocompleteRequest
            {
                TenantId = tenantId,
                Prefix = q,
                Limit = limit
            };

            if (!string.IsNullOrEmpty(types))
            {
                request.DocumentTypes = types.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var result = await _searchService.AutocompleteAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting autocomplete for '{Query}'", q);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a document by ID
    /// </summary>
    [HttpGet("{documentType}/{id}")]
    public async Task<IActionResult> GetDocument(string documentType, string id)
    {
        try
        {
            var tenantId = GetTenantId();
            var document = await _searchService.GetDocumentAsync(tenantId, documentType, id);

            if (document == null)
                return NotFound(new { error = "Document not found" });

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentType}/{Id}", documentType, id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Advanced search with filters
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedSearchDto request)
    {
        try
        {
            var tenantId = GetTenantId();

            var searchRequest = new SearchRequest
            {
                TenantId = tenantId,
                Query = request.Query,
                DocumentTypes = request.DocumentTypes ?? [],
                SearchFields = request.SearchFields ?? [],
                Cursor = request.Cursor,
                Limit = request.Limit ?? 20,
                IncludeSource = true,
                Highlight = request.Highlight ?? true
            };

            // Add filters
            if (request.Filters != null)
            {
                foreach (var filter in request.Filters)
                {
                    searchRequest.Filters.Add(new SearchFilter
                    {
                        Field = filter.Field,
                        Operator = Enum.TryParse<SearchFilterOperator>(filter.Operator, true, out var op) 
                            ? op 
                            : SearchFilterOperator.Equals,
                        Value = filter.Value ?? string.Empty
                    });
                }
            }

            // Add sorting
            if (request.Sorts != null)
            {
                foreach (var sort in request.Sorts)
                {
                    searchRequest.Sorts.Add(new SearchSort
                    {
                        Field = sort.Field,
                        Direction = (sort.Descending ?? false) ? SortDirection.Descending : SortDirection.Ascending
                    });
                }
            }

            var result = await _searchService.SearchAsync(searchRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced search");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// REQUEST DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public class AdvancedSearchDto
{
    public string? Query { get; set; }
    public List<string>? DocumentTypes { get; set; }
    public List<string>? SearchFields { get; set; }
    public List<SearchFilterDto>? Filters { get; set; }
    public List<SearchSortDto>? Sorts { get; set; }
    public string? Cursor { get; set; }
    public int? Limit { get; set; }
    public bool? Highlight { get; set; }
}

public class SearchFilterDto
{
    public required string Field { get; set; }
    public string Operator { get; set; } = "equals";
    public object? Value { get; set; }
}

public class SearchSortDto
{
    public required string Field { get; set; }
    public bool? Descending { get; set; }
}
