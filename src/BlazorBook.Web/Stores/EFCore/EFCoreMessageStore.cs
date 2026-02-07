using Chat.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IMessageStore
/// </summary>
public class EFCoreMessageStore : IMessageStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreMessageStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MessageDto> UpsertAsync(
        MessageDto message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrEmpty(message.Id))
        {
            message.Id = Guid.NewGuid().ToString();
        }

        var existing = await _context.Messages
            .FirstOrDefaultAsync(m => 
                m.Id == message.Id && 
                m.TenantId == message.TenantId && 
                m.ConversationId == message.ConversationId, ct);

        if (existing == null)
        {
            _context.Messages.Add(message);
        }
        else
        {
            existing.Body = message.Body;
            existing.EditedAt = message.EditedAt;
            existing.IsDeleted = message.IsDeleted;
            existing.DeletedByProfileIds = message.DeletedByProfileIds;
        }

        await _context.SaveChangesAsync(ct);
        return message;
    }

    public async Task<MessageDto?> GetByIdAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default)
    {
        return await _context.Messages
            .FirstOrDefaultAsync(m => 
                m.TenantId == tenantId && 
                m.ConversationId == conversationId && 
                m.Id == messageId, ct);
    }

    public async Task<ChatPageResult<MessageDto>> QueryAsync(
        MessageQuery query,
        CancellationToken ct = default)
    {
        var queryable = _context.Messages
            .Where(m => m.TenantId == query.TenantId && m.ConversationId == query.ConversationId)
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        // Apply cursor-based pagination
        if (!string.IsNullOrEmpty(query.Cursor))
        {
            var cursorMessage = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == query.Cursor, ct);

            if (cursorMessage != null)
            {
                if (query.Direction == MessageQueryDirection.Older)
                {
                    queryable = queryable.Where(m => m.CreatedAt < cursorMessage.CreatedAt);
                }
                else
                {
                    queryable = queryable.Where(m => m.CreatedAt > cursorMessage.CreatedAt);
                }
            }
        }

        // Order by date
        if (query.Direction == MessageQueryDirection.Older)
        {
            queryable = queryable.OrderByDescending(m => m.CreatedAt);
        }
        else
        {
            queryable = queryable.OrderBy(m => m.CreatedAt);
        }

        var messages = await queryable
            .Take(query.Limit + 1)
            .ToListAsync(ct);

        // Filter out messages deleted by the viewer (in memory due to JSON list)
        var filtered = messages
            .Where(m => m.DeletedByProfileIds == null || !m.DeletedByProfileIds.Contains(query.Viewer.Id))
            .ToList();

        var items = filtered.Take(query.Limit).ToList();
        var hasMore = filtered.Count > query.Limit;

        return new ChatPageResult<MessageDto>
        {
            Items = items,
            NextCursor = hasMore && items.Count > 0 ? items[^1].Id : null,
            HasMore = hasMore
        };
    }

    public async Task SoftDeleteAsync(
        string tenantId,
        string conversationId,
        string messageId,
        string profileId,
        CancellationToken ct = default)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => 
                m.TenantId == tenantId && 
                m.ConversationId == conversationId && 
                m.Id == messageId, ct);

        if (message != null)
        {
            message.DeletedByProfileIds ??= [];
            if (!message.DeletedByProfileIds.Contains(profileId))
            {
                message.DeletedByProfileIds.Add(profileId);
            }
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task HardDeleteAsync(
        string tenantId,
        string conversationId,
        string messageId,
        CancellationToken ct = default)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => 
                m.TenantId == tenantId && 
                m.ConversationId == conversationId && 
                m.Id == messageId, ct);

        if (message != null)
        {
            message.IsDeleted = true;
            message.Body = "[Message deleted]";
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<MessageDto?> GetLatestMessageAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default)
    {
        return await _context.Messages
            .Where(m => m.TenantId == tenantId && m.ConversationId == conversationId)
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CountMessagesAfterAsync(
        string tenantId,
        string conversationId,
        string? afterMessageId,
        string excludeProfileId,
        CancellationToken ct = default)
    {
        var query = _context.Messages
            .Where(m => m.TenantId == tenantId && m.ConversationId == conversationId)
            .Where(m => !m.IsDeleted)
            .Where(m => m.Sender.Id != excludeProfileId);

        if (!string.IsNullOrEmpty(afterMessageId))
        {
            var afterMessage = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == afterMessageId, ct);

            if (afterMessage != null)
            {
                query = query.Where(m => m.CreatedAt > afterMessage.CreatedAt);
            }
        }

        return await query.CountAsync(ct);
    }
}
