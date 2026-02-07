using Content.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of ICommentStore
/// </summary>
public class EFCoreCommentStore : ICommentStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreCommentStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<CommentDto> UpsertAsync(CommentDto comment, CancellationToken ct = default)
    {
        var existing = await _context.Comments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == comment.TenantId && c.Id == comment.Id, ct);

        if (existing == null)
        {
            _context.Comments.Add(comment);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(comment);
            existing.Author = comment.Author;
            existing.ReactionCounts = comment.ReactionCounts;
        }

        await _context.SaveChangesAsync(ct);
        return comment;
    }

    public async Task<CommentDto?> GetByIdAsync(string tenantId, string commentId, CancellationToken ct = default)
    {
        return await _context.Comments
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == commentId, ct);
    }

    public async Task<ContentPageResult<CommentDto>> QueryAsync(CommentQuery query, CancellationToken ct = default)
    {
        var queryable = _context.Comments
            .Where(c => c.TenantId == query.TenantId)
            .Where(c => c.PostId == query.PostId);

        if (!query.IncludeDeleted)
        {
            queryable = queryable.IgnoreQueryFilters().Where(c => !c.IsDeleted);
        }

        // Filter by parent (null = top-level only)
        if (query.ParentCommentId == null)
        {
            queryable = queryable.Where(c => c.ParentCommentId == null);
        }
        else
        {
            queryable = queryable.Where(c => c.ParentCommentId == query.ParentCommentId);
        }

        queryable = queryable
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id);

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

        return new ContentPageResult<CommentDto>
        {
            Items = items,
            NextCursor = hasMore ? EncodeCursor(offset + query.Limit) : null,
            TotalCount = -1
        };
    }

    public async Task DeleteAsync(string tenantId, string commentId, CancellationToken ct = default)
    {
        var comment = await _context.Comments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == commentId, ct);
        
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task IncrementReplyCountAsync(string tenantId, string commentId, int delta, CancellationToken ct = default)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == commentId, ct);

        if (comment != null)
        {
            comment.ReplyCount = Math.Max(0, comment.ReplyCount + delta);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateReactionCountAsync(string tenantId, string commentId, ReactionType type, int delta, CancellationToken ct = default)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == commentId, ct);

        if (comment != null)
        {
            if (!comment.ReactionCounts.ContainsKey(type))
            {
                comment.ReactionCounts[type] = 0;
            }

            comment.ReactionCounts[type] = Math.Max(0, comment.ReactionCounts[type] + delta);

            if (comment.ReactionCounts[type] <= 0)
            {
                comment.ReactionCounts.Remove(type);
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
