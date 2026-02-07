using Content.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IPostStore
/// </summary>
public class EFCorePostStore : IPostStore
{
    private readonly ApplicationDbContext _context;

    public EFCorePostStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PostDto> UpsertAsync(PostDto post, CancellationToken ct = default)
    {
        var existing = await _context.Posts
            .FirstOrDefaultAsync(p => p.TenantId == post.TenantId && p.Id == post.Id, ct);

        if (existing == null)
        {
            _context.Posts.Add(post);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(post);
            existing.Author = post.Author;
            existing.MediaIds = post.MediaIds;
            existing.ReactionCounts = post.ReactionCounts;
        }

        await _context.SaveChangesAsync(ct);
        return post;
    }

    public async Task<PostDto?> GetByIdAsync(string tenantId, string postId, CancellationToken ct = default)
    {
        return await _context.Posts
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == postId, ct);
    }

    public async Task<ContentPageResult<PostDto>> QueryAsync(PostQuery query, CancellationToken ct = default)
    {
        var queryable = _context.Posts
            .Where(p => p.TenantId == query.TenantId);

        if (!query.IncludeDeleted)
        {
            queryable = queryable.IgnoreQueryFilters().Where(p => !p.IsDeleted);
        }

        if (query.Author != null)
        {
            queryable = queryable.Where(p => p.Author.Id == query.Author.Id);
        }

        if (query.MinVisibility.HasValue)
        {
            queryable = queryable.Where(p => p.Visibility <= query.MinVisibility.Value);
        }

        queryable = queryable
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id);

        var offset = DecodeCursor(query.Cursor);
        var items = await queryable
            .Skip(offset)
            .Take(query.Limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > query.Limit;
        if (hasMore)
        {
            items = items.Take(query.Limit).ToList();
        }

        return new ContentPageResult<PostDto>
        {
            Items = items,
            NextCursor = hasMore ? EncodeCursor(offset + query.Limit) : null,
            TotalCount = -1
        };
    }

    public async Task DeleteAsync(string tenantId, string postId, CancellationToken ct = default)
    {
        var post = await GetByIdAsync(tenantId, postId, ct);
        if (post != null)
        {
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task IncrementCommentCountAsync(string tenantId, string postId, int delta, CancellationToken ct = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == postId, ct);

        if (post != null)
        {
            post.CommentCount = Math.Max(0, post.CommentCount + delta);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateReactionCountAsync(string tenantId, string postId, ReactionType type, int delta, CancellationToken ct = default)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == postId, ct);

        if (post != null)
        {
            if (!post.ReactionCounts.ContainsKey(type))
            {
                post.ReactionCounts[type] = 0;
            }

            post.ReactionCounts[type] = Math.Max(0, post.ReactionCounts[type] + delta);
            
            if (post.ReactionCounts[type] <= 0)
            {
                post.ReactionCounts.Remove(type);
            }

            await _context.SaveChangesAsync(ct);
        }
    }

    private static int DecodeCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return 0;

        if (int.TryParse(cursor, out var offset))
            return offset;

        return 0;
    }

    private static string EncodeCursor(int offset)
    {
        return offset.ToString();
    }
}
