using Chat.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of IConversationStore
/// </summary>
public class EFCoreConversationStore : IConversationStore
{
    private readonly ApplicationDbContext _context;

    public EFCoreConversationStore(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ConversationDto> UpsertAsync(
        ConversationDto conversation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        if (string.IsNullOrEmpty(conversation.Id))
        {
            conversation.Id = Guid.NewGuid().ToString();
        }

        var existing = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversation.Id && c.TenantId == conversation.TenantId, ct);

        if (existing == null)
        {
            _context.Conversations.Add(conversation);
        }
        else
        {
            existing.Title = conversation.Title;
            existing.AvatarUrl = conversation.AvatarUrl;
            existing.Participants = conversation.Participants;
            existing.UpdatedAt = conversation.UpdatedAt;
        }

        await _context.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task<ConversationDto?> GetByIdAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == conversationId, ct);
    }

    public async Task<ConversationDto?> FindDirectConversationAsync(
        string tenantId,
        string profileId1,
        string profileId2,
        CancellationToken ct = default)
    {
        var conversations = await _context.Conversations
            .Where(c => c.TenantId == tenantId && c.Type == ConversationType.Direct)
            .ToListAsync(ct);

        return conversations.FirstOrDefault(c =>
        {
            var ids = c.Participants
                .Where(p => !p.HasLeft)
                .Select(p => p.Profile.Id)
                .ToHashSet();
            return ids.Contains(profileId1) && ids.Contains(profileId2);
        });
    }

    public async Task<ChatPageResult<ConversationDto>> QueryAsync(
        ConversationQuery query,
        CancellationToken ct = default)
    {
        var queryable = _context.Conversations
            .Where(c => c.TenantId == query.TenantId)
            .AsQueryable();

        // Filter by type
        if (query.Type.HasValue)
        {
            queryable = queryable.Where(c => c.Type == query.Type.Value);
        }

        // Load all matching conversations to filter by participant in memory
        // (complex JSON filtering needs to be done in memory)
        // SQLite doesn't support DateTimeOffset in ORDER BY, so we use client-side sorting
        var conversations = await queryable.ToListAsync(ct);
        
        conversations = conversations
            .OrderByDescending(c => c.UpdatedAt)
            .ToList();

        // Filter by participant
        var filtered = conversations
            .Where(c => c.Participants.Any(p => p.Profile.Id == query.Participant.Id && !p.HasLeft))
            .ToList();

        // Filter by archived if needed
        if (!query.IncludeArchived)
        {
            filtered = filtered.Where(c => !c.IsArchived).ToList();
        }

        // Apply cursor
        if (!string.IsNullOrEmpty(query.Cursor))
        {
            var cursorIndex = filtered.FindIndex(c => c.Id == query.Cursor);
            if (cursorIndex >= 0)
            {
                filtered = filtered.Skip(cursorIndex + 1).ToList();
            }
        }

        var items = filtered.Take(query.Limit).ToList();
        var hasMore = filtered.Count > query.Limit;

        return new ChatPageResult<ConversationDto>
        {
            Items = items,
            NextCursor = hasMore && items.Count > 0 ? items[^1].Id : null,
            HasMore = hasMore
        };
    }

    public async Task DeleteAsync(
        string tenantId,
        string conversationId,
        CancellationToken ct = default)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == conversationId, ct);

        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateParticipantAsync(
        string tenantId,
        string conversationId,
        ConversationParticipantDto participant,
        CancellationToken ct = default)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == conversationId, ct);

        if (conversation != null)
        {
            var existingIndex = conversation.Participants.FindIndex(p => p.Profile.Id == participant.Profile.Id);
            if (existingIndex >= 0)
            {
                conversation.Participants[existingIndex] = participant;
            }
            else
            {
                conversation.Participants.Add(participant);
            }

            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<ConversationParticipantDto?> GetParticipantAsync(
        string tenantId,
        string conversationId,
        string profileId,
        CancellationToken ct = default)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == conversationId, ct);

        return conversation?.Participants.FirstOrDefault(p => p.Profile.Id == profileId);
    }

    public async Task SetUserSettingsAsync(
        string tenantId,
        string conversationId,
        string profileId,
        bool? isArchived,
        bool? isMuted,
        CancellationToken ct = default)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == conversationId, ct);

        if (conversation != null)
        {
            if (isArchived.HasValue)
                conversation.IsArchived = isArchived.Value;
            if (isMuted.HasValue)
                conversation.IsMuted = isMuted.Value;

            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<(bool IsArchived, bool IsMuted)> GetUserSettingsAsync(
        string tenantId,
        string conversationId,
        string profileId,
        CancellationToken ct = default)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == conversationId, ct);

        if (conversation == null)
            return (false, false);

        return (conversation.IsArchived, conversation.IsMuted);
    }
}
